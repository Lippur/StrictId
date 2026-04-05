using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using StrictId.EFCore;
using StrictId.EFCore.ValueConverters;

namespace StrictId.Test.EFCore;

/// <summary>
/// Tests that <see cref="IdConvention"/> consults <see cref="StrictIdEfCoreRegistry"/>
/// before the reflection-based <see cref="Activator.CreateInstance(Type)"/> fallback.
/// The source generator populates the registry at module init for every
/// <c>[IdPrefix]</c>-decorated type; these tests simulate that by registering a
/// sentinel converter directly and asserting the convention surfaces it on the model.
/// </summary>
[TestFixture]
public class RegistryIntegrationTests
{
	public class RegistryTestEntity
	{
		public Id<RegistryTestEntity> Id { get; set; }
	}

	/// <summary>
	/// Sentinel converter used only for identity comparison. Writes an easily-asserted
	/// marker value so tests that probe the actual stored form would see it; instance
	/// identity is the primary assertion.
	/// </summary>
	public sealed class SentinelConverter () : ValueConverter<Id<RegistryTestEntity>, string>(
		id => "sentinel_" + id.ToString("B"),
		value => default
	);

	private class TestDbContext (DbContextOptions<TestDbContext> options) : DbContext(options)
	{
		public DbSet<RegistryTestEntity> Entities => Set<RegistryTestEntity>();

		protected override void ConfigureConventions (ModelConfigurationBuilder configurationBuilder)
		{
			configurationBuilder.ConfigureStrictId();
		}

		protected override void OnModelCreating (ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<RegistryTestEntity>().HasKey(e => e.Id);
		}
	}

	[Test]
	public void IdConvention_UsesRegisteredConverterInsteadOfReflectionFallback ()
	{
		var sentinel = new SentinelConverter();
		StrictIdEfCoreRegistry.RegisterValueConverter<Id<RegistryTestEntity>>(sentinel);

		using var connection = new SqliteConnection("DataSource=:memory:");
		connection.Open();
		var options = new DbContextOptionsBuilder<TestDbContext>().UseSqlite(connection).Options;
		using var db = new TestDbContext(options);

		var entityType = db.Model.FindEntityType(typeof(RegistryTestEntity))!;
		var property = entityType.FindProperty(nameof(RegistryTestEntity.Id))!;
		var appliedConverter = property.GetValueConverter();

		appliedConverter.Should().BeSameAs(sentinel);
	}

	[Test]
	public void IdConvention_FallsBackToReflectionForUnregisteredType ()
	{
		// A type with no registry entry still gets a working converter via the
		// reflection fallback. The type below intentionally has no RegisterValueConverter
		// call, so this exercises the else-branch of ResolveConverter.
		using var connection = new SqliteConnection("DataSource=:memory:");
		connection.Open();
		var options = new DbContextOptionsBuilder<FallbackDbContext>().UseSqlite(connection).Options;
		using var db = new FallbackDbContext(options);

		var entityType = db.Model.FindEntityType(typeof(FallbackEntity))!;
		var property = entityType.FindProperty(nameof(FallbackEntity.Id))!;
		var appliedConverter = property.GetValueConverter();

		appliedConverter.Should().NotBeNull();
		appliedConverter!.GetType().Should().Be(typeof(IdToStringConverter<FallbackEntity>));
	}

	public class FallbackEntity
	{
		public Id<FallbackEntity> Id { get; set; }
	}

	private class FallbackDbContext (DbContextOptions<FallbackDbContext> options) : DbContext(options)
	{
		public DbSet<FallbackEntity> Entities => Set<FallbackEntity>();

		protected override void ConfigureConventions (ModelConfigurationBuilder configurationBuilder)
		{
			configurationBuilder.ConfigureStrictId();
		}

		protected override void OnModelCreating (ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<FallbackEntity>().HasKey(e => e.Id);
		}
	}
}
