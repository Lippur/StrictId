using FluentAssertions;

namespace StrictId.Test.Ids;

[TestFixture]
public class IdStringTests
{
	// ═════ Defaults ══════════════════════════════════════════════════════════

	[Test]
	public void Default_HasNullValueAndHasValueFalse ()
	{
		var empty = default(IdString);
		empty.Value.Should().BeNull();
		empty.HasValue.Should().BeFalse();
	}

	[Test]
	public void Empty_EqualsDefault ()
	{
		IdString.Empty.Should().Be(default(IdString));
	}

	[Test]
	public void Default_ToString_IsEmpty ()
	{
		default(IdString).ToString().Should().Be(string.Empty);
	}

	// ═════ Construction ══════════════════════════════════════════════════════

	[Test]
	public void Constructor_FromSimpleString ()
	{
		var id = new IdString("abc123");
		id.Value.Should().Be("abc123");
		id.HasValue.Should().BeTrue();
	}

	[Test]
	public void Constructor_EmptyString_Throws ()
	{
		var act = () => new IdString("");
		act.Should().Throw<FormatException>().WithMessage("*empty*");
	}

	[Test]
	public void Constructor_WhitespaceString_Throws ()
	{
		var act = () => new IdString("abc def");
		act.Should().Throw<FormatException>().WithMessage("*whitespace*");
	}

	[Test]
	public void Constructor_TooLong_Throws ()
	{
		var tooLong = new string('x', 256);
		var act = () => new IdString(tooLong);
		act.Should().Throw<FormatException>().WithMessage("*exceeds the maximum*");
	}

	[Test]
	public void Constructor_ExactlyMaxLength_Succeeds ()
	{
		var max = new string('x', 255);
		var id = new IdString(max);
		id.Value.Length.Should().Be(255);
	}

	[Test]
	public void Constructor_DashAndUnderscore_AllowedByDefault ()
	{
		// Default charset AlphanumericDashUnderscore accepts _ and -.
		new IdString("abc_def").Value.Should().Be("abc_def");
		new IdString("abc-def").Value.Should().Be("abc-def");
	}

	[Test]
	public void Constructor_OtherSpecialChars_RejectedByDefault ()
	{
		// Default charset AlphanumericDashUnderscore rejects other characters.
		((Func<IdString>)(() => new IdString("abc/def"))).Should().Throw<FormatException>();
		((Func<IdString>)(() => new IdString("abc.def"))).Should().Throw<FormatException>();
		((Func<IdString>)(() => new IdString("abc:def"))).Should().Throw<FormatException>();
		((Func<IdString>)(() => new IdString("abc@def"))).Should().Throw<FormatException>();
	}

	// ═════ ToString ══════════════════════════════════════════════════════════

	[Test]
	public void ToString_ReturnsValue ()
	{
		new IdString("abc123").ToString().Should().Be("abc123");
	}

	[Test]
	public void ToString_FormatC_SameAsDefault ()
	{
		new IdString("abc123").ToString("C").Should().Be("abc123");
	}

	[Test]
	public void ToString_FormatB_SameAsDefault ()
	{
		new IdString("abc123").ToString("B").Should().Be("abc123");
	}

	[Test]
	public void ToString_InvalidFormat_Throws ()
	{
		var id = new IdString("abc");
		var act = () => id.ToString("Z");
		act.Should().Throw<FormatException>();
	}

	// ═════ Parse ═════════════════════════════════════════════════════════════

	[Test]
	public void Parse_SimpleString_Succeeds ()
	{
		IdString.Parse("abc123").Value.Should().Be("abc123");
	}

	[Test]
	public void Parse_Empty_Throws ()
	{
		var act = () => IdString.Parse("");
		act.Should().Throw<FormatException>().WithMessage("*empty*");
	}

	[Test]
	public void Parse_Whitespace_Throws ()
	{
		var act = () => IdString.Parse("abc def");
		act.Should().Throw<FormatException>();
	}

	[Test]
	public void TryParse_Valid_ReturnsTrue ()
	{
		IdString.TryParse("abc", out var id).Should().BeTrue();
		id.Value.Should().Be("abc");
	}

