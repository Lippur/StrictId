using FluentAssertions;

namespace StrictId.Generators.Test;

/// <summary>
/// Snapshot-style tests for the prefix-cache incremental generator. Each test feeds a
/// single C# source snippet through <see cref="GeneratorRunner"/> and asserts on the
/// emitted registration code (and any diagnostics). The assertions look for
/// characteristic substrings rather than full-text equality so cosmetic formatting
/// changes in the generator do not require test churn.
/// </summary>
[TestFixture]
public class StrictIdGeneratorTests
{
	[Test]
	public void SinglePrefix_EmitsRegisterPrefixCall ()
	{
		var result = GeneratorRunner.Run("""
			using StrictId;
			namespace MyApp;
			[IdPrefix("user")]
			public class User { }
			""");

		result.Diagnostics.Should().BeEmpty();
		result.GeneratedSources.Should().HaveCount(1);

		var generated = result.GeneratedSources[0];
		generated.Should().Contain("ModuleInitializer");
		generated.Should().Contain("RegisterPrefix<global::MyApp.User>");
		generated.Should().Contain("canonical: \"user\"");
		generated.Should().Contain("aliases: new string[] { \"user\" }");
		generated.Should().Contain("separator: global::StrictId.IdSeparator.Underscore");
	}

	[Test]
	public void MultiplePrefixes_EmitsCanonicalFirstThenAliases ()
	{
		var result = GeneratorRunner.Run("""
			using StrictId;
			namespace MyApp;
			[IdPrefix("order", IsDefault = true)]
			[IdPrefix("ord")]
			[IdPrefix("o")]
			public class Order { }
			""");

		result.Diagnostics.Should().BeEmpty();
		var generated = result.GeneratedSources[0];

		generated.Should().Contain("canonical: \"order\"");
		generated.Should().Contain("aliases: new string[] { \"order\", \"ord\", \"o\" }");
	}

	[Test]
	public void CustomSeparator_IsReflectedInRegistration ()
	{
		var result = GeneratorRunner.Run("""
			using StrictId;
			namespace MyApp;
			[IdPrefix("team")]
			[IdSeparator(IdSeparator.Colon)]
			public class Team { }
			""");

		result.Diagnostics.Should().BeEmpty();
		var generated = result.GeneratedSources[0];
		generated.Should().Contain("separator: global::StrictId.IdSeparator.Colon");
	}

	[Test]
	public void IdStringAttribute_EmitsRegisterStringOptionsCall ()
	{
		var result = GeneratorRunner.Run("""
			using StrictId;
			namespace MyApp;
			[IdPrefix("cus")]
			[IdString(MaxLength = 32, CharSet = IdStringCharSet.AlphanumericUnderscore, IgnoreCase = true)]
			public class Customer { }
			""");

		result.Diagnostics.Should().BeEmpty();
		var generated = result.GeneratedSources[0];
		generated.Should().Contain("RegisterStringOptions<global::MyApp.Customer>");
		generated.Should().Contain("maxLength: 32");
		generated.Should().Contain("charSet: global::StrictId.IdStringCharSet.AlphanumericUnderscore");
		generated.Should().Contain("ignoreCase: true");
	}

	[Test]
	public void InvalidPrefixGrammar_IsSilentlySkipped ()
	{
		// STRID003 in StrictIdAttributeAnalyzer surfaces the grammar error to the
		// user; the generator's job is just to refuse to emit a broken registration.
		var result = GeneratorRunner.Run("""
			using StrictId;
			namespace MyApp;
			[IdPrefix("UpperCaseInvalid")]
			public class BadType { }
			""");

		result.Diagnostics.Should().BeEmpty();
		if (result.GeneratedSources.Length > 0)
		{
			result.GeneratedSources[0].Should().NotContain("RegisterPrefix<global::MyApp.BadType>");
		}
	}

	[Test]
	public void MultiplePrefixesWithoutDefault_IsSilentlySkipped ()
	{
		// STRID003 reports the "no default" case; generator just stays silent.
		var result = GeneratorRunner.Run("""
			using StrictId;
			namespace MyApp;
			[IdPrefix("a")]
			[IdPrefix("b")]
			public class Ambiguous { }
			""");

		result.Diagnostics.Should().BeEmpty();
		if (result.GeneratedSources.Length > 0)
		{
			result.GeneratedSources[0].Should().NotContain("RegisterPrefix<global::MyApp.Ambiguous>");
		}
	}

