using FluentAssertions;

namespace StrictId.Test.Ids;

[TestFixture]
public class IdStringOfTTests
{
	// ───── Test entity types ─────────────────────────────────────────────────

	private class NoPrefix;

	[IdPrefix("cus")]
	[IdString(MaxLength = 32, CharSet = IdStringCharSet.AlphanumericUnderscore)]
	private class Customer;

	[IdPrefix("sku")]
	[IdString(MaxLength = 20, CharSet = IdStringCharSet.AlphanumericDash)]
	private class Sku;

	[IdPrefix("slug")]
	[IdString(MaxLength = 64, CharSet = IdStringCharSet.AlphanumericDash, IgnoreCase = true)]
	private class Slug;

	[IdPrefix("ref", IsDefault = true)]
	[IdPrefix("r")]
	[IdString(MaxLength = 10, CharSet = IdStringCharSet.Alphanumeric)]
	private class Reference;

	[IdPrefix("tight")]
	[IdString(MaxLength = 5)]
	private class Tight;

	[IdPrefix("wide")]
	[IdString(MaxLength = 512)]
	private class Wide;

	// ═════ Defaults ══════════════════════════════════════════════════════════

	[Test]
	public void Default_HasNullValueAndHasValueFalse ()
	{
		default(IdString<Customer>).Value.Should().BeNull();
		default(IdString<Customer>).HasValue.Should().BeFalse();
	}

	[Test]
	public void Default_ToString_IsEmpty ()
	{
		default(IdString<Customer>).ToString().Should().Be(string.Empty);
	}

	// ═════ Construction and validation ═══════════════════════════════════════

	[Test]
	public void Constructor_FromBareSuffix_Succeeds ()
	{
		var id = new IdString<Customer>("L8x9Kq4YZ");
		id.Value.Should().Be("L8x9Kq4YZ");
	}

	[Test]
	public void Constructor_FromPrefixedForm_StripsPrefix ()
	{
		var id = new IdString<Customer>("cus_L8x9Kq4YZ");
		id.Value.Should().Be("L8x9Kq4YZ");
	}

	[Test]
	public void Constructor_TooLong_Throws ()
	{
		var tooLong = new string('x', 33); // Customer MaxLength is 32
		var act = () => new IdString<Customer>(tooLong);
		act.Should().Throw<FormatException>().WithMessage("*exceeds the maximum of 32*");
	}

	[Test]
	public void Constructor_CharsetViolation_Throws ()
	{
		// Customer uses AlphanumericUnderscore; a dash is not allowed.
		var act = () => new IdString<Customer>("abc-def");
		act.Should().Throw<FormatException>().WithMessage("*AlphanumericUnderscore*");
	}

	[Test]
	public void Constructor_SkuAllowsDash ()
	{
		// Sku uses AlphanumericDash; a dash in the suffix is fine.
		var id = new IdString<Sku>("ABC-123-XYZ");
		id.Value.Should().Be("ABC-123-XYZ");
	}

	[Test]
	public void Constructor_SkuRejectsUnderscore ()
	{
		// Sku does not allow underscore in its AlphanumericDash charset.
		// But the parser will interpret "sku_ABC" as prefix + separator, so test
		// with a bare suffix that contains an underscore.
		var act = () => new IdString<Sku>("ABC-123_xyz");
		act.Should().Throw<FormatException>();
	}

	// ═════ Case sensitivity ══════════════════════════════════════════════════

	[Test]
	public void CaseSensitive_DifferentCasesAreNotEqual ()
	{
		// Customer is case-sensitive (default).
		var a = new IdString<Customer>("AbcDef");
		var b = new IdString<Customer>("abcdef");
		a.Should().NotBe(b);
	}

	[Test]
	public void CaseInsensitive_NormalizesToLowercase ()
	{
		// Slug has IgnoreCase = true; values normalize to lowercase on construction.
		var id = new IdString<Slug>("My-Slug");
		id.Value.Should().Be("my-slug");
	}

	[Test]
	public void CaseInsensitive_DifferentCasesCompareEqual ()
	{
		var a = new IdString<Slug>("Hello-World");
		var b = new IdString<Slug>("hello-world");
		a.Should().Be(b);
		a.GetHashCode().Should().Be(b.GetHashCode());
	}

	// ═════ Parse with prefix ═════════════════════════════════════════════════

