using FluentAssertions;
using StrictId.Internal;

// The test fixtures below deliberately declare malformed [IdPrefix] attributes to
// exercise the runtime resolver's error paths. STRID003 (the compile-time analyzer
// counterpart) correctly flags these as errors, so suppress the rule for this file
// only — otherwise the test fixtures cannot compile.
#pragma warning disable STRID003

namespace StrictId.Test.Internal;

[TestFixture]
public class StrictIdMetadataResolverTests
{
	// ───── Happy-path test types ─────────────────────────────────────────────

	private class NoAttributes;

	[IdPrefix("user")]
	private class SinglePrefix;

	[IdPrefix("order", IsDefault = true)]
	[IdPrefix("ord")]
	[IdPrefix("o")]
	private class MultiPrefix;

	[IdPrefix("user")]
	[IdSeparator(IdSeparator.Colon)]
	private class PrefixAndSeparator;

	[IdSeparator(IdSeparator.Slash)]
	private class SeparatorOnly;

	[IdPrefix("a23456789012345678901234567890123456789012345678901234567890123")]
	private class MaxLengthPrefix;

	// ───── Inheritance test types ────────────────────────────────────────────

	[IdPrefix("base")]
	[IdSeparator(IdSeparator.Colon)]
	private class BaseEntity;

	private class InheritsBoth : BaseEntity;

	[IdPrefix("derived")]
	private class OverridesPrefix : BaseEntity;

	[IdSeparator(IdSeparator.Slash)]
	private class OverridesSeparator : BaseEntity;

	[IdPrefix("derived")]
	[IdSeparator(IdSeparator.Underscore)]
	private class OverridesBoth : BaseEntity;

	[IdPrefix("a", IsDefault = true)]
	[IdPrefix("b")]
	private class MultiBase;

	[IdPrefix("c")]
	private class MultiOverride : MultiBase;

	// ───── Invalid test types ────────────────────────────────────────────────

	[IdPrefix("USER")]
	private class InvalidUpperCase;

	[IdPrefix("1abc")]
	private class InvalidStartsWithDigit;

	[IdPrefix("")]
	private class InvalidEmpty;

	[IdPrefix("has-dash")]
	private class InvalidContainsDash;

	[IdPrefix("aaaaaaaaaabbbbbbbbbbccccccccccddddddddddeeeeeeeeeeffffffffffggggg")] // 65 chars
	private class InvalidTooLong;

	[IdPrefix("a")]
	[IdPrefix("b")]
	private class InvalidMultipleNoDefault;

	[IdPrefix("a", IsDefault = true)]
	[IdPrefix("b", IsDefault = true)]
	private class InvalidMultipleDefaults;

	[IdPrefix("dup")]
	[IdPrefix("dup")]
	private class InvalidDuplicate;

	// ───── IdString config test types ────────────────────────────────────────

	private class NoStringAttr;

	[IdString(MaxLength = 32, CharSet = IdStringCharSet.Alphanumeric, IgnoreCase = true)]
	private class WithStringAttr;

	[IdString(MaxLength = 50)]
	private class StringBase;

	private class InheritsString : StringBase;

	[IdString(MaxLength = 100)]
	private class OverridesString : StringBase;

	// ═════ Prefix resolution ═════════════════════════════════════════════════

	[Test]
	public void NoAttributes_ReturnsEmptyPrefixWithUnderscoreSeparator ()
	{
		var info = StrictIdMetadataResolver.ResolvePrefix(typeof(NoAttributes));

		info.Canonical.Should().BeNull();
		info.HasPrefix.Should().BeFalse();
		info.Aliases.Should().BeEmpty();
		info.Separator.Should().Be(IdSeparator.Underscore);
	}

	[Test]
	public void SinglePrefix_IsImplicitlyCanonical ()
	{
		var info = StrictIdMetadataResolver.ResolvePrefix(typeof(SinglePrefix));

		info.Canonical.Should().Be("user");
		info.HasPrefix.Should().BeTrue();
		info.Aliases.Should().Equal("user");
		info.Separator.Should().Be(IdSeparator.Underscore);
	}

	[Test]
	public void MultiplePrefixes_DefaultIsCanonicalAndFirstInAliases ()
	{
		var info = StrictIdMetadataResolver.ResolvePrefix(typeof(MultiPrefix));

		info.Canonical.Should().Be("order");
		info.Aliases.Should().HaveCount(3);
		info.Aliases[0].Should().Be("order");
		info.Aliases.Should().Contain(["order", "ord", "o"]);
	}

