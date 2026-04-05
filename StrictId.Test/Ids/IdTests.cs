using FluentAssertions;

namespace StrictId.Test.Ids;

[TestFixture]
public class IdTests
{
	private const string SampleUlidLower = "01hv9af3qa4t121hcz873m0bkk";
	private const string SampleUlidUpper = "01HV9AF3QA4T121HCZ873M0BKK";
	private const string SampleGuidLower = "018ed2a7-8eea-2682-20c5-9f41c7402e73";

	// ═════ Defaults and known values ═════════════════════════════════════════

	[Test]
	public void Empty_IsDefault ()
	{
		Id.Empty.Should().Be(default(Id));
		Id.Empty.HasValue.Should().BeFalse();
	}

	[Test]
	public void MinValue_EqualsEmpty ()
	{
		Id.MinValue.Should().Be(Id.Empty);
	}

	[Test]
	public void MaxValue_IsUlidMaxValue ()
	{
		Id.MaxValue.Value.Should().Be(Ulid.MaxValue);
	}

	// ═════ Generation ════════════════════════════════════════════════════════

	[Test]
	public void NewId_GeneratesNonEmpty ()
	{
		var id = Id.NewId();
		id.HasValue.Should().BeTrue();
	}

	[Test]
	public void NewId_GeneratesUniqueValues ()
	{
		var a = Id.NewId();
		var b = Id.NewId();
		a.Should().NotBe(b);
	}

	[Test]
	public void NewId_WithTimestamp_EmbedsThatTimestamp ()
	{
		var timestamp = new DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.Zero);
		var id = Id.NewId(timestamp);

