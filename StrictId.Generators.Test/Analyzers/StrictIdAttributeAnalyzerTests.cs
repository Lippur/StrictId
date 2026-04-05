using FluentAssertions;
using StrictId.Generators.Analyzers;

namespace StrictId.Generators.Test.Analyzers;

/// <summary>
/// Snapshot-style tests for the Phase 9 attribute analyzer. Covers STRID003 (invalid
/// <c>[IdPrefix]</c> grammar/cardinality) and STRID004 (out-of-range
/// <c>[IdSeparator]</c>). Each test feeds a single source snippet through
/// <see cref="AnalyzerRunner"/> and asserts on the diagnostic IDs and counts.
/// </summary>
[TestFixture]
public class StrictIdAttributeAnalyzerTests
{
	private static readonly StrictIdAttributeAnalyzer Analyzer = new();

	// ═════ STRID003 — invalid [IdPrefix] grammar ═════════════════════════════

	[Test]
	public async Task ValidSinglePrefix_NoDiagnostic ()
	{
		var result = await AnalyzerRunner.RunAsync(Analyzer, """
			using StrictId;
			namespace MyApp;
			[IdPrefix("user")]
			public class User { }
			""");

		result.Diagnostics.Should().BeEmpty();
	}

	[Test]
	public async Task UppercasePrefix_ReportsSTRID003 ()
	{
		var result = await AnalyzerRunner.RunAsync(Analyzer, """
			using StrictId;
			namespace MyApp;
			[IdPrefix("USER")]
			public class User { }
			""");

		result.Diagnostics.Should().ContainSingle(d => d.Id == "STRID003");
	}

	[Test]
	public async Task PrefixStartingWithDigit_ReportsSTRID003 ()
	{
		var result = await AnalyzerRunner.RunAsync(Analyzer, """
			using StrictId;
			namespace MyApp;
			[IdPrefix("1abc")]
			public class BadType { }
			""");

		result.Diagnostics.Should().ContainSingle(d => d.Id == "STRID003");
	}

	[Test]
	public async Task EmptyPrefix_ReportsSTRID003 ()
	{
		var result = await AnalyzerRunner.RunAsync(Analyzer, """
			using StrictId;
			namespace MyApp;
			[IdPrefix("")]
			public class BadType { }
			""");

		result.Diagnostics.Should().ContainSingle(d => d.Id == "STRID003");
	}

	[Test]
	public async Task PrefixWithDash_ReportsSTRID003 ()
	{
		var result = await AnalyzerRunner.RunAsync(Analyzer, """
			using StrictId;
			namespace MyApp;
			[IdPrefix("has-dash")]
			public class BadType { }
			""");

		result.Diagnostics.Should().ContainSingle(d => d.Id == "STRID003");
	}

	[Test]
	public async Task DuplicatePrefix_ReportsSTRID003 ()
	{
		var result = await AnalyzerRunner.RunAsync(Analyzer, """
			using StrictId;
			namespace MyApp;
			[IdPrefix("user")]
			[IdPrefix("user")]
			public class User { }
			""");

		// The analyzer reports the duplicate once; the cardinality rule does not
		// double-fire because only one valid declaration remains.
		result.Diagnostics.Should().ContainSingle(d => d.Id == "STRID003");
	}

	[Test]
	public async Task MultiplePrefixesWithoutDefault_ReportsSTRID003 ()
	{
		var result = await AnalyzerRunner.RunAsync(Analyzer, """
			using StrictId;
			namespace MyApp;
			[IdPrefix("a")]
			[IdPrefix("b")]
			public class Ambiguous { }
			""");

		result.Diagnostics.Should().ContainSingle(d => d.Id == "STRID003");
	}

	[Test]
	public async Task MultiplePrefixesWithSingleDefault_NoDiagnostic ()
	{
		var result = await AnalyzerRunner.RunAsync(Analyzer, """
			using StrictId;
			namespace MyApp;
			[IdPrefix("user", IsDefault = true)]
			[IdPrefix("u")]
			public class User { }
			""");

		result.Diagnostics.Should().BeEmpty();
	}

	[Test]
	public async Task MultipleDefaults_ReportsSTRID003Twice ()
	{
		// Each redundant default is flagged individually so users see every offender.
		var result = await AnalyzerRunner.RunAsync(Analyzer, """
			using StrictId;
			namespace MyApp;
			[IdPrefix("a", IsDefault = true)]
			[IdPrefix("b", IsDefault = true)]
			public class Ambiguous { }
			""");

		result.Diagnostics.Count(d => d.Id == "STRID003").Should().Be(2);
	}

	[Test]
	public async Task PrefixAtMaxLength_NoDiagnostic ()
	{
		// 63 chars is the maximum allowed by the grammar.
		var result = await AnalyzerRunner.RunAsync(Analyzer, """
			using StrictId;
			namespace MyApp;
			[IdPrefix("a23456789012345678901234567890123456789012345678901234567890123")]
			public class MaxLengthPrefix { }
			""");

		result.Diagnostics.Should().BeEmpty();
	}

	[Test]
	public async Task PrefixOverMaxLength_ReportsSTRID003 ()
	{
		// 64 chars: one over.
		var result = await AnalyzerRunner.RunAsync(Analyzer, """
			using StrictId;
			namespace MyApp;
			[IdPrefix("a234567890123456789012345678901234567890123456789012345678901234")]
			public class TooLong { }
			""");

		result.Diagnostics.Should().ContainSingle(d => d.Id == "STRID003");
	}

	// ═════ STRID004 — invalid [IdSeparator] ══════════════════════════════════

	[Test]
	public async Task ValidSeparatorValue_NoDiagnostic ()
	{
		var result = await AnalyzerRunner.RunAsync(Analyzer, """
			using StrictId;
			namespace MyApp;
			[IdPrefix("user")]
			[IdSeparator(IdSeparator.Colon)]
			public class User { }
			""");

		result.Diagnostics.Should().BeEmpty();
	}

	[Test]
	public async Task OutOfRangeSeparatorCast_ReportsSTRID004 ()
	{
		var result = await AnalyzerRunner.RunAsync(Analyzer, """
			using StrictId;
			namespace MyApp;
			[IdPrefix("user")]
			[IdSeparator((IdSeparator)99)]
			public class User { }
			""");

		result.Diagnostics.Should().ContainSingle(d => d.Id == "STRID004");
	}

	[Test]
	public async Task NegativeSeparatorCast_ReportsSTRID004 ()
	{
		var result = await AnalyzerRunner.RunAsync(Analyzer, """
			using StrictId;
			namespace MyApp;
			[IdPrefix("user")]
			[IdSeparator((IdSeparator)(-1))]
			public class User { }
			""");

		result.Diagnostics.Should().ContainSingle(d => d.Id == "STRID004");
	}
}