	[Test]
	public void Parse_PrefixedCanonical_Succeeds ()
	{
		IdString<Customer>.Parse("cus_L8x9Kq4YZ").Value.Should().Be("L8x9Kq4YZ");
	}

	[Test]
	public void Parse_PrefixedAlias_Succeeds ()
	{
		IdString<Reference>.Parse("ref_ABC123").Value.Should().Be("ABC123");
		IdString<Reference>.Parse("r_ABC123").Value.Should().Be("ABC123");
	}

	[Test]
	public void Parse_PrefixedUppercase_Succeeds ()
	{
		// Prefix matching is always case-insensitive.
		IdString<Customer>.Parse("CUS_abc123").Value.Should().Be("abc123");
	}

	[Test]
	public void Parse_BareSuffix_IsAccepted ()
	{
		// Bare suffix (no prefix) is always accepted even for types with a registered prefix.
		IdString<Customer>.Parse("directValue").Value.Should().Be("directValue");
	}

	[Test]
	public void Parse_SeparatorToleranceOnRead ()
	{
		// Any of the four separators is accepted on parse.
		IdString<Customer>.Parse("cus_abc").Value.Should().Be("abc");
		IdString<Customer>.Parse("cus/abc").Value.Should().Be("abc");
		IdString<Customer>.Parse("cus.abc").Value.Should().Be("abc");
		IdString<Customer>.Parse("cus:abc").Value.Should().Be("abc");
	}

	[Test]
	public void Parse_PrefixSimilarButDifferent_TreatedAsBare ()
	{
		// "cst_abc" doesn't match any registered prefix for Customer, so the
		// whole input is treated as the bare suffix. But "cst_abc" contains an
		// underscore, which is allowed by Customer's AlphanumericUnderscore charset.
		// So this succeeds with Value = "cst_abc".
		IdString<Customer>.Parse("cst_abc").Value.Should().Be("cst_abc");
	}

	[Test]
	public void Parse_Empty_Throws ()
	{
		var act = () => IdString<Customer>.Parse("");
		act.Should().Throw<FormatException>().WithMessage("*empty*");
	}

	[Test]
	public void Parse_LengthOverflow_ThrowsWithVerboseMessage ()
	{
		var tooLong = "cus_" + new string('a', 33);
		var act = () => IdString<Customer>.Parse(tooLong);
		act.Should().Throw<FormatException>()
			.WithMessage("*IdString<Customer>*")
			.WithMessage("*exceeds the maximum of 32*");
	}

	[Test]
	public void Parse_CharsetViolation_ThrowsWithVerboseMessage ()
	{
		var act = () => new IdString<Sku>("badchar$");
		act.Should().Throw<FormatException>();
	}

	// ═════ Longest-match prefix ══════════════════════════════════════════════

	[Test]
	public void Parse_LongestPrefixWins ()
	{
		// Reference has both "ref" and "r" — a longer match should win.
		// "ref_X" should match "ref" (suffix "X"), not "r" (suffix "ef_X" which is invalid).
		var id = IdString<Reference>.Parse("ref_X12");
		id.Value.Should().Be("X12");
	}

	// ═════ Round-trip ════════════════════════════════════════════════════════

	[Test]
	public void RoundTrip_ToStringParse ()
	{
		var original = new IdString<Customer>("cus_L8x9Kq4YZ");
		var roundTripped = IdString<Customer>.Parse(original.ToString());
		roundTripped.Should().Be(original);
		original.ToString().Should().Be("cus_L8x9Kq4YZ");
	}

	[Test]
	public void RoundTrip_CaseInsensitive_NormalizesOnRead ()
	{
		var original = new IdString<Slug>("HELLO-WORLD");
		var roundTripped = IdString<Slug>.Parse(original.ToString());
		roundTripped.Should().Be(original);
		original.ToString().Should().Be("slug_hello-world");
	}

	// ═════ Cross-type safety ═════════════════════════════════════════════════

	[Test]
	public void Equals_CrossType_FalseEvenWithSameValue ()
	{
		var customer = new IdString<Customer>("abc");
		var reference = new IdString<Reference>("abc");
		((object)customer).Equals(reference).Should().BeFalse();
	}

	// ═════ Operators ═════════════════════════════════════════════════════════

