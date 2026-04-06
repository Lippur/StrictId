using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using StrictId.EFCore;

namespace StrictId.Test.EFCore;

/// <summary>
/// Integration tests for the Phase 7 EF Core rewrite. Every test spins up a fresh
/// SQLite in-memory database and asserts the bare-storage invariant per family:
/// prefixes are a C# type-system concept and must never appear in the persisted column.
/// </summary>
[TestFixture]
public class EFCoreTests
{
	[IdPrefix("user")]
	private class User
	{
		public Id<User> Id { get; set; }
		public string Name { get; set; } = string.Empty;
	}

	[IdPrefix("ord")]
	private class Order
	{
		public IdNumber<Order> Id { get; set; }
		public string Description { get; set; } = string.Empty;
	}

	[IdPrefix("cus"), IdString(MaxLength = 32, CharSet = IdStringCharSet.AlphanumericUnderscore)]
	private class StripeCustomer
	{
		public IdString<StripeCustomer> Id { get; set; }
		public string Email { get; set; } = string.Empty;
	}

	[IdString(MaxLength = 64, CharSet = IdStringCharSet.Any)]
	private class UnicodeTag
	{
		public IdString<UnicodeTag> Id { get; set; }
	}

	[IdPrefix("prod")]
	private class Product
	{
		public Guid<Product> Id { get; set; }
		public string Title { get; set; } = string.Empty;
	}

	/// <summary>
	/// Entity used to exercise the non-generic StrictId families. A plain integer
	/// primary key keeps the schema orthogonal to the family under test so that
	/// failures in the non-generic converter paths are not confounded by key issues.
	/// </summary>
	private class Event
	{
		public int EventId { get; set; }
		public Id CorrelationId { get; set; }
		public IdNumber SequenceNumber { get; set; }
		public IdString ExternalRef { get; set; }
	}

	private class TestDbContext (DbContextOptions<TestDbContext> options) : DbContext(options)
	{
		public DbSet<User> Users => Set<User>();
		public DbSet<Order> Orders => Set<Order>();
		public DbSet<StripeCustomer> StripeCustomers => Set<StripeCustomer>();
		public DbSet<Product> Products => Set<Product>();
		public DbSet<Event> Events => Set<Event>();
		public DbSet<UnicodeTag> UnicodeTags => Set<UnicodeTag>();

		protected override void ConfigureConventions (ModelConfigurationBuilder configurationBuilder)
		{
			configurationBuilder.ConfigureStrictId();
		}

		protected override void OnModelCreating (ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<User>().HasKey(u => u.Id);
			modelBuilder.Entity<Order>().HasKey(o => o.Id);
			modelBuilder.Entity<StripeCustomer>().HasKey(c => c.Id);
			modelBuilder.Entity<Product>().HasKey(p => p.Id);
			modelBuilder.Entity<Event>().HasKey(e => e.EventId);
			modelBuilder.Entity<UnicodeTag>().HasKey(t => t.Id);
		}
	}

	private SqliteConnection _connection = null!;
	private TestDbContext _db = null!;

	[SetUp]
	public void SetUp ()
	{
		_connection = new SqliteConnection("DataSource=:memory:");
		_connection.Open();
		var options = new DbContextOptionsBuilder<TestDbContext>()
			.UseSqlite(_connection)
			.Options;
		_db = new TestDbContext(options);
		_db.Database.EnsureCreated();
	}

	[TearDown]
	public void TearDown ()
	{
		_db.Dispose();
		_connection.Dispose();
	}

	// ═════ Id<T> (ULID family) ═══════════════════════════════════════════════

	[Test]
	public void IdOfT_RoundTripsThroughSqlite ()
	{
		var user = new User { Id = Id<User>.NewId(), Name = "Alice" };
		_db.Users.Add(user);
		_db.SaveChanges();

		_db.ChangeTracker.Clear();

		var fetched = _db.Users.Single(u => u.Id == user.Id);

		fetched.Id.Should().Be(user.Id);
		fetched.Name.Should().Be("Alice");
	}

	[Test]
	public void IdOfT_StoresBareUlidWithoutPrefix ()
	{
		var user = new User { Id = Id<User>.NewId(), Name = "Bob" };
		_db.Users.Add(user);
		_db.SaveChanges();

		var stored = ReadScalarString("SELECT Id FROM Users LIMIT 1");

		stored.Should().NotStartWith("user_");
		stored.Should().NotContain("_");
		stored.Should().HaveLength(26);
		stored.Should().Be(user.Id.ToString("B"));
	}

	[Test]
	public void IdOfT_LinqFilterSendsBareValueToSql ()
	{
		var target = Id<User>.NewId();
		_db.Users.Add(new User { Id = target, Name = "Carol" });
		_db.Users.Add(new User { Id = Id<User>.NewId(), Name = "Dan" });
		_db.SaveChanges();

		_db.ChangeTracker.Clear();

		// The LINQ query below is correct only if the converter emits the bare ULID
		// on both sides of the comparison. If the WHERE clause used the prefixed form
		// and the column held the bare form (or vice versa), no rows would match.
		var hit = _db.Users.Single(u => u.Id == target);
		hit.Name.Should().Be("Carol");
	}

