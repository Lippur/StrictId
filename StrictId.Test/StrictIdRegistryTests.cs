using FluentAssertions;
using StrictId.Internal;

namespace StrictId.Test;

/// <summary>
/// Tests that <see cref="StrictIdRegistry"/> short-circuits the reflection path in
/// <see cref="StrictIdMetadataResolver"/>. The source generator (Phase 8) populates
/// the registry at module init; these tests simulate that by calling the public
/// registration API directly, using throw-away marker types unique to each test so
/// the cached <c>StrictIdMetadata&lt;T&gt;</c> of other tests is unaffected.
/// </summary>
[TestFixture]
public class StrictIdRegistryTests
{
	private class NotAnnotatedButRegistered;

	private class NotAnnotatedAndUnregistered;

	[IdPrefix("declared")]
	private class DeclaredButOverridden;

	private class StringOptionsRegisteredMarker;

	[Test]
	public void RegisterPrefix_ShortCircuitsReflection ()
	{
		// The type has no [IdPrefix], so reflection would return PrefixInfo.None.
		// Registering a prefix should be observed by ResolvePrefix.
		StrictIdRegistry.RegisterPrefix<NotAnnotatedButRegistered>(
			canonical: "reg",
			aliases: ["reg"],
			separator: IdSeparator.Slash);

		var info = StrictIdMetadataResolver.ResolvePrefix(typeof(NotAnnotatedButRegistered));

		info.Canonical.Should().Be("reg");
		info.Aliases.Should().Equal("reg");
		info.Separator.Should().Be(IdSeparator.Slash);
	}

	[Test]
	public void RegisterPrefix_OverridesAttributeDeclarations ()
	{
		// When both the attribute and a registration exist, the registration wins
		// because it is consulted first. This is the normal path: the generator emits
		// metadata that matches the attributes, so the override is indistinguishable.
		// We deliberately register a different value here to prove precedence.
		StrictIdRegistry.RegisterPrefix<DeclaredButOverridden>(
			canonical: "override",
			aliases: ["override", "alt"],
			separator: IdSeparator.Colon);

		var info = StrictIdMetadataResolver.ResolvePrefix(typeof(DeclaredButOverridden));

		info.Canonical.Should().Be("override");
		info.Aliases.Should().Equal("override", "alt");
		info.Separator.Should().Be(IdSeparator.Colon);
	}

	[Test]
	public void ResolvePrefix_FallsBackToReflectionOnMiss ()
	{
		// No registration for this type — the reflection path must still run and
		// return PrefixInfo.None for an unannotated class.
		var info = StrictIdMetadataResolver.ResolvePrefix(typeof(NotAnnotatedAndUnregistered));

		info.HasPrefix.Should().BeFalse();
		info.Canonical.Should().BeNull();
		info.Aliases.Should().BeEmpty();
	}

	[Test]
	public void RegisterStringOptions_ShortCircuitsReflection ()
	{
		StrictIdRegistry.RegisterStringOptions<StringOptionsRegisteredMarker>(
			maxLength: 16,
			charSet: IdStringCharSet.AlphanumericDash,
			ignoreCase: true);

		var options = StrictIdMetadataResolver.ResolveStringOptions(typeof(StringOptionsRegisteredMarker));

		options.MaxLength.Should().Be(16);
		options.CharSet.Should().Be(IdStringCharSet.AlphanumericDash);
		options.IgnoreCase.Should().BeTrue();
	}
}
