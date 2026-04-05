using FluentAssertions;
using StrictId.Generators.Analyzers;

namespace StrictId.Generators.Test.Analyzers;

/// <summary>
/// Snapshot-style tests for the Phase 9 usage analyzer. Covers STRID001 (cross-type
/// <c>.Value</c> comparison), STRID002 (<c>default(Id&lt;T&gt;)</c> assigned to an
/// <c>Id</c> property), STRID005 (wrong StrictId family given the entity's
/// attribute configuration), and STRID006 (closing a StrictId generic with a generic
/// type parameter).
/// </summary>
[TestFixture]
public class StrictIdUsageAnalyzerTests
{
	private static readonly StrictIdUsageAnalyzer Analyzer = new();

	// ═════ STRID001 — cross-type .Value comparison ═══════════════════════════

	[Test]
	public async Task SameType_ValueComparison_NoDiagnostic ()
	{
		var result = await AnalyzerRunner.RunAsync(Analyzer, """
			using StrictId;
			namespace MyApp;
			public class User { }
			public static class Checker
			{
				public static bool Same(Id<User> a, Id<User> b) => a.Value == b.Value;
			}
			""");

		result.Diagnostics.Should().BeEmpty();
	}

	[Test]
	public async Task DifferentTypes_ValueEquals_ReportsSTRID001 ()
	{
		var result = await AnalyzerRunner.RunAsync(Analyzer, """
			using StrictId;
			namespace MyApp;
			public class User { }
			public class Order { }
			public static class Checker
			{
				public static bool Same(Id<User> a, Id<Order> b) => a.Value == b.Value;
			}
			""");

		result.Diagnostics.Should().ContainSingle(d => d.Id == "STRID001");
	}

	[Test]
	public async Task DifferentTypes_ValueNotEquals_ReportsSTRID001 ()
	{
		var result = await AnalyzerRunner.RunAsync(Analyzer, """
			using StrictId;
			namespace MyApp;
			public class User { }
			public class Order { }
			public static class Checker
			{
				public static bool Different(Id<User> a, Id<Order> b) => a.Value != b.Value;
			}
			""");

		result.Diagnostics.Should().ContainSingle(d => d.Id == "STRID001");
	}

	[Test]
	public async Task DifferentFamilies_ValueEquals_ReportsSTRID001 ()
	{
		// Id<User>.Value (Ulid) vs IdNumber<Order>.Value (ulong) — different types
		// entirely, but the analyzer still fires because both are StrictId generics
		// and the containing types differ.
		var result = await AnalyzerRunner.RunAsync(Analyzer, """
			using StrictId;
			namespace MyApp;
			public class User { }
			public class Order { }
			public static class Checker
			{
				public static bool Cross(Id<User> a, IdNumber<Order> b) => (object)a.Value == (object)b.Value;
			}
			""");

		result.Diagnostics.Should().ContainSingle(d => d.Id == "STRID001");
	}

	[Test]
	public async Task NonStrictIdComparison_NoDiagnostic ()
	{
		// Comparing two unrelated .Value properties must not fire the rule.
		var result = await AnalyzerRunner.RunAsync(Analyzer, """
			namespace MyApp;
			public class Holder { public int Value { get; set; } }
			public static class Checker
			{
				public static bool Same(Holder a, Holder b) => a.Value == b.Value;
			}
			""");

		result.Diagnostics.Should().BeEmpty();
	}

	// ═════ STRID002 — default(Id<T>) assigned to an 'Id' property ════════════

	[Test]
	public async Task ObjectInitializer_DefaultId_ReportsSTRID002 ()
	{
		var result = await AnalyzerRunner.RunAsync(Analyzer, """
			using StrictId;
			namespace MyApp;
			public class User
			{
				public Id<User> Id { get; set; }
			}
			public static class Factory
			{
				public static User Create() => new User { Id = default };
			}
			""");

		result.Diagnostics.Should().ContainSingle(d => d.Id == "STRID002");
	}

	[Test]
	public async Task ObjectInitializer_DefaultIdExplicitType_ReportsSTRID002 ()
	{
		var result = await AnalyzerRunner.RunAsync(Analyzer, """
			using StrictId;
			namespace MyApp;
			public class User
			{
				public Id<User> Id { get; set; }
			}
			public static class Factory
			{
				public static User Create() => new User { Id = default(Id<User>) };
			}
			""");

		result.Diagnostics.Should().ContainSingle(d => d.Id == "STRID002");
	}

	[Test]
	public async Task Constructor_DefaultIdAssignment_ReportsSTRID002 ()
	{
		var result = await AnalyzerRunner.RunAsync(Analyzer, """
			using StrictId;
			namespace MyApp;
			public class User
			{
				public Id<User> Id { get; set; }
				public User() { Id = default; }
			}
			""");

		result.Diagnostics.Should().ContainSingle(d => d.Id == "STRID002");
	}