	// ═════ IdNumber<T> (integer family) ══════════════════════════════════════

	[Test]
	public void IdNumberOfT_RoundTripsThroughSqlite ()
	{
		var order = new Order { Id = new IdNumber<Order>(42UL), Description = "widgets" };
		_db.Orders.Add(order);
		_db.SaveChanges();

		_db.ChangeTracker.Clear();

		var fetched = _db.Orders.Single(o => o.Id == order.Id);

		fetched.Id.Should().Be(order.Id);
		fetched.Id.Value.Should().Be(42UL);
		fetched.Description.Should().Be("widgets");
	}

	[Test]
	public void IdNumberOfT_StoresBareBigIntWithoutPrefix ()
	{
		_db.Orders.Add(new Order { Id = new IdNumber<Order>(12345UL), Description = "thing" });
		_db.SaveChanges();

		// SELECT the column as text so any prefix text would be visible. SQLite will
		// coerce an INTEGER to its base-10 string form.
		var stored = ReadScalarString("SELECT CAST(Id AS TEXT) FROM Orders LIMIT 1");

		stored.Should().Be("12345");
	}

	[Test]
	public void IdNumberOfT_LargeUlongWithinLongRangeIsPersisted ()
	{
		var large = new IdNumber<Order>((ulong)long.MaxValue);
		_db.Orders.Add(new Order { Id = large, Description = "big" });
		_db.SaveChanges();

		_db.ChangeTracker.Clear();

		var fetched = _db.Orders.Single();
		fetched.Id.Should().Be(large);
	}

	[Test]
	public void IdNumberOfT_OverflowOnInsertThrows ()
	{
		// Values above long.MaxValue cannot be represented as a signed bigint and
		// must surface as an OverflowException at save time via the checked cast in
		// the converter. This guards against silent truncation.
		var overflow = new IdNumber<Order>(ulong.MaxValue);
		_db.Orders.Add(new Order { Id = overflow, Description = "overflow" });

		var act = () => _db.SaveChanges();
		act.Should().Throw<Exception>().Where(ex =>
			ex is OverflowException || ex.InnerException is OverflowException);
	}

	// ═════ IdString<T> (opaque string family) ════════════════════════════════

	[Test]
	public void IdStringOfT_RoundTripsThroughSqlite ()
	{
		var customer = new StripeCustomer
		{
			Id = new IdString<StripeCustomer>("1a2b3c4d5e"),
			Email = "alice@example.com",
		};
		_db.StripeCustomers.Add(customer);
		_db.SaveChanges();

		_db.ChangeTracker.Clear();

		var fetched = _db.StripeCustomers.Single(c => c.Id == customer.Id);

		fetched.Id.Should().Be(customer.Id);
		fetched.Id.Value.Should().Be("1a2b3c4d5e");
		fetched.Email.Should().Be("alice@example.com");
	}

	[Test]
	public void IdStringOfT_StoresBareSuffixWithoutPrefix ()
	{
		_db.StripeCustomers.Add(new StripeCustomer
		{
			Id = new IdString<StripeCustomer>("abcXYZ"),
			Email = "bob@example.com",
		});
		_db.SaveChanges();

		var stored = ReadScalarString("SELECT Id FROM StripeCustomers LIMIT 1");

		stored.Should().Be("abcXYZ");
		stored.Should().NotStartWith("cus_");
	}

	[Test]
	public void IdStringOfT_AcceptsPrefixedConstructionAndStoresBareSuffix ()
	{
		// Constructing the IdString<T> from its prefixed form must still persist only
		// the bare suffix — the prefix is a C# type-system artefact, not a column value.
		_db.StripeCustomers.Add(new StripeCustomer
		{
			Id = new IdString<StripeCustomer>("cus_stripeId01"),
			Email = "carol@example.com",
		});
		_db.SaveChanges();

		var stored = ReadScalarString("SELECT Id FROM StripeCustomers LIMIT 1");

		stored.Should().Be("stripeId01");
	}

	[Test]
	public void IdStringOfT_MaxLengthConstraintReflectsAttribute ()
	{
		// The StripeCustomer entity declares [IdString(MaxLength = 32)], so the
		// convention should have applied HasMaxLength(32) to the Id column. EF Core
		// surfaces this in the model metadata regardless of whether SQLite enforces it.
		var entityType = _db.Model.FindEntityType(typeof(StripeCustomer))!;
		var property = entityType.FindProperty(nameof(StripeCustomer.Id))!;
		property.GetMaxLength().Should().Be(32);
	}