	[Test]
	public void MultipleDefaults_IsSilentlySkipped ()
	{
		var result = GeneratorRunner.Run("""
			using StrictId;
			namespace MyApp;
			[IdPrefix("a", IsDefault = true)]
			[IdPrefix("b", IsDefault = true)]
			public class Ambiguous { }
			""");

		result.Diagnostics.Should().BeEmpty();
		if (result.GeneratedSources.Length > 0)
		{
			result.GeneratedSources[0].Should().NotContain("RegisterPrefix<global::MyApp.Ambiguous>");
		}
	}

	[Test]
	public void PrivateNestedType_IsSilentlySkipped ()
	{
		var result = GeneratorRunner.Run("""
			using StrictId;
			namespace MyApp;
			public class Outer
			{
				[IdPrefix("nested")]
				private class Nested { }
			}
			""");

		// No diagnostics (the type is valid), but no registration either — the type
		// cannot be named from a top-level namespace so it falls back to reflection
		// at runtime.
		result.Diagnostics.Should().BeEmpty();
		if (result.GeneratedSources.Length > 0)
		{
			result.GeneratedSources[0].Should().NotContain("RegisterPrefix<global::MyApp.Outer.Nested>");
		}
	}

	[Test]
	public void NoDecoratedTypes_EmitsNothing ()
	{
		var result = GeneratorRunner.Run("""
			namespace MyApp;
			public class Unrelated { }
			""");

		result.Diagnostics.Should().BeEmpty();
		result.GeneratedSources.Should().BeEmpty();
	}

	[Test]
	public void GeneratedFile_HasExpectedHintName ()
	{
		var result = GeneratorRunner.Run("""
			using StrictId;
			namespace MyApp;
			[IdPrefix("x")]
			public class X { }
			""");

		result.GeneratedSources.Should().HaveCount(1);
		// A single file is emitted; its contents are the module initializer.
		result.GeneratedSources[0].Should().StartWith("// <auto-generated/>");
	}

	// ═════ Assembly-level [IdSeparator] fallback ═════════════════════════════

	[Test]
	public void AssemblyLevelSeparator_IsUsedWhenTypeHasNoSeparator ()
	{
		var result = GeneratorRunner.Run("""
			using StrictId;
			[assembly: IdSeparator(IdSeparator.Colon)]
			namespace MyApp;
			[IdPrefix("user")]
			public class User { }
			""");

		result.Diagnostics.Should().BeEmpty();
		var generated = result.GeneratedSources[0];
		generated.Should().Contain("separator: global::StrictId.IdSeparator.Colon");
	}

	[Test]
	public void AssemblyLevelSeparator_IsOverriddenByTypeLevelSeparator ()
	{
		var result = GeneratorRunner.Run("""
			using StrictId;
			[assembly: IdSeparator(IdSeparator.Colon)]
			namespace MyApp;
			[IdPrefix("user")]
			[IdSeparator(IdSeparator.Period)]
			public class User { }
			""");

		result.Diagnostics.Should().BeEmpty();
		var generated = result.GeneratedSources[0];
		generated.Should().Contain("separator: global::StrictId.IdSeparator.Period");
	}

	[Test]
	public void AssemblyLevelSeparator_AppliesToAllTypesWithoutOverride ()
	{
		var result = GeneratorRunner.Run("""
			using StrictId;
			[assembly: IdSeparator(IdSeparator.Slash)]
			namespace MyApp;
			[IdPrefix("user")]
			public class User { }
			[IdPrefix("order")]
			public class Order { }
			[IdPrefix("team")]
			[IdSeparator(IdSeparator.Period)]
			public class Team { }
			""");

		result.Diagnostics.Should().BeEmpty();
		var generated = result.GeneratedSources[0];

		// User and Order inherit the assembly-level Slash.
		generated.Should().Contain("RegisterPrefix<global::MyApp.User>(canonical: \"user\", aliases: new string[] { \"user\" }, separator: global::StrictId.IdSeparator.Slash)");
		generated.Should().Contain("RegisterPrefix<global::MyApp.Order>(canonical: \"order\", aliases: new string[] { \"order\" }, separator: global::StrictId.IdSeparator.Slash)");

		// Team has its own type-level Period, overriding the assembly-level Slash.
		generated.Should().Contain("RegisterPrefix<global::MyApp.Team>(canonical: \"team\", aliases: new string[] { \"team\" }, separator: global::StrictId.IdSeparator.Period)");
	}

	[Test]
	public void NoAssemblyLevelSeparator_DefaultsToUnderscore ()
	{
		// Without [assembly: IdSeparator] and no type-level separator, Underscore
		// is the built-in default (existing behaviour, regression guard).
		var result = GeneratorRunner.Run("""
			using StrictId;
			namespace MyApp;
			[IdPrefix("user")]
			public class User { }
			""");

		result.Diagnostics.Should().BeEmpty();
		var generated = result.GeneratedSources[0];
		generated.Should().Contain("separator: global::StrictId.IdSeparator.Underscore");
	}

