using FluentAssertions;

namespace StrictId.Test.Ids;

[TestFixture]
public class IdOfTTests
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

	[IdPrefix("gadget")]
	[IdSeparator(IdSeparator.Slash)]
	private class Gadget;

	private const string SampleUlidLower = "01hv9af3qa4t121hcz873m0bkk";
	private const string SampleUlidUpper = "01HV9AF3QA4T121HCZ873M0BKK";
	private const string SampleGuidLower = "018ed2a7-8eea-2682-20c5-9f41c7402e73";

	// ═════ Defaults ══════════════════════════════════════════════════════════

	[Test]
	public void Empty_IsDefault ()
	{
		Id<User>.Empty.Should().Be(default(Id<User>));
		Id<User>.Empty.HasValue.Should().BeFalse();
	}

	[Test]
	public void NewId_GeneratesNonEmpty ()
	{
		Id<User>.NewId().HasValue.Should().BeTrue();
	}

	[Test]
	public void NewId_WithTimestamp_EmbedsTimestamp ()
	{
		var t = new DateTimeOffset(2023, 3, 14, 15, 9, 26, TimeSpan.Zero);
		var id = Id<User>.NewId(t);
		id.Time.ToUnixTimeMilliseconds().Should().Be(t.ToUnixTimeMilliseconds());
	}

	// ═════ ToString with prefix ══════════════════════════════════════════════

	[Test]
	public void ToString_NoPrefix_IsBareLowercaseUlid ()
	{
		var id = new Id<NoPrefix>(Ulid.Parse(SampleUlidUpper));
		id.ToString().Should().Be(SampleUlidLower);
	}

	[Test]
	public void ToString_WithSinglePrefix_IsPrefixedLowercase ()
	{
		var id = new Id<User>(Ulid.Parse(SampleUlidUpper));
		id.ToString().Should().Be($"user_{SampleUlidLower}");
	}

	[Test]
	public void ToString_WithCanonicalSelection_UsesDefaultPrefix ()
	{
		var id = new Id<Order>(Ulid.Parse(SampleUlidUpper));
		id.ToString().Should().Be($"order_{SampleUlidLower}");
	}

	[Test]
	public void ToString_WithCustomSeparator_UsesIt ()
	{
		var id = new Id<Widget>(Ulid.Parse(SampleUlidUpper));
		id.ToString().Should().Be($"widget:{SampleUlidLower}");
	}

	[Test]
	public void ToString_WithSlashSeparator ()
	{
		var id = new Id<Gadget>(Ulid.Parse(SampleUlidUpper));
		id.ToString().Should().Be($"gadget/{SampleUlidLower}");
	}

	[Test]
	public void ToString_FormatB_StripsPrefix ()
	{
		var id = new Id<User>(Ulid.Parse(SampleUlidUpper));
		id.ToString("B").Should().Be(SampleUlidLower);
	}

	[Test]
	public void ToString_FormatG_IsPrefixedGuid ()
	{
		var id = new Id<User>(Ulid.Parse(SampleUlidUpper));
		id.ToString("G").Should().Be($"user_{SampleGuidLower}");
	}

	[Test]
	public void ToString_FormatBG_StripsPrefix ()
	{
		var id = new Id<User>(Ulid.Parse(SampleUlidUpper));
		id.ToString("BG").Should().Be(SampleGuidLower);
	}

	[Test]
	public void ToString_FormatU_IsBareUppercaseUlid ()
	{
		var id = new Id<User>(Ulid.Parse(SampleUlidUpper));
		id.ToString("U").Should().Be(SampleUlidUpper);
	}

	// ═════ Parse with prefix ═════════════════════════════════════════════════

	[Test]
	public void Parse_BareUlid_AlwaysAccepted ()
	{
		// A bare ULID parses even for a type that has a registered prefix.
		var id = Id<User>.Parse(SampleUlidLower);
		id.Value.Should().Be(Ulid.Parse(SampleUlidUpper));
	}

	[Test]
	public void Parse_BareGuid_AlwaysAccepted ()
	{
		var id = Id<User>.Parse(SampleGuidLower);
		id.ToGuid().Should().Be(Guid.Parse(SampleGuidLower));
	}

	[Test]
	public void Parse_PrefixedCanonical_Succeeds ()
	{
		var id = Id<User>.Parse($"user_{SampleUlidLower}");
		id.Value.Should().Be(Ulid.Parse(SampleUlidUpper));
	}

	[Test]
	public void Parse_PrefixedAlias_Succeeds ()
	{
		var a = Id<Order>.Parse($"ord_{SampleUlidLower}");
		var b = Id<Order>.Parse($"o_{SampleUlidLower}");
		var c = Id<Order>.Parse($"order_{SampleUlidLower}");

		a.Should().Be(b);
		b.Should().Be(c);
	}

	[Test]
	public void Parse_PrefixedUppercase_Succeeds ()
	{
		var id = Id<User>.Parse($"USER_{SampleUlidUpper}");
		id.Value.Should().Be(Ulid.Parse(SampleUlidUpper));
	}

	[Test]
	public void Parse_PrefixedGuid_Succeeds ()
	{
		var id = Id<User>.Parse($"user_{SampleGuidLower}");
		id.ToGuid().Should().Be(Guid.Parse(SampleGuidLower));
	}

	[Test]
	public void Parse_SeparatorToleranceOnRead ()
	{
		// Widget declares Colon as canonical, but all four separators must parse.
		var colon = Id<Widget>.Parse($"widget:{SampleUlidLower}");
		var underscore = Id<Widget>.Parse($"widget_{SampleUlidLower}");
		var slash = Id<Widget>.Parse($"widget/{SampleUlidLower}");
		var period = Id<Widget>.Parse($"widget.{SampleUlidLower}");

		colon.Should().Be(underscore);
		underscore.Should().Be(slash);
		slash.Should().Be(period);
	}

	[Test]
	public void Parse_UnknownPrefix_ThrowsWithVerboseMessage ()
	{
		var act = () => Id<User>.Parse($"usr_{SampleUlidLower}");
		act.Should().Throw<FormatException>()
			.WithMessage("*Id<User>*")
			.WithMessage("*'user'*")
			.WithMessage("*usr*")
			.WithMessage("*not registered*");
	}

	[Test]
	public void Parse_InvalidSeparator_ThrowsWithVerboseMessage ()
	{
		var act = () => Id<User>.Parse($"user-{SampleUlidLower}");
		act.Should().Throw<FormatException>()
			.WithMessage("*Id<User>*");
	}

	[Test]
	public void Parse_GarbagedInput_ThrowsWithVerboseMessage ()
	{
		var act = () => Id<User>.Parse("definitely not an id");
		act.Should().Throw<FormatException>()
			.WithMessage("*definitely not an id*");
	}

	[Test]
	public void TryParse_Invalid_ReturnsFalse ()
	{
		Id<User>.TryParse("xyz", out _).Should().BeFalse();
	}

	// ═════ Round-trip ════════════════════════════════════════════════════════

	[Test]
	public void RoundTrip_UserToStringParse ()
	{
		var original = Id<User>.NewId();
		var roundTripped = Id<User>.Parse(original.ToString());
		roundTripped.Should().Be(original);
	}

	[Test]
	public void RoundTrip_WidgetWithColonSeparator ()
	{
		var original = Id<Widget>.NewId();
		var roundTripped = Id<Widget>.Parse(original.ToString());
		roundTripped.Should().Be(original);
		original.ToString().Should().Contain(":");
	}

	[Test]
	public void RoundTrip_AllFormatSpecifiers ()
	{
		var original = Id<User>.NewId();

		foreach (var format in new[] { "C", "B", "G", "BG", "U" })
		{
			var formatted = original.ToString(format);
			var parsed = Id<User>.Parse(formatted);
			parsed.Should().Be(original, because: $"format '{format}' should round-trip");
		}
	}

	// ═════ Cross-type safety ═════════════════════════════════════════════════

	[Test]
	public void Equals_CrossType_FalseEvenWithSameUnderlyingBytes ()
	{
		var ulid = Ulid.NewUlid();
		var user = new Id<User>(ulid);
		var order = new Id<Order>(ulid);

		((object)user).Equals(order).Should().BeFalse();
		((object)order).Equals(user).Should().BeFalse();
	}

	[Test]
	public void Equals_SameType_TrueWithSameUnderlyingBytes ()
	{
		var ulid = Ulid.NewUlid();
		var a = new Id<User>(ulid);
		var b = new Id<User>(ulid);

		a.Should().Be(b);
		a.GetHashCode().Should().Be(b.GetHashCode());
	}

	// ═════ Operators ═════════════════════════════════════════════════════════

	[Test]
	public void Operator_ImplicitFromUlid ()
	{
		Ulid u = Ulid.NewUlid();
		Id<User> id = u;
		id.Value.Should().Be(u);
	}

	[Test]
	public void Operator_ImplicitFromGuid ()
	{
		var g = Guid.NewGuid();
		Id<User> id = g;
		id.ToGuid().Should().Be(g);
	}

	[Test]
	public void Operator_ImplicitFromNonGenericId ()
	{
		Id nonTyped = Id.NewId();
		Id<User> typed = nonTyped;
		typed.Value.Should().Be(nonTyped.Value);
	}

	[Test]
	public void Operator_ExplicitToNonGenericId ()
	{
		Id<User> typed = Id<User>.NewId();
		Id nonTyped = (Id)typed;
		nonTyped.Value.Should().Be(typed.Value);
	}

	[Test]
	public void Operator_ExplicitToUlid ()
	{
		var id = Id<User>.NewId();
		var ulid = (Ulid)id;
		ulid.Should().Be(id.Value);
	}

	[Test]
	public void Operator_ExplicitToGuid ()
	{
		var id = Id<User>.NewId();
		var guid = (Guid)id;
		guid.Should().Be(id.ToGuid());
	}

	[Test]
	public void Operator_ExplicitToString ()
	{
		var id = new Id<User>(Ulid.Parse(SampleUlidUpper));
		((string)id).Should().Be($"user_{SampleUlidLower}");
	}

	[Test]
	public void Operator_ExplicitFromString ()
	{
		var id = (Id<User>)$"user_{SampleUlidLower}";
		id.Value.Should().Be(Ulid.Parse(SampleUlidUpper));
	}

	// ═════ TryFormat ═════════════════════════════════════════════════════════

	[Test]
	public void TryFormatChar_Prefixed_WritesFullCanonical ()
	{
		var id = new Id<User>(Ulid.Parse(SampleUlidUpper));
		var expected = $"user_{SampleUlidLower}";
		Span<char> buffer = stackalloc char[expected.Length];
		id.TryFormat(buffer, out var written, default, null).Should().BeTrue();
		written.Should().Be(expected.Length);
		buffer.ToString().Should().Be(expected);
	}

	[Test]
	public void TryFormatChar_BufferTooSmall_ReturnsFalse ()
	{
		var id = new Id<User>(Ulid.NewUlid());
		Span<char> buffer = stackalloc char[10];
		id.TryFormat(buffer, out var written, default, null).Should().BeFalse();
		written.Should().Be(0);
	}

	[Test]
	public void TryFormatByte_Prefixed_WritesUtf8 ()
	{
		var id = new Id<User>(Ulid.Parse(SampleUlidUpper));
		var expected = $"user_{SampleUlidLower}";
		Span<byte> buffer = stackalloc byte[expected.Length];
		id.TryFormat(buffer, out var written, default, null).Should().BeTrue();
		written.Should().Be(expected.Length);
		System.Text.Encoding.ASCII.GetString(buffer).Should().Be(expected);
	}

	// ═════ Comparison ════════════════════════════════════════════════════════

	[Test]
	public void CompareTo_SameValue_IsZero ()
	{
		var id = Id<User>.NewId();
		id.CompareTo(id).Should().Be(0);
	}

	[Test]
	public void CompareTo_WrongType_Throws ()
	{
		var act = () => Id<User>.NewId().CompareTo(Id<Order>.NewId());
		act.Should().Throw<ArgumentException>();
	}

	// ═════ Helpers ═══════════════════════════════════════════════════════════

	[Test]
	public void ToId_ErasesGenericType ()
	{
		var typed = Id<User>.NewId();
		var erased = typed.ToId();
		erased.Value.Should().Be(typed.Value);
	}

	[Test]
	public void ToByteArray_RoundTrips ()
	{
		var original = Id<User>.NewId();
		var bytes = original.ToByteArray();
		var roundTripped = new Id<User>(new Ulid(bytes));
		roundTripped.Should().Be(original);
	}
}
