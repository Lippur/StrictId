using FluentAssertions;

namespace StrictId.Test.Ids;

[TestFixture]
public class GuidOfTTests
{
	// ───── Test entity types ─────────────────────────────────────────────────

	private class NoPrefix;

	[IdPrefix("user")]
	private class User;

	[IdPrefix("order", IsDefault = true)]
	[IdPrefix("ord")]
	[IdPrefix("o")]
	private class Order;

	[IdPrefix("widget")]
	[IdSeparator(IdSeparator.Colon)]
	private class Widget;

	private static readonly Guid SampleGuid = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");

	// ═════ Defaults ══════════════════════════════════════════════════════════

	[Test]
	public void Empty_IsDefault ()
	{
		Guid<User>.Empty.Should().Be(default(Guid<User>));
		Guid<User>.Empty.HasValue.Should().BeFalse();
	}

	[Test]
	public void NewGuid_GeneratesNonEmpty ()
	{
		Guid<User>.NewGuid().HasValue.Should().BeTrue();
	}

	[Test]
	public void NewId_GeneratesNonEmpty ()
	{
		Guid<User>.NewId().HasValue.Should().BeTrue();
	}

	[Test]
	public void CreateVersion7_GeneratesNonEmpty ()
	{
		Guid<User>.CreateVersion7().HasValue.Should().BeTrue();
	}