	[Test]
	public async Task NewIdAssignment_NoDiagnostic ()
	{
		var result = await AnalyzerRunner.RunAsync(Analyzer, """
			using StrictId;
			namespace MyApp;
			public class User
			{
				public Id<User> Id { get; set; }
			}
			public static class Factory
			{
				public static User Create() => new User { Id = Id<User>.NewId() };
			}
			""");

		result.Diagnostics.Should().BeEmpty();
	}

	[Test]
	public async Task NonIdPropertyDefaultAssignment_NoDiagnostic ()
	{
		// Property not named Id — heuristic must not fire.
		var result = await AnalyzerRunner.RunAsync(Analyzer, """
			using StrictId;
			namespace MyApp;
			public class User
			{
				public Id<User> Identifier { get; set; }
			}
			public static class Factory
			{
				public static User Create() => new User { Identifier = default };
			}
			""");

		result.Diagnostics.Should().BeEmpty();
	}

	[Test]
	public async Task GeneralPurposeDefaultAssignment_NoDiagnostic ()
	{
		// Outside an object initializer or constructor — heuristic must not fire.
		var result = await AnalyzerRunner.RunAsync(Analyzer, """
			using StrictId;
			namespace MyApp;
			public class User { public Id<User> Id { get; set; } }
			public static class Resetter
			{
				public static void Reset(User u) { u.Id = default; }
			}
			""");

		result.Diagnostics.Should().BeEmpty();
	}

	// ═════ STRID005 — wrong family for entity attribute configuration ════════

	[Test]
	public async Task IdStringAttributeWithIdWrapper_ReportsSTRID005 ()
	{
		var result = await AnalyzerRunner.RunAsync(Analyzer, """
			using StrictId;
			namespace MyApp;
			[IdString(MaxLength = 32)]
			public class Customer { }
			public static class Bag
			{
				public static Id<Customer>? CustomerId;
			}
			""");

		result.Diagnostics.Should().Contain(d => d.Id == "STRID005");
	}

	[Test]
	public async Task IdStringAttributeWithIdNumberWrapper_ReportsSTRID005 ()
	{
		var result = await AnalyzerRunner.RunAsync(Analyzer, """
			using StrictId;
			namespace MyApp;
			[IdString(MaxLength = 32)]
			public class Customer { }
			public static class Bag
			{
				public static IdNumber<Customer>? CustomerId;
			}
			""");

		result.Diagnostics.Should().Contain(d => d.Id == "STRID005");
	}

	[Test]
	public async Task IdStringAttributeWithIdStringWrapper_NoDiagnostic ()
	{
		var result = await AnalyzerRunner.RunAsync(Analyzer, """
			using StrictId;
			namespace MyApp;
			[IdString(MaxLength = 32)]
			public class Customer { }
			public static class Bag
			{
				public static IdString<Customer>? CustomerId;
			}
			""");

		result.Diagnostics.Should().NotContain(d => d.Id == "STRID005");
	}

	[Test]
	public async Task TypeWithoutIdStringAttribute_NoDiagnostic ()
	{
		var result = await AnalyzerRunner.RunAsync(Analyzer, """
			using StrictId;
			namespace MyApp;
			[IdPrefix("user")]
			public class User { }
			public static class Bag
			{
				public static Id<User>? UserId;
			}
			""");

		result.Diagnostics.Should().NotContain(d => d.Id == "STRID005");
	}

	// ═════ STRID006 — generic type parameter used as StrictId type arg ═══════

	[Test]
	public async Task IdOpenOverGenericParameter_ReportsSTRID006 ()
	{
		var result = await AnalyzerRunner.RunAsync(Analyzer, """
			using StrictId;
			namespace MyApp;
			public class Repository<T>
			{
				public Id<T>? LastId { get; set; }
			}
			""");

		result.Diagnostics.Should().ContainSingle(d => d.Id == "STRID006");
	}

	[Test]
	public async Task IdNumberOpenOverGenericParameter_ReportsSTRID006 ()
	{
		var result = await AnalyzerRunner.RunAsync(Analyzer, """
			using StrictId;
			namespace MyApp;
			public class Repository<T>
			{
				public IdNumber<T>? LastId { get; set; }
			}
			""");

		result.Diagnostics.Should().ContainSingle(d => d.Id == "STRID006");
	}

	[Test]
	public async Task IdStringOpenOverGenericParameter_ReportsSTRID006 ()
	{
		var result = await AnalyzerRunner.RunAsync(Analyzer, """
			using StrictId;
			namespace MyApp;
			public class Repository<T>
			{
				public IdString<T>? LastId { get; set; }
			}
			""");

		result.Diagnostics.Should().ContainSingle(d => d.Id == "STRID006");
	}

	[Test]
	public async Task ClosedGenericNotOverTypeParameter_NoDiagnostic ()
	{
		var result = await AnalyzerRunner.RunAsync(Analyzer, """
			using StrictId;
			namespace MyApp;
			public class User { }
			public class Repository
			{
				public Id<User>? LastId { get; set; }
			}
			""");

		result.Diagnostics.Should().NotContain(d => d.Id == "STRID006");
	}
}