	[Test]
	public void SeparatorAttribute_IsRespected ()
	{
		var info = StrictIdMetadataResolver.ResolvePrefix(typeof(PrefixAndSeparator));

		info.Canonical.Should().Be("user");
		info.Separator.Should().Be(IdSeparator.Colon);
	}

	[Test]
	public void SeparatorWithoutPrefix_IsAllowedButEmitsBare ()
	{
		var info = StrictIdMetadataResolver.ResolvePrefix(typeof(SeparatorOnly));

		info.Canonical.Should().BeNull();
		info.HasPrefix.Should().BeFalse();
		info.Separator.Should().Be(IdSeparator.Slash);
	}

	[Test]
	public void MaxLengthPrefix_Exactly63Chars_IsAccepted ()
	{
		var info = StrictIdMetadataResolver.ResolvePrefix(typeof(MaxLengthPrefix));

		info.Canonical!.Length.Should().Be(63);
	}

	// ═════ Inheritance ═══════════════════════════════════════════════════════

	[Test]
	public void Inheritance_DerivedInheritsBothFromBase ()
	{
		var info = StrictIdMetadataResolver.ResolvePrefix(typeof(InheritsBoth));

		info.Canonical.Should().Be("base");
		info.Separator.Should().Be(IdSeparator.Colon);
	}

	[Test]
	public void Inheritance_DerivedOverridesPrefixInheritsSeparator ()
	{
		var info = StrictIdMetadataResolver.ResolvePrefix(typeof(OverridesPrefix));

		info.Canonical.Should().Be("derived");
		info.Separator.Should().Be(IdSeparator.Colon);
	}

	[Test]
	public void Inheritance_DerivedOverridesSeparatorInheritsPrefix ()
	{
		var info = StrictIdMetadataResolver.ResolvePrefix(typeof(OverridesSeparator));

		info.Canonical.Should().Be("base");
		info.Separator.Should().Be(IdSeparator.Slash);
	}

	[Test]
	public void Inheritance_DerivedOverridesBoth ()
	{
		var info = StrictIdMetadataResolver.ResolvePrefix(typeof(OverridesBoth));

		info.Canonical.Should().Be("derived");
		info.Separator.Should().Be(IdSeparator.Underscore);
	}

	[Test]
	public void Inheritance_DerivedOverrideFullyReplacesNotMerges ()
	{
		var info = StrictIdMetadataResolver.ResolvePrefix(typeof(MultiOverride));

		info.Canonical.Should().Be("c");
		info.Aliases.Should().Equal("c");
		info.Aliases.Should().NotContain("a");
		info.Aliases.Should().NotContain("b");
	}

	// ═════ Invalid declarations ══════════════════════════════════════════════

	[Test]
	public void Invalid_UppercasePrefix_Throws ()
	{
		var act = () => StrictIdMetadataResolver.ResolvePrefix(typeof(InvalidUpperCase));

		act.Should().Throw<InvalidOperationException>()
			.WithMessage("*USER*")
			.WithMessage("*lowercase ASCII letter*");
	}

	[Test]
	public void Invalid_StartsWithDigit_Throws ()
	{
		var act = () => StrictIdMetadataResolver.ResolvePrefix(typeof(InvalidStartsWithDigit));

		act.Should().Throw<InvalidOperationException>()
			.WithMessage("*1abc*")
			.WithMessage("*lowercase ASCII letter*");
	}

	[Test]
	public void Invalid_EmptyPrefix_Throws ()
	{
		var act = () => StrictIdMetadataResolver.ResolvePrefix(typeof(InvalidEmpty));

		act.Should().Throw<InvalidOperationException>()
			.WithMessage("*empty*");
	}

	[Test]
	public void Invalid_ContainsDash_Throws ()
	{
		var act = () => StrictIdMetadataResolver.ResolvePrefix(typeof(InvalidContainsDash));

		act.Should().Throw<InvalidOperationException>()
			.WithMessage("*has-dash*")
			.WithMessage("*position 3*");
	}

	[Test]
	public void Invalid_TooLong_Throws ()
	{
		var act = () => StrictIdMetadataResolver.ResolvePrefix(typeof(InvalidTooLong));

		act.Should().Throw<InvalidOperationException>()
			.WithMessage("*63 characters*");
	}

	[Test]
	public void Invalid_MultipleNoDefault_Throws ()
	{
		var act = () => StrictIdMetadataResolver.ResolvePrefix(typeof(InvalidMultipleNoDefault));

		act.Should().Throw<InvalidOperationException>()
			.WithMessage("*none is marked IsDefault*");
	}