	[Test]
	public void Operator_ImplicitFromString ()
	{
		IdString<Customer> id = "cus_abc123";
		id.Value.Should().Be("abc123");
	}

	[Test]
	public void Operator_ImplicitFromNonGenericIdString ()
	{
		IdString nonTyped = "abc";
		IdString<Customer> typed = nonTyped;
		typed.Value.Should().Be("abc");
	}

	[Test]
	public void Operator_ImplicitFromDefaultNonGeneric ()
	{
		IdString<Customer> typed = default(IdString);
		typed.Should().Be(default(IdString<Customer>));
		typed.HasValue.Should().BeFalse();
	}

	[Test]
	public void Operator_ExplicitToNonGeneric ()
	{
		IdString<Customer> typed = "cus_abc";
		IdString nonTyped = (IdString)typed;
		nonTyped.Value.Should().Be("abc");
	}

	[Test]
	public void Operator_ExplicitToString ()
	{
		IdString<Customer> id = "cus_abc123";
		((string)id).Should().Be("cus_abc123");
	}

	// ═════ TryFormat ═════════════════════════════════════════════════════════

	[Test]
	public void TryFormatChar_WithPrefix_WritesCanonical ()
	{
		var id = new IdString<Customer>("cus_abc");
		Span<char> buffer = stackalloc char[20];
		id.TryFormat(buffer, out var written, default, null).Should().BeTrue();
		buffer[..written].ToString().Should().Be("cus_abc");
	}

	[Test]
	public void TryFormatChar_BufferTooSmall_False ()
	{
		var id = new IdString<Customer>("abcdefghij");
		Span<char> buffer = stackalloc char[5]; // not enough for "cus_abcdefghij"
		id.TryFormat(buffer, out var written, default, null).Should().BeFalse();
		written.Should().Be(0);
	}

	[Test]
	public void TryFormatByte_WithPrefix_WritesUtf8 ()
	{
		var id = new IdString<Customer>("abc");
		Span<byte> buffer = stackalloc byte[20];
		id.TryFormat(buffer, out var written, default, null).Should().BeTrue();
		System.Text.Encoding.ASCII.GetString(buffer[..written]).Should().Be("cus_abc");
	}

	// ═════ Helpers ═══════════════════════════════════════════════════════════

	[Test]
	public void ToBareString_StripsPrefix ()
	{
		var typed = new IdString<Customer>("cus_abc123");
		typed.ToString().Should().Be("cus_abc123");
		typed.ToBareString().Should().Be("abc123");
	}

	[Test]
	public void ToBareString_DefaultInstance_IsEmpty ()
	{
		default(IdString<Customer>).ToBareString().Should().Be(string.Empty);
	}

	[Test]
	public void ToIdString_ErasesGenericType ()
	{
		var typed = new IdString<Customer>("abc");
		var erased = typed.ToIdString();
		erased.Value.Should().Be("abc");
	}

	[Test]
	public void ToIdString_DefaultStaysDefault ()
	{
		var typed = default(IdString<Customer>);
		var erased = typed.ToIdString();
		erased.Should().Be(default(IdString));
	}

	[Test]
	public void ToIdString_OverDefaultMaxLength_IsLossless ()
	{
		// Wide's MaxLength is 512 — far above the non-generic IdString default of 255.
		// ToIdString() must bypass the non-generic validator so that type erasure is
		// infallible for an already-validated typed value.
		var big = new string('a', 400);
		var typed = new IdString<Wide>(big);
		typed.Value.Length.Should().Be(400);

		var erased = typed.ToIdString();
		erased.Value.Should().Be(big);
	}

	[Test]
	public void CompareTo_WrongType_Throws ()
	{
		var act = () => new IdString<Customer>("abc").CompareTo(new IdString<Reference>("abc"));
		act.Should().Throw<ArgumentException>();
	}

	// ═════ No-prefix type ════════════════════════════════════════════════════

	[Test]
	public void NoPrefix_UsesDefaultOptions_AndBareForm ()
	{
		var id = new IdString<NoPrefix>("abc123");
		id.ToString().Should().Be("abc123");
	}

	[Test]
	public void NoPrefix_RejectsSeparatorCharInValue ()
	{
		// NoPrefix has no [IdString] attribute → defaults (Any charset, excludes separators).
		var act = () => new IdString<NoPrefix>("abc_def");
		act.Should().Throw<FormatException>();
	}
}