	// ═════ JSON converter registrations ══════════════════════════════════════

	[Test]
	public void DecoratedType_EmitsJsonConverterRegistrationsForAllThreeFamilies ()
	{
		var result = GeneratorRunner.Run("""
			using StrictId;
			namespace MyApp;
			[IdPrefix("user")]
			public class User { }
			""");

		result.Diagnostics.Should().BeEmpty();
		var generated = result.GeneratedSources[0];

		// All three typed JSON converters get populated per decorated entity type —
		// the generator doesn't know which family the user actually uses, so it
		// pre-populates all three slots. Unused slots cost nothing beyond the registry entry.
		generated.Should().Contain(
			"RegisterJsonConverter<global::StrictId.Id<global::MyApp.User>>(new global::StrictId.Json.IdTypedJsonConverter<global::MyApp.User>())");
		generated.Should().Contain(
			"RegisterJsonConverter<global::StrictId.IdNumber<global::MyApp.User>>(new global::StrictId.Json.IdNumberTypedJsonConverter<global::MyApp.User>())");
		generated.Should().Contain(
			"RegisterJsonConverter<global::StrictId.IdString<global::MyApp.User>>(new global::StrictId.Json.IdStringTypedJsonConverter<global::MyApp.User>())");
	}

	[Test]
	public void InvalidPrefixType_DoesNotEmitJsonRegistrations ()
	{
		// Invalid grammar causes the descriptor to be filtered; the generator must
		// not emit JSON (or EF) registrations for such types, because the analyzer
		// will surface the error and the registration would never be valid anyway.
		var result = GeneratorRunner.Run("""
			using StrictId;
			namespace MyApp;
			[IdPrefix("BAD")]
			public class Bad { }
			""");

		result.Diagnostics.Should().BeEmpty();
		if (result.GeneratedSources.Length > 0)
		{
			result.GeneratedSources[0].Should().NotContain("RegisterJsonConverter<global::StrictId.Id<global::MyApp.Bad>>");
		}
	}

	// ═════ EF Core value-converter registrations ═════════════════════════════

	[Test]
	public void EfCoreNotReferenced_EmitsNoEfRegistrations ()
	{
		// Without StrictId.EFCore on the compilation, the generator must not emit
		// StrictIdEfCoreRegistry calls — they would fail to compile for consumers that
		// only depend on the core package.
		var result = GeneratorRunner.Run("""
			using StrictId;
			namespace MyApp;
			[IdPrefix("user")]
			public class User { }
			""");

		result.Diagnostics.Should().BeEmpty();
		var generated = result.GeneratedSources[0];
		generated.Should().NotContain("StrictIdEfCoreRegistry");
		generated.Should().NotContain("IdToStringConverter<");
	}

	[Test]
	public void EfCoreReferenced_EmitsEfValueConverterRegistrationsForAllThreeFamilies ()
	{
		var result = GeneratorRunner.RunWithEfCore("""
			using StrictId;
			namespace MyApp;
			[IdPrefix("user")]
			public class User { }
			""");

		result.Diagnostics.Should().BeEmpty();
		var generated = result.GeneratedSources[0];

		generated.Should().Contain(
			"StrictIdEfCoreRegistry.RegisterValueConverter<global::StrictId.Id<global::MyApp.User>>(new global::StrictId.EFCore.ValueConverters.IdToStringConverter<global::MyApp.User>())");
		generated.Should().Contain(
			"StrictIdEfCoreRegistry.RegisterValueConverter<global::StrictId.IdNumber<global::MyApp.User>>(new global::StrictId.EFCore.ValueConverters.IdNumberToLongConverter<global::MyApp.User>())");
		generated.Should().Contain(
			"StrictIdEfCoreRegistry.RegisterValueConverter<global::StrictId.IdString<global::MyApp.User>>(new global::StrictId.EFCore.ValueConverters.IdStringToStringConverter<global::MyApp.User>())");
	}

	[Test]
	public void EfCoreReferenced_StillEmitsJsonRegistrations ()
	{
		// Adding EF Core to the mix must not displace the JSON registrations — the
		// two paths are independent.
		var result = GeneratorRunner.RunWithEfCore("""
			using StrictId;
			namespace MyApp;
			[IdPrefix("user")]
			public class User { }
			""");

		result.Diagnostics.Should().BeEmpty();
		result.GeneratedSources[0].Should().Contain("RegisterJsonConverter<global::StrictId.Id<global::MyApp.User>>");
	}
}