		// ULID timestamps have millisecond precision, so compare at ms level.
		id.Time.ToUnixTimeMilliseconds().Should().Be(timestamp.ToUnixTimeMilliseconds());
	}

	// ═════ Constructors ══════════════════════════════════════════════════════

	[Test]
	public void Constructor_FromUlid_PreservesValue ()
	{
		var ulid = Ulid.NewUlid();
		var id = new Id(ulid);
		id.Value.Should().Be(ulid);
	}

	[Test]
	public void Constructor_FromGuid_RoundTrips ()
	{
		var guid = Guid.NewGuid();
		var id = new Id(guid);
		id.ToGuid().Should().Be(guid);
	}

	[Test]
	public void Parse_FromLowercaseUlidString_Parses ()
	{
		var id = Id.Parse(SampleUlidLower);
		id.Value.Should().Be(Ulid.Parse(SampleUlidUpper));
	}

	[Test]
	public void Parse_FromUppercaseUlidString_Parses ()
	{
		var id = Id.Parse(SampleUlidUpper);
		id.Value.Should().Be(Ulid.Parse(SampleUlidUpper));
	}

	[Test]
	public void Parse_FromGuidString_Parses ()
	{
		var id = Id.Parse(SampleGuidLower);
		id.ToGuid().Should().Be(Guid.Parse(SampleGuidLower));
	}

	[Test]
	public void Parse_FromInvalidString_ThrowsFormatException ()
	{
		var act = () => Id.Parse("not a ulid or guid");
		act.Should().Throw<FormatException>();
	}

	// ═════ ToString & format specifiers ══════════════════════════════════════

	[Test]
	public void ToString_Default_IsLowercaseUlid ()
	{
		var id = Id.Parse(SampleUlidUpper);
		id.ToString().Should().Be(SampleUlidLower);
	}

	[Test]
	public void ToString_FormatC_IsLowercaseUlid ()
	{
		var id = Id.Parse(SampleUlidUpper);
		id.ToString("C").Should().Be(SampleUlidLower);
	}

	[Test]
	public void ToString_FormatB_IsBareLowercaseUlid ()
	{
		var id = Id.Parse(SampleUlidUpper);
		id.ToString("B").Should().Be(SampleUlidLower);
	}

	[Test]
	public void ToString_FormatU_IsUppercaseUlid_V2Compat ()
	{
		var id = Id.Parse(SampleUlidUpper);
		id.ToString("U").Should().Be(SampleUlidUpper);
	}

	[Test]
	public void ToString_FormatBG_IsBareLowercaseGuid ()
	{
		var id = Id.Parse(SampleUlidUpper);
		id.ToString("BG").Should().Be(SampleGuidLower);
	}

	[Test]
	public void ToString_FormatG_IsLowercaseGuid_ForNonGeneric ()
	{
		// Id has no prefix, so G and BG are equivalent.
		var id = Id.Parse(SampleUlidUpper);
		id.ToString("G").Should().Be(SampleGuidLower);
	}

	[Test]
	public void ToString_UnknownFormat_Throws ()
	{
		var id = Id.NewId();
		var act = () => id.ToString("Z");
		act.Should().Throw<FormatException>();
	}

	// ═════ Parse / TryParse ══════════════════════════════════════════════════

	[Test]
	public void Parse_BareUlid_Succeeds ()
	{
		var id = Id.Parse(SampleUlidLower);
		id.ToString().Should().Be(SampleUlidLower);
	}

	[Test]
	public void Parse_BareGuid_Succeeds ()
	{
		var id = Id.Parse(SampleGuidLower);
		id.ToGuid().Should().Be(Guid.Parse(SampleGuidLower));
	}

	[Test]
	public void Parse_UppercaseUlid_Succeeds ()
	{
		var id = Id.Parse(SampleUlidUpper);
		id.ToString().Should().Be(SampleUlidLower);
	}

	[Test]
	public void Parse_PrefixedInput_ThrowsForNonGenericId ()
	{
		// Non-generic Id has no prefix concept; prefixed input must not parse.
		var act = () => Id.Parse($"user_{SampleUlidLower}");
		act.Should().Throw<FormatException>()
			.WithMessage("*user*")
			.WithMessage("*no registered prefix*");
	}

	[Test]
	public void Parse_Empty_Throws ()
	{
		var act = () => Id.Parse("");
		act.Should().Throw<FormatException>()
			.WithMessage("*empty*");
	}

	[Test]
	public void Parse_Garbage_Throws ()
	{
		var act = () => Id.Parse("xyz");
		act.Should().Throw<FormatException>();
	}

	[Test]
	public void TryParse_Valid_ReturnsTrue ()
	{
		Id.TryParse(SampleUlidLower, out var id).Should().BeTrue();
		id.ToString().Should().Be(SampleUlidLower);
	}

	[Test]
	public void TryParse_Null_ReturnsFalse ()
	{
		Id.TryParse((string?)null, out var id).Should().BeFalse();
		id.Should().Be(Id.Empty);
	}

	[Test]
	public void TryParse_Invalid_ReturnsFalse ()
	{
		Id.TryParse("not an id", out _).Should().BeFalse();
	}

	[Test]
	public void IsValid_True ()
	{
		Id.IsValid(SampleUlidLower).Should().BeTrue();
	}

	[Test]
	public void IsValid_False ()
	{
		Id.IsValid("garbage").Should().BeFalse();
	}

	// ═════ Round-trip ════════════════════════════════════════════════════════

	[Test]
	public void RoundTrip_ToStringParse ()
	{
		var original = Id.NewId();
		var roundTripped = Id.Parse(original.ToString());
		roundTripped.Should().Be(original);
	}

	[Test]
	public void RoundTrip_ToGuidAndBack ()
	{
		var original = Id.NewId();
		var roundTripped = new Id(original.ToGuid());
		roundTripped.Should().Be(original);
	}

	[Test]
	public void RoundTrip_ToByteArrayAndBack ()
	{
		var original = Id.NewId();
		var bytes = original.ToByteArray();
		var roundTripped = new Id(new Ulid(bytes));
		roundTripped.Should().Be(original);
	}

	// ═════ Comparison ════════════════════════════════════════════════════════

	[Test]
	public void CompareTo_SameValue_IsZero ()
	{
		var id = Id.NewId();
		id.CompareTo(id).Should().Be(0);
	}

	[Test]
	public void CompareTo_EarlierThanLater_IsNegative ()
	{
		var earlier = Id.NewId(new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero));
		var later = Id.NewId(new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero));
		earlier.CompareTo(later).Should().BeNegative();
	}

	[Test]
	public void CompareTo_NullObject_IsPositive ()
	{
		Id.NewId().CompareTo(null).Should().BePositive();
	}

	[Test]
	public void CompareTo_WrongType_Throws ()
	{
		var act = () => Id.NewId().CompareTo("not an id");
		act.Should().Throw<ArgumentException>();
	}

	// ═════ Operators ═════════════════════════════════════════════════════════

	[Test]
	public void Operator_ImplicitFromUlid ()
	{
		Ulid u = Ulid.NewUlid();
		Id id = u;
		id.Value.Should().Be(u);
	}

	[Test]
	public void Operator_ImplicitFromGuid ()
	{
		var g = Guid.NewGuid();
		Id id = g;
		id.ToGuid().Should().Be(g);
	}

	[Test]
	public void Operator_ExplicitFromString ()
	{
		var id = (Id)SampleUlidLower;
		id.ToString().Should().Be(SampleUlidLower);
	}

	[Test]
	public void Operator_ExplicitToUlid ()
	{
		var id = Id.NewId();
		var ulid = (Ulid)id;
		ulid.Should().Be(id.Value);
	}

	[Test]
	public void Operator_ExplicitToGuid ()
	{
		var id = Id.NewId();
		var guid = (Guid)id;
		guid.Should().Be(id.ToGuid());
	}

	[Test]
	public void Operator_ExplicitToString ()
	{
		var id = Id.Parse(SampleUlidUpper);
		var s = (string)id;
		s.Should().Be(SampleUlidLower);
	}

	// ═════ TryFormat ═════════════════════════════════════════════════════════

	[Test]
	public void TryFormatChar_Sufficient_WritesLowercaseUlid ()
	{
		var id = Id.Parse(SampleUlidUpper);
		Span<char> buffer = stackalloc char[26];
		id.TryFormat(buffer, out var written, default, null).Should().BeTrue();
		written.Should().Be(26);
		buffer.ToString().Should().Be(SampleUlidLower);
	}

	[Test]
	public void TryFormatChar_BufferTooSmall_ReturnsFalse ()
	{
		var id = Id.NewId();
		Span<char> buffer = stackalloc char[10];
		id.TryFormat(buffer, out var written, default, null).Should().BeFalse();
		written.Should().Be(0);
	}

	[Test]
	public void TryFormatByte_WritesUtf8Ascii ()
	{
		var id = Id.Parse(SampleUlidUpper);
		Span<byte> buffer = stackalloc byte[26];
		id.TryFormat(buffer, out var written, default, null).Should().BeTrue();
		written.Should().Be(26);
		System.Text.Encoding.ASCII.GetString(buffer).Should().Be(SampleUlidLower);
	}
}