	[Test]
	public void TryParse_Null_ReturnsFalse ()
	{
		IdString.TryParse((string?)null, out _).Should().BeFalse();
	}

	[Test]
	public void TryParse_Invalid_ReturnsFalse ()
	{
		IdString.TryParse("has spaces", out _).Should().BeFalse();
	}

	// ═════ Round-trip ════════════════════════════════════════════════════════

	[Test]
	public void RoundTrip_CleanSuffix ()
	{
		var original = new IdString("L8x9Kq4YZ");
		IdString.Parse(original.ToString()).Should().Be(original);
	}

	[Test]
	public void Constructor_ThirdPartyIdWithDashOrUnderscore_AcceptedByDefault ()
	{
		// Default charset AlphanumericDashUnderscore accepts dashes and underscores.
		new IdString("cus_L8x9Kq4YZ").Value.Should().Be("cus_L8x9Kq4YZ");
		new IdString("my-item-42").Value.Should().Be("my-item-42");
	}

	// ═════ Comparison ════════════════════════════════════════════════════════

	[Test]
	public void CompareTo_OrdinalOrder ()
	{
		var a = new IdString("aaa");
		var b = new IdString("bbb");
		a.CompareTo(b).Should().BeNegative();
		b.CompareTo(a).Should().BePositive();
		a.CompareTo(a).Should().Be(0);
	}

	[Test]
	public void CompareTo_NullObject_IsPositive ()
	{
		new IdString("abc").CompareTo((object?)null).Should().BePositive();
	}

	[Test]
	public void CompareTo_WrongType_Throws ()
	{
		var act = () => new IdString("abc").CompareTo(42);
		act.Should().Throw<ArgumentException>();
	}

	// ═════ Equality ══════════════════════════════════════════════════════════

	[Test]
	public void Equals_SameValue_True ()
	{
		var a = new IdString("abc");
		var b = new IdString("abc");
		a.Should().Be(b);
		a.GetHashCode().Should().Be(b.GetHashCode());
	}

	[Test]
	public void Equals_DifferentCase_FalseByDefault ()
	{
		// Non-generic IdString is case-sensitive by default.
		var a = new IdString("Abc");
		var b = new IdString("abc");
		a.Should().NotBe(b);
	}

	// ═════ Operators ═════════════════════════════════════════════════════════

	[Test]
	public void Operator_ImplicitFromString ()
	{
		IdString id = "abc123";
		id.Value.Should().Be("abc123");
	}

	[Test]
	public void Operator_ImplicitFromInvalidString_Throws ()
	{
		var act = () =>
		{
			IdString _ = "has spaces";
		};
		act.Should().Throw<FormatException>();
	}

	[Test]
	public void Operator_ExplicitToString ()
	{
		var id = new IdString("abc123");
		((string)id).Should().Be("abc123");
	}

	// ═════ TryFormat ═════════════════════════════════════════════════════════

	[Test]
	public void TryFormatChar_Sufficient_Succeeds ()
	{
		var id = new IdString("hello");
		Span<char> buffer = stackalloc char[10];
		id.TryFormat(buffer, out var written, default, null).Should().BeTrue();
		written.Should().Be(5);
		buffer[..written].ToString().Should().Be("hello");
	}

	[Test]
	public void TryFormatChar_BufferTooSmall_False ()
	{
		var id = new IdString("abcdefghij");
		Span<char> buffer = stackalloc char[5];
		id.TryFormat(buffer, out var written, default, null).Should().BeFalse();
		written.Should().Be(0);
	}

	[Test]
	public void TryFormatChar_DefaultInstance_WritesZero ()
	{
		var id = default(IdString);
		Span<char> buffer = stackalloc char[10];
		id.TryFormat(buffer, out var written, default, null).Should().BeTrue();
		written.Should().Be(0);
	}

	[Test]
	public void TryFormatByte_WritesUtf8 ()
	{
		var id = new IdString("abc");
		Span<byte> buffer = stackalloc byte[10];
		id.TryFormat(buffer, out var written, default, null).Should().BeTrue();
		written.Should().Be(3);
		System.Text.Encoding.ASCII.GetString(buffer[..written]).Should().Be("abc");
	}
}