	[Test]
	public void IdOfT_ColumnHasMaxLength26 ()
	{
		var entityType = _db.Model.FindEntityType(typeof(User))!;
		var property = entityType.FindProperty(nameof(User.Id))!;
		property.GetMaxLength().Should().Be(26);
	}

	// ═════ Guid<T> (Guid family) ════════════════════════════════════════════

	[Test]
	public void GuidOfT_RoundTripsThroughSqlite ()
	{
		var product = new Product { Id = Guid<Product>.NewId(), Title = "Widget" };
		_db.Products.Add(product);
		_db.SaveChanges();

		_db.ChangeTracker.Clear();

		var fetched = _db.Products.Single(p => p.Id == product.Id);

		fetched.Id.Should().Be(product.Id);
		fetched.Title.Should().Be("Widget");
	}

	[Test]
	public void GuidOfT_StoresNativeGuidWithoutPrefix ()
	{
		var guid = Guid.NewGuid();
		_db.Products.Add(new Product { Id = new Guid<Product>(guid), Title = "Gadget" });
		_db.SaveChanges();

		// SQLite stores Guid as a blob/text. Read the raw value and verify no prefix.
		var stored = ReadScalarString("SELECT Id FROM Products LIMIT 1");

		stored.Should().NotStartWith("prod_");
		// SQLite stores the Guid in uppercase hyphenated format.
		stored.Should().ContainEquivalentOf(guid.ToString("D"));
	}

	[Test]
	public void GuidOfT_LinqFilterWorks ()
	{
		var target = Guid<Product>.NewId();
		_db.Products.Add(new Product { Id = target, Title = "Target" });
		_db.Products.Add(new Product { Id = Guid<Product>.NewId(), Title = "Other" });
		_db.SaveChanges();

		_db.ChangeTracker.Clear();

		var hit = _db.Products.Single(p => p.Id == target);
		hit.Title.Should().Be("Target");
	}

	// ═════ Non-generic families ══════════════════════════════════════════════

	[Test]
	public void NonGenericFamilies_RoundTripThroughSqlite ()
	{
		var evt = new Event
		{
			CorrelationId = Id.NewId(),
			SequenceNumber = new IdNumber(7UL),
			ExternalRef = new IdString("ref-001"),
		};
		_db.Events.Add(evt);
		_db.SaveChanges();

		_db.ChangeTracker.Clear();

		var fetched = _db.Events.Single();
		fetched.CorrelationId.Should().Be(evt.CorrelationId);
		fetched.SequenceNumber.Should().Be(evt.SequenceNumber);
		fetched.ExternalRef.Should().Be(evt.ExternalRef);
	}

	[Test]
	public void NonGeneric_ColumnsReflectFamilyWidths ()
	{
		var entityType = _db.Model.FindEntityType(typeof(Event))!;
		entityType.FindProperty(nameof(Event.CorrelationId))!.GetMaxLength().Should().Be(26);
		entityType.FindProperty(nameof(Event.ExternalRef))!.GetMaxLength().Should().Be(255);
	}

	// ═════ Unicode / column-hint propagation ═════════════════════════════════

	[Test]
	public void IdOfT_IsDeclaredNonUnicode ()
	{
		// Crockford base32 ULID strings are ASCII-only, so the column should be
		// declared non-Unicode to avoid a 2x storage cost on SQL Server.
		var property = _db.Model.FindEntityType(typeof(User))!.FindProperty(nameof(User.Id))!;
		property.IsUnicode().Should().BeFalse();
	}

	[Test]
	public void NonGenericId_IsDeclaredNonUnicode ()
	{
		var property = _db.Model.FindEntityType(typeof(Event))!.FindProperty(nameof(Event.CorrelationId))!;
		property.IsUnicode().Should().BeFalse();
	}

	[Test]
	public void IdStringOfT_WithAsciiCharSetIsDeclaredNonUnicode ()
	{
		// StripeCustomer's [IdString(CharSet = AlphanumericUnderscore)] restricts the
		// suffix to ASCII, so the column can be non-Unicode.
		var property = _db.Model.FindEntityType(typeof(StripeCustomer))!.FindProperty(nameof(StripeCustomer.Id))!;
		property.IsUnicode().Should().BeFalse();
	}

	[Test]
	public void IdStringOfT_WithAnyCharSetRemainsUnicode ()
	{
		// UnicodeTag declares [IdString(MaxLength = 64, CharSet = Any)] — the convention
		// must leave Unicode as the provider default for the Any charset.
		var property = _db.Model.FindEntityType(typeof(UnicodeTag))!.FindProperty(nameof(UnicodeTag.Id))!;
		property.IsUnicode().Should().NotBe(false);
		property.GetMaxLength().Should().Be(64);
	}

	private string ReadScalarString (string sql)
	{
		using var command = _connection.CreateCommand();
		command.CommandText = sql;
		var result = command.ExecuteScalar();
		return result?.ToString() ?? string.Empty;
	}
}
