using FluentAssertions;

namespace StrictId.Test.Ids;

[TestFixture]
public class IdNumberTests
{
	// ═════ Defaults ══════════════════════════════════════════════════════════

	[Test]
	public void Empty_IsZero ()
	{
		IdNumber.Empty.Value.Should().Be(0UL);
		IdNumber.Empty.HasValue.Should().BeFalse();
	}

	[Test]
	public void MinValue_IsZero ()
	{
		IdNumber.MinValue.Should().Be(IdNumber.Empty);
	}

	[Test]
	public void MaxValue_IsUlongMaxValue ()
	{
		IdNumber.MaxValue.Value.Should().Be(ulong.MaxValue);
	}

	[Test]
	public void HasValue_TrueForNonZero ()
	{
		new IdNumber(1).HasValue.Should().BeTrue();
	}

	// ═════ Constructors ══════════════════════════════════════════════════════

	[Test]
	public void Constructor_FromUlong ()
	{
		new IdNumber(42UL).Value.Should().Be(42UL);
	}

	[Test]
	public void Constructor_FromLong ()
	{
		new IdNumber(42L).Value.Should().Be(42UL);
	}

	[Test]
	public void Constructor_FromInt ()
	{
		new IdNumber(42).Value.Should().Be(42UL);
	}

	[Test]
	public void Constructor_FromNegativeLong_Throws ()
	{
		var act = () => new IdNumber(-1L);
		act.Should().Throw<OverflowException>();
	}

	[Test]
	public void Constructor_FromNegativeInt_Throws ()
	{
		var act = () => new IdNumber(-1);
		act.Should().Throw<OverflowException>();
	}

	[Test]
	public void Constructor_FromString_Parses ()
	{
		new IdNumber("42").Value.Should().Be(42UL);
	}

	[Test]
	public void Constructor_FromInvalidString_Throws ()
	{
		var act = () => new IdNumber("not a number");
		act.Should().Throw<FormatException>();
	}

	// ═════ ToString ══════════════════════════════════════════════════════════

	[Test]
	public void ToString_IsPlainDigits ()
	{
		new IdNumber(42UL).ToString().Should().Be("42");
	}

	[Test]
	public void ToString_Zero ()
	{
		IdNumber.Empty.ToString().Should().Be("0");
	}

	[Test]
	public void ToString_MaxUlong_IsAllDigits ()
	{
		IdNumber.MaxValue.ToString().Should().Be("18446744073709551615");
	}

	[Test]
	public void ToString_FormatC_SameAsDefault ()
	{
		new IdNumber(100UL).ToString("C").Should().Be("100");
	}

	[Test]
	public void ToString_FormatB_SameAsDefault_ForNonGeneric ()
	{
		new IdNumber(100UL).ToString("B").Should().Be("100");
	}

	[Test]
	public void ToString_InvalidFormat_Throws ()
	{
		var act = () => new IdNumber(1UL).ToString("X");
		act.Should().Throw<FormatException>();
	}

	// ═════ Parse ═════════════════════════════════════════════════════════════

	[Test]
	public void Parse_BareDigits_Succeeds ()
	{
		IdNumber.Parse("42").Value.Should().Be(42UL);
	}

	[Test]
	public void Parse_Zero_Succeeds ()
	{
		IdNumber.Parse("0").Value.Should().Be(0UL);
	}

	[Test]
	public void Parse_MaxUlong_Succeeds ()
	{
		IdNumber.Parse("18446744073709551615").Value.Should().Be(ulong.MaxValue);
	}

	[Test]
	public void Parse_Overflow_Throws ()
	{
		var act = () => IdNumber.Parse("18446744073709551616");
		act.Should().Throw<FormatException>()
			.WithMessage("*out of range*");
	}

	[Test]
	public void Parse_Negative_Throws ()
	{
		var act = () => IdNumber.Parse("-42");
		act.Should().Throw<FormatException>();
	}

	[Test]
	public void Parse_Empty_Throws ()
	{
		var act = () => IdNumber.Parse("");
		act.Should().Throw<FormatException>()
			.WithMessage("*empty*");
	}

	[Test]
	public void Parse_TrailingNonDigit_Throws ()
	{
		var act = () => IdNumber.Parse("42abc");
		act.Should().Throw<FormatException>()
			.WithMessage("*does not end in a decimal digit*");
	}

	[Test]
	public void Parse_PrefixedInput_ThrowsForNonGeneric ()
	{
		var act = () => IdNumber.Parse("invoice_42");
		act.Should().Throw<FormatException>()
			.WithMessage("*no registered prefix*");
	}

	[Test]
	public void TryParse_Valid_True ()
	{
		IdNumber.TryParse("42", out var n).Should().BeTrue();
		n.Value.Should().Be(42UL);
	}

	[Test]
	public void TryParse_Null_False ()
	{
		IdNumber.TryParse((string?)null, out _).Should().BeFalse();
	}

	[Test]
	public void TryParse_Invalid_False ()
	{
		IdNumber.TryParse("abc", out _).Should().BeFalse();
	}

	[Test]
	public void IsValid_True ()
	{
		IdNumber.IsValid("42").Should().BeTrue();
	}

	[Test]
	public void IsValid_False ()
	{
		IdNumber.IsValid("abc").Should().BeFalse();
	}

	// ═════ Round-trip ════════════════════════════════════════════════════════