	[Test]
	public void CreateVersion7_WithTimestamp_ProducesValidGuid ()
	{
		var t = new DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.Zero);
		var id = Guid<User>.CreateVersion7(t);
		id.HasValue.Should().BeTrue();
	}

	[Test]
	public void NewGuid_ProducesUniqueValues ()
	{
		var a = Guid<User>.NewGuid();
		var b = Guid<User>.NewGuid();
		a.Should().NotBe(b);
	}

	// ═════ ToString with prefix ══════════════════════════════════════════════

	[Test]
	public void ToString_NoPrefix_IsStandardGuidFormat ()
	{
		var id = new Guid<NoPrefix>(SampleGuid);
		id.ToString().Should().Be("550e8400-e29b-41d4-a716-446655440000");
	}

	[Test]
	public void ToString_WithSinglePrefix_IsPrefixed ()
	{
		var id = new Guid<User>(SampleGuid);
		id.ToString().Should().Be("user_550e8400-e29b-41d4-a716-446655440000");
	}

	[Test]
	public void ToString_WithCanonicalSelection_UsesDefaultPrefix ()
	{
		var id = new Guid<Order>(SampleGuid);
		id.ToString().Should().Be("order_550e8400-e29b-41d4-a716-446655440000");
	}

	[Test]
	public void ToString_WithCustomSeparator_UsesIt ()
	{
		var id = new Guid<Widget>(SampleGuid);
		id.ToString().Should().Be("widget:550e8400-e29b-41d4-a716-446655440000");
	}

	// ═════ Format specifiers ════════════════════════════════════════════════

	[Test]
	public void ToString_C_IsCanonical ()
	{
		var id = new Guid<User>(SampleGuid);
		id.ToString("C").Should().Be("user_550e8400-e29b-41d4-a716-446655440000");
	}

	[Test]
	public void ToString_D_IsBareGuidDFormat ()
	{
		var id = new Guid<User>(SampleGuid);
		id.ToString("D").Should().Be("550e8400-e29b-41d4-a716-446655440000");
	}

	[Test]
	public void ToString_N_IsBareGuidNFormat ()
	{
		var id = new Guid<User>(SampleGuid);
		id.ToString("N").Should().Be("550e8400e29b41d4a716446655440000");
	}

	[Test]
	public void ToString_B_IsBareGuidBFormat ()
	{
		var id = new Guid<User>(SampleGuid);
		id.ToString("B").Should().Be("{550e8400-e29b-41d4-a716-446655440000}");
	}

	[Test]
	public void ToString_P_IsBareGuidPFormat ()
	{
		var id = new Guid<User>(SampleGuid);
		id.ToString("P").Should().Be("(550e8400-e29b-41d4-a716-446655440000)");
	}

	[Test]
	public void ToString_NoPrefix_D_MatchesSystemGuid ()
	{
		var id = new Guid<NoPrefix>(SampleGuid);
		id.ToString("D").Should().Be(SampleGuid.ToString("D"));
	}

	[Test]
	public void ToString_NoPrefix_Default_MatchesSystemGuid ()
	{
		var id = new Guid<NoPrefix>(SampleGuid);
		id.ToString().Should().Be(SampleGuid.ToString());
	}

	[Test]
	public void ToString_InvalidFormat_Throws ()
	{
		var id = new Guid<User>(SampleGuid);
		var act = () => id.ToString("Z");
		act.Should().Throw<FormatException>();
	}

	// ═════ Parse ═════════════════════════════════════════════════════════════

	[Test]
	public void Parse_BareGuid_DFormat ()
	{
		var result = Guid<NoPrefix>.Parse("550e8400-e29b-41d4-a716-446655440000");
		result.Value.Should().Be(SampleGuid);
	}

	[Test]
	public void Parse_BareGuid_NFormat ()
	{
		var result = Guid<NoPrefix>.Parse("550e8400e29b41d4a716446655440000");
		result.Value.Should().Be(SampleGuid);
	}

	[Test]
	public void Parse_BareGuid_BFormat ()
	{
		var result = Guid<NoPrefix>.Parse("{550e8400-e29b-41d4-a716-446655440000}");
		result.Value.Should().Be(SampleGuid);
	}

	[Test]
	public void Parse_PrefixedGuid_Canonical ()
	{
		var result = Guid<User>.Parse("user_550e8400-e29b-41d4-a716-446655440000");
		result.Value.Should().Be(SampleGuid);
	}

	[Test]
	public void Parse_PrefixedGuid_AliasParsed ()
	{
		var result = Guid<Order>.Parse("ord_550e8400-e29b-41d4-a716-446655440000");
		result.Value.Should().Be(SampleGuid);
	}

	[Test]
	public void Parse_PrefixedGuid_AlternateSeparator ()
	{
		var result = Guid<User>.Parse("user/550e8400-e29b-41d4-a716-446655440000");
		result.Value.Should().Be(SampleGuid);
	}

	[Test]
	public void Parse_BareGuid_AcceptedWhenTypeHasPrefix ()
	{
		var result = Guid<User>.Parse("550e8400-e29b-41d4-a716-446655440000");
		result.Value.Should().Be(SampleGuid);
	}

	[Test]
	public void Parse_WrongPrefix_ThrowsFormatException ()
	{
		var act = () => Guid<User>.Parse("order_550e8400-e29b-41d4-a716-446655440000");
		act.Should().Throw<FormatException>()
			.WithMessage("*not registered*");
	}

	[Test]
	public void Parse_InvalidGuid_ThrowsFormatException ()
	{
		var act = () => Guid<User>.Parse("not-a-guid");
		act.Should().Throw<FormatException>();
	}

	[Test]
	public void TryParse_ValidBareGuid_ReturnsTrue ()
	{
		Guid<User>.TryParse("550e8400-e29b-41d4-a716-446655440000", out var result).Should().BeTrue();
		result.Value.Should().Be(SampleGuid);
	}

	[Test]
	public void TryParse_Null_ReturnsFalse ()
	{
		Guid<User>.TryParse(null, out _).Should().BeFalse();
	}

	[Test]
	public void IsValid_ValidGuid_ReturnsTrue ()
	{
		Guid<User>.IsValid("550e8400-e29b-41d4-a716-446655440000").Should().BeTrue();
	}

	[Test]
	public void IsValid_Invalid_ReturnsFalse ()
	{
		Guid<User>.IsValid("nope").Should().BeFalse();
	}

	// ═════ IdFormat.RequirePrefix ════════════════════════════════════════════

	[Test]
	public void Parse_RequirePrefix_AcceptsPrefixed ()
	{
		var id = Guid<User>.Parse($"user_{SampleGuid:D}", IdFormat.RequirePrefix);
		id.Value.Should().Be(SampleGuid);
	}

	[Test]
	public void Parse_RequirePrefix_AcceptsAlias ()
	{
		var id = Guid<Order>.Parse($"ord_{SampleGuid:D}", IdFormat.RequirePrefix);
		id.Value.Should().Be(SampleGuid);
	}

	[Test]
	public void Parse_RequirePrefix_RejectsBareGuid ()
	{
		var act = () => Guid<User>.Parse(SampleGuid.ToString("D"), IdFormat.RequirePrefix);
		act.Should().Throw<FormatException>()
			.WithMessage("*bare GUID*prefix is required*");
	}

	[Test]
	public void TryParse_RequirePrefix_ReturnsFalseForBare ()
	{
		Guid<User>.TryParse(SampleGuid.ToString("D"), IdFormat.RequirePrefix, out _).Should().BeFalse();
	}

	[Test]
	public void TryParse_RequirePrefix_ReturnsTrueForPrefixed ()
	{
		Guid<User>.TryParse($"user_{SampleGuid:D}", IdFormat.RequirePrefix, out var id).Should().BeTrue();
		id.Value.Should().Be(SampleGuid);
	}

	[Test]
	public void Parse_RequirePrefix_NoPrefixRegistered_AcceptsBare ()
	{
		var id = Guid<NoPrefix>.Parse(SampleGuid.ToString("D"), IdFormat.RequirePrefix);
		id.Value.Should().Be(SampleGuid);
	}

	[Test]
	public void TryParse_RequirePrefix_NoPrefixRegistered_AcceptsBare ()
	{
		Guid<NoPrefix>.TryParse(SampleGuid.ToString("D"), IdFormat.RequirePrefix, out var id).Should().BeTrue();
		id.Value.Should().Be(SampleGuid);
	}

	[Test]
	public void Parse_NullProvider_SameAsLenient ()
	{
		var id = Guid<User>.Parse(SampleGuid.ToString("D"), null);
		id.Value.Should().Be(SampleGuid);
	}

	// ═════ Round-trip ════════════════════════════════════════════════════════

	[Test]
	public void RoundTrip_WithPrefix ()
	{
		var original = Guid<User>.NewId();
		var text = original.ToString();
		var parsed = Guid<User>.Parse(text);
		parsed.Should().Be(original);
	}

	[Test]
	public void RoundTrip_WithoutPrefix ()
	{
		var original = Guid<NoPrefix>.NewId();
		var text = original.ToString();
		var parsed = Guid<NoPrefix>.Parse(text);
		parsed.Should().Be(original);
	}

	// ═════ Operators ════════════════════════════════════════════════════════

	[Test]
	public void ImplicitConversion_FromGuid ()
	{
		Guid<User> id = SampleGuid;
		id.Value.Should().Be(SampleGuid);
	}

	[Test]
	public void ExplicitConversion_ToGuid ()
	{
		var id = new Guid<User>(SampleGuid);
		((Guid)id).Should().Be(SampleGuid);
	}

	[Test]
	public void ExplicitConversion_ToString ()
	{
		var id = new Guid<User>(SampleGuid);
		((string)id).Should().Be(id.ToString());
	}

	[Test]
	public void ExplicitConversion_FromString ()
	{
		var id = (Guid<User>)"user_550e8400-e29b-41d4-a716-446655440000";
		id.Value.Should().Be(SampleGuid);
	}

	// ═════ Helpers ═══════════════════════════════════════════════════════════

	[Test]
	public void ToGuid_ReturnsValue ()
	{
		var id = new Guid<User>(SampleGuid);
		id.ToGuid().Should().Be(SampleGuid);
	}

	[Test]
	public void ToByteArray_MatchesGuid ()
	{
		var id = new Guid<User>(SampleGuid);
		id.ToByteArray().Should().Equal(SampleGuid.ToByteArray());
	}

	// ═════ Cross-type safety ════════════════════════════════════════════════

	[Test]
	public void CrossType_Equality_NeverHolds ()
	{
		var userId = new Guid<User>(SampleGuid);
		var orderId = new Guid<Order>(SampleGuid);
		object boxedUser = userId;
		object boxedOrder = orderId;
		boxedUser.Equals(boxedOrder).Should().BeFalse();
	}

	// ═════ CompareTo ═════════════════════════════════════════════════════════

	[Test]
	public void CompareTo_SameType_Compares ()
	{
		var a = new Guid<User>(Guid.Parse("00000000-0000-0000-0000-000000000001"));
		var b = new Guid<User>(Guid.Parse("00000000-0000-0000-0000-000000000002"));
		a.CompareTo(b).Should().BeNegative();
		b.CompareTo(a).Should().BePositive();
		a.CompareTo(a).Should().Be(0);
	}

	[Test]
	public void CompareTo_Object_WrongType_Throws ()
	{
		var id = Guid<User>.NewGuid();
		var act = () => id.CompareTo("not an id");
		act.Should().Throw<ArgumentException>();
	}

	[Test]
	public void CompareTo_Object_Null_Returns1 ()
	{
		var id = Guid<User>.NewGuid();
		id.CompareTo(null).Should().Be(1);
	}
}