	[Test]
	public void Invalid_MultipleDefaults_Throws ()
	{
		var act = () => StrictIdMetadataResolver.ResolvePrefix(typeof(InvalidMultipleDefaults));

		act.Should().Throw<InvalidOperationException>()
			.WithMessage("*marked IsDefault = true*");
	}

	[Test]
	public void Invalid_DuplicatePrefix_Throws ()
	{
		var act = () => StrictIdMetadataResolver.ResolvePrefix(typeof(InvalidDuplicate));

		act.Should().Throw<InvalidOperationException>()
			.WithMessage("*'dup'*")
			.WithMessage("*more than once*");
	}

	// ═════ IsKnownPrefix ═════════════════════════════════════════════════════

	[Test]
	public void IsKnownPrefix_MatchesCanonical ()
	{
		var info = StrictIdMetadataResolver.ResolvePrefix(typeof(MultiPrefix));

		info.IsKnownPrefix("order".AsSpan()).Should().BeTrue();
	}

	[Test]
	public void IsKnownPrefix_MatchesAlias ()
	{
		var info = StrictIdMetadataResolver.ResolvePrefix(typeof(MultiPrefix));

		info.IsKnownPrefix("ord".AsSpan()).Should().BeTrue();
		info.IsKnownPrefix("o".AsSpan()).Should().BeTrue();
	}

	[Test]
	public void IsKnownPrefix_IsCaseInsensitive ()
	{
		var info = StrictIdMetadataResolver.ResolvePrefix(typeof(MultiPrefix));

		info.IsKnownPrefix("ORDER".AsSpan()).Should().BeTrue();
		info.IsKnownPrefix("Order".AsSpan()).Should().BeTrue();
		info.IsKnownPrefix("ORD".AsSpan()).Should().BeTrue();
	}

	[Test]
	public void IsKnownPrefix_RejectsUnknown ()
	{
		var info = StrictIdMetadataResolver.ResolvePrefix(typeof(MultiPrefix));

		info.IsKnownPrefix("unknown".AsSpan()).Should().BeFalse();
		info.IsKnownPrefix("".AsSpan()).Should().BeFalse();
	}

	[Test]
	public void IsKnownPrefix_EmptyAliasList_RejectsEverything ()
	{
		var info = StrictIdMetadataResolver.ResolvePrefix(typeof(NoAttributes));

		info.IsKnownPrefix("anything".AsSpan()).Should().BeFalse();
	}

	// ═════ IdStringOptions ═══════════════════════════════════════════════════

	[Test]
	public void IdStringOptions_DefaultsWhenNoAttribute ()
	{
		var opts = StrictIdMetadataResolver.ResolveStringOptions(typeof(NoStringAttr));

		opts.MaxLength.Should().Be(255);
		opts.CharSet.Should().Be(IdStringCharSet.AlphanumericDashUnderscore);
		opts.IgnoreCase.Should().BeFalse();
	}

	[Test]
	public void IdStringOptions_RespectsAttribute ()
	{
		var opts = StrictIdMetadataResolver.ResolveStringOptions(typeof(WithStringAttr));

		opts.MaxLength.Should().Be(32);
		opts.CharSet.Should().Be(IdStringCharSet.Alphanumeric);
		opts.IgnoreCase.Should().BeTrue();
	}

	[Test]
	public void IdStringOptions_InheritsFromBase ()
	{
		var opts = StrictIdMetadataResolver.ResolveStringOptions(typeof(InheritsString));

		opts.MaxLength.Should().Be(50);
	}

	[Test]
	public void IdStringOptions_DerivedOverridesBase ()
	{
		var opts = StrictIdMetadataResolver.ResolveStringOptions(typeof(OverridesString));

		opts.MaxLength.Should().Be(100);
	}

	// ═════ Generic cache ═════════════════════════════════════════════════════

	[Test]
	public void GenericCache_CachesPerClosedGeneric ()
	{
		var a = StrictIdMetadata<SinglePrefix>.Prefix;
		var b = StrictIdMetadata<NoAttributes>.Prefix;

		a.Canonical.Should().Be("user");
		b.Canonical.Should().BeNull();
	}

	[Test]
	public void GenericCache_IdStringOptionsIsSeparate ()
	{
		var prefix = StrictIdMetadata<WithStringAttr>.Prefix;
		var opts = IdStringMetadata<WithStringAttr>.Options;

		prefix.Canonical.Should().BeNull();
		opts.MaxLength.Should().Be(32);
	}
}