	[Test]
	public void RoundTrip_ToStringParse ()
	{
		var original = new IdNumber(42424242UL);
		var roundTripped = IdNumber.Parse(original.ToString());
		roundTripped.Should().Be(original);
	}

	// ═════ Comparison ════════════════════════════════════════════════════════

	[Test]
	public void CompareTo_Lower_IsNegative ()
	{
		new IdNumber(1UL).CompareTo(new IdNumber(2UL)).Should().BeNegative();
	}

	[Test]
	public void CompareTo_SameValue_IsZero ()
	{
		new IdNumber(5UL).CompareTo(new IdNumber(5UL)).Should().Be(0);
	}

	[Test]
	public void CompareTo_NullObject_IsPositive ()
	{
		new IdNumber(1UL).CompareTo(null).Should().BePositive();
	}

	[Test]
	public void CompareTo_WrongType_Throws ()
	{
		var act = () => new IdNumber(1UL).CompareTo("not an id");
		act.Should().Throw<ArgumentException>();
	}

	// ═════ Operators ═════════════════════════════════════════════════════════

	[Test]
	public void Operator_ImplicitFromUlong ()
	{
		IdNumber n = 42UL;
		n.Value.Should().Be(42UL);
	}

	[Test]
	public void Operator_ImplicitFromLong ()
	{
		IdNumber n = 42L;
		n.Value.Should().Be(42UL);
	}

	[Test]
	public void Operator_ImplicitFromUInt ()
	{
		IdNumber n = 42U;
		n.Value.Should().Be(42UL);
	}

	[Test]
	public void Operator_ImplicitFromInt ()
	{
		IdNumber n = 42;
		n.Value.Should().Be(42UL);
	}

	[Test]
	public void Operator_ImplicitFromUShort ()
	{
		IdNumber n = (ushort)42;
		n.Value.Should().Be(42UL);
	}

	[Test]
	public void Operator_ImplicitFromShort ()
	{
		IdNumber n = (short)42;
		n.Value.Should().Be(42UL);
	}

	[Test]
	public void Operator_ImplicitFromByte ()
	{
		IdNumber n = (byte)42;
		n.Value.Should().Be(42UL);
	}

	[Test]
	public void Operator_ImplicitFromSByte ()
	{
		IdNumber n = (sbyte)42;
		n.Value.Should().Be(42UL);
	}

	[Test]
	public void Operator_ImplicitFromNegativeInt_Throws ()
	{
		var act = () =>
		{
			IdNumber _ = -1;
		};
		act.Should().Throw<OverflowException>();
	}

	[Test]
	public void Operator_ImplicitFromNegativeShort_Throws ()
	{
		var act = () =>
		{
			IdNumber _ = (short)-1;
		};
		act.Should().Throw<OverflowException>();
	}

	[Test]
	public void Operator_ImplicitFromNegativeSByte_Throws ()
	{
		var act = () =>
		{
			IdNumber _ = (sbyte)-1;
		};
		act.Should().Throw<OverflowException>();
	}

	[Test]
	public void Operator_ExplicitFromString ()
	{
		var n = (IdNumber)"42";
		n.Value.Should().Be(42UL);
	}

	[Test]
	public void Operator_ExplicitToUlong ()
	{
		var n = new IdNumber(42UL);
		((ulong)n).Should().Be(42UL);
	}

	[Test]
	public void Operator_ExplicitToLong ()
	{
		var n = new IdNumber(42UL);
		((long)n).Should().Be(42L);
	}

	[Test]
	public void Operator_ExplicitToLong_Overflow_Throws ()
	{
		var n = IdNumber.MaxValue;
		var act = () => (long)n;
		act.Should().Throw<OverflowException>();
	}

	[Test]
	public void Operator_ExplicitToString ()
	{
		var n = new IdNumber(42UL);
		((string)n).Should().Be("42");
	}

	// ═════ Helpers ═══════════════════════════════════════════════════════════

	[Test]
	public void ToUInt64_ReturnsValue ()
	{
		new IdNumber(42UL).ToUInt64().Should().Be(42UL);
	}

	[Test]
	public void ToInt64_Overflow_Throws ()
	{
		var act = () => IdNumber.MaxValue.ToInt64();
		act.Should().Throw<OverflowException>();
	}

	// ═════ TryFormat ═════════════════════════════════════════════════════════

	[Test]
	public void TryFormatChar_Sufficient_Succeeds ()
	{
		var n = new IdNumber(42UL);
		Span<char> buffer = stackalloc char[10];
		n.TryFormat(buffer, out var written, default, null).Should().BeTrue();
		written.Should().Be(2);
		buffer[..written].ToString().Should().Be("42");
	}

	[Test]
	public void TryFormatChar_BufferTooSmall_False ()
	{
		var n = IdNumber.MaxValue;
		Span<char> buffer = stackalloc char[10]; // not enough for 20 digits
		n.TryFormat(buffer, out var written, default, null).Should().BeFalse();
		written.Should().Be(0);
	}

	[Test]
	public void TryFormatByte_WritesUtf8Digits ()
	{
		var n = new IdNumber(42UL);
		Span<byte> buffer = stackalloc byte[10];
		n.TryFormat(buffer, out var written, default, null).Should().BeTrue();
		written.Should().Be(2);
		System.Text.Encoding.ASCII.GetString(buffer[..written]).Should().Be("42");
	}
}
