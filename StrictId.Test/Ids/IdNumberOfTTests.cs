using FluentAssertions;

namespace StrictId.Test.Ids;

[TestFixture]
public class IdNumberOfTTests
{
	// ───── Test entity types ─────────────────────────────────────────────────

	private class NoPrefix;

	[IdPrefix("invoice")]
	private class Invoice;

	[IdPrefix("account", IsDefault = true)]
	[IdPrefix("acct")]
	[IdPrefix("a")]
	private class Account;

	[IdPrefix("widget")]
	[IdSeparator(IdSeparator.Colon)]
	private class Widget;

	// ═════ Defaults ══════════════════════════════════════════════════════════

	[Test]
	public void Empty_IsZero ()
	{
		IdNumber<Invoice>.Empty.Value.Should().Be(0UL);
		IdNumber<Invoice>.Empty.HasValue.Should().BeFalse();
	}

	// ═════ ToString with prefix ══════════════════════════════════════════════

	[Test]
	public void ToString_NoPrefix_IsBareDigits ()
	{
		new IdNumber<NoPrefix>(42UL).ToString().Should().Be("42");
	}

	[Test]
	public void ToString_WithPrefix_IsPrefixed ()
	{
		new IdNumber<Invoice>(42UL).ToString().Should().Be("invoice_42");
	}

	[Test]
	public void ToString_WithCanonicalSelection_UsesDefault ()
	{
		new IdNumber<Account>(42UL).ToString().Should().Be("account_42");
	}

	[Test]
	public void ToString_WithColonSeparator ()
	{
		new IdNumber<Widget>(42UL).ToString().Should().Be("widget:42");
	}

	[Test]
	public void ToString_FormatB_StripsPrefix ()
	{
		new IdNumber<Invoice>(42UL).ToString("B").Should().Be("42");
	}

	[Test]
	public void ToString_FormatC_IsCanonicalPrefixed ()
	{
		new IdNumber<Invoice>(42UL).ToString("C").Should().Be("invoice_42");
	}

	// ═════ Parse with prefix ═════════════════════════════════════════════════

	[Test]
	public void Parse_BareDigits_AlwaysAccepted ()
	{
		IdNumber<Invoice>.Parse("42").Value.Should().Be(42UL);
	}

	[Test]
	public void Parse_PrefixedCanonical_Succeeds ()
	{
		IdNumber<Invoice>.Parse("invoice_42").Value.Should().Be(42UL);
	}

	[Test]
	public void Parse_PrefixedAlias_Succeeds ()
	{
		IdNumber<Account>.Parse("acct_42").Value.Should().Be(42UL);
		IdNumber<Account>.Parse("a_42").Value.Should().Be(42UL);
	}

	[Test]
	public void Parse_PrefixedUppercase_Succeeds ()
	{
		IdNumber<Invoice>.Parse("INVOICE_42").Value.Should().Be(42UL);
	}

	[Test]
	public void Parse_SeparatorToleranceOnRead ()
	{
		// Widget declares Colon as canonical; all four separators must parse.
		IdNumber<Widget>.Parse("widget:42").Value.Should().Be(42UL);
		IdNumber<Widget>.Parse("widget_42").Value.Should().Be(42UL);
		IdNumber<Widget>.Parse("widget/42").Value.Should().Be(42UL);
		IdNumber<Widget>.Parse("widget.42").Value.Should().Be(42UL);
	}

	[Test]
	public void Parse_UnknownPrefix_Throws ()
	{
		var act = () => IdNumber<Invoice>.Parse("inv_42");
		act.Should().Throw<FormatException>()
			.WithMessage("*IdNumber<Invoice>*")
			.WithMessage("*inv*")
			.WithMessage("*not registered*");
	}

	[Test]
	public void Parse_PrefixedOnTypeWithoutPrefix_Throws ()
	{
		var act = () => IdNumber<NoPrefix>.Parse("something_42");
		act.Should().Throw<FormatException>()
			.WithMessage("*no registered prefix*");
	}

	[Test]
	public void Parse_InvalidSeparator_Throws ()
	{
		var act = () => IdNumber<Invoice>.Parse("invoice-42");
		act.Should().Throw<FormatException>();
	}

	// ═════ IdFormat.RequirePrefix ════════════════════════════════════════════

	[Test]
	public void Parse_RequirePrefix_AcceptsPrefixed ()
	{
		var id = IdNumber<Invoice>.Parse("invoice_42", IdFormat.RequirePrefix);
		id.Value.Should().Be(42UL);
	}

	[Test]
	public void Parse_RequirePrefix_AcceptsAlias ()
	{
		var id = IdNumber<Account>.Parse("acct_99", IdFormat.RequirePrefix);
		id.Value.Should().Be(99UL);
	}

	[Test]
	public void Parse_RequirePrefix_RejectsBareDigits ()
	{
		var act = () => IdNumber<Invoice>.Parse("42", IdFormat.RequirePrefix);
		act.Should().Throw<FormatException>()
			.WithMessage("*bare decimal digits*prefix is required*");
	}

	[Test]
	public void TryParse_RequirePrefix_ReturnsFalseForBare ()
	{
		IdNumber<Invoice>.TryParse("42", IdFormat.RequirePrefix, out _).Should().BeFalse();
	}

	[Test]
	public void TryParse_RequirePrefix_ReturnsTrueForPrefixed ()
	{
		IdNumber<Invoice>.TryParse("invoice_42", IdFormat.RequirePrefix, out var id).Should().BeTrue();
		id.Value.Should().Be(42UL);
	}

	[Test]
	public void Parse_RequirePrefix_NoPrefixRegistered_AcceptsBare ()
	{
		var id = IdNumber<NoPrefix>.Parse("42", IdFormat.RequirePrefix);
		id.Value.Should().Be(42UL);
	}

	[Test]
	public void TryParse_RequirePrefix_NoPrefixRegistered_AcceptsBare ()
	{
		IdNumber<NoPrefix>.TryParse("42", IdFormat.RequirePrefix, out var id).Should().BeTrue();
		id.Value.Should().Be(42UL);
	}

	[Test]
	public void Parse_NullProvider_SameAsLenient ()
	{
		var id = IdNumber<Invoice>.Parse("42", null);
		id.Value.Should().Be(42UL);
	}

	// ═════ Round-trip ════════════════════════════════════════════════════════

	[Test]
	public void RoundTrip_ToStringParse ()
	{
		var original = new IdNumber<Invoice>(99999UL);
		IdNumber<Invoice>.Parse(original.ToString()).Should().Be(original);
	}

	[Test]
	public void RoundTrip_AllFormatSpecifiers ()
	{
		var original = new IdNumber<Invoice>(777UL);
		foreach (var format in new[] { "C", "B" })
		{
			var formatted = original.ToString(format);
			var parsed = IdNumber<Invoice>.Parse(formatted);
			parsed.Should().Be(original, because: $"format '{format}' should round-trip");
		}
	}

	// ═════ Cross-type safety ═════════════════════════════════════════════════

	[Test]
	public void Equals_CrossType_FalseEvenWithSameValue ()
	{
		var invoice = new IdNumber<Invoice>(42UL);
		var account = new IdNumber<Account>(42UL);

		((object)invoice).Equals(account).Should().BeFalse();
	}

	[Test]
	public void Equals_SameType_TrueWithSameValue ()
	{
		var a = new IdNumber<Invoice>(42UL);
		var b = new IdNumber<Invoice>(42UL);
		a.Should().Be(b);
		a.GetHashCode().Should().Be(b.GetHashCode());
	}

	// ═════ Operators ═════════════════════════════════════════════════════════

	[Test]
	public void Operator_ImplicitFromUlong ()
	{
		IdNumber<Invoice> n = 42UL;
		n.Value.Should().Be(42UL);
	}

	[Test]
	public void Operator_ImplicitFromLong ()
	{
		IdNumber<Invoice> n = 42L;
		n.Value.Should().Be(42UL);
	}

	[Test]
	public void Operator_ImplicitFromUInt ()
	{
		IdNumber<Invoice> n = 42U;
		n.Value.Should().Be(42UL);
	}

	[Test]
	public void Operator_ImplicitFromInt ()
	{
		IdNumber<Invoice> n = 42;
		n.Value.Should().Be(42UL);
	}

	[Test]
	public void Operator_ImplicitFromUShort ()
	{
		IdNumber<Invoice> n = (ushort)42;
		n.Value.Should().Be(42UL);
	}

	[Test]
	public void Operator_ImplicitFromShort ()
	{
		IdNumber<Invoice> n = (short)42;
		n.Value.Should().Be(42UL);
	}

	[Test]
	public void Operator_ImplicitFromByte ()
	{
		IdNumber<Invoice> n = (byte)42;
		n.Value.Should().Be(42UL);
	}

	[Test]
	public void Operator_ImplicitFromSByte ()
	{
		IdNumber<Invoice> n = (sbyte)42;
		n.Value.Should().Be(42UL);
	}

	[Test]
	public void Operator_ImplicitFromNegativeInt_Throws ()
	{
		var act = () =>
		{
			IdNumber<Invoice> _ = -1;
		};
		act.Should().Throw<OverflowException>();
	}

	[Test]
	public void Operator_ImplicitFromNegativeLong_Throws ()
	{
		var act = () =>
		{
			IdNumber<Invoice> _ = -1L;
		};
		act.Should().Throw<OverflowException>();
	}

	[Test]
	public void Operator_ImplicitFromNonGenericIdNumber ()
	{
		IdNumber nonTyped = 42UL;
		IdNumber<Invoice> typed = nonTyped;
		typed.Value.Should().Be(42UL);
	}

	[Test]
	public void Operator_ExplicitToNonGenericIdNumber ()
	{
		IdNumber<Invoice> typed = 42UL;
		IdNumber nonTyped = (IdNumber)typed;
		nonTyped.Value.Should().Be(42UL);
	}

	[Test]
	public void Operator_ExplicitFromString ()
	{
		var n = (IdNumber<Invoice>)"invoice_42";
		n.Value.Should().Be(42UL);
	}

	[Test]
	public void Operator_ExplicitToUlong ()
	{
		IdNumber<Invoice> n = 42UL;
		((ulong)n).Should().Be(42UL);
	}

	[Test]
	public void Operator_ExplicitToLong ()
	{
		IdNumber<Invoice> n = 42UL;
		((long)n).Should().Be(42L);
	}

	[Test]
	public void Operator_ExplicitToString ()
	{
		IdNumber<Invoice> n = 42UL;
		((string)n).Should().Be("invoice_42");
	}

	// ═════ TryFormat ═════════════════════════════════════════════════════════

	[Test]
	public void TryFormatChar_WithPrefix_WritesCanonical ()
	{
		var n = new IdNumber<Invoice>(42UL);
		Span<char> buffer = stackalloc char[20];
		n.TryFormat(buffer, out var written, default, null).Should().BeTrue();
		buffer[..written].ToString().Should().Be("invoice_42");
	}

	[Test]
	public void TryFormatByte_WithPrefix_WritesUtf8 ()
	{
		var n = new IdNumber<Invoice>(42UL);
		Span<byte> buffer = stackalloc byte[20];
		n.TryFormat(buffer, out var written, default, null).Should().BeTrue();
		System.Text.Encoding.ASCII.GetString(buffer[..written]).Should().Be("invoice_42");
	}

	// ═════ Helpers ═══════════════════════════════════════════════════════════

	[Test]
	public void ToIdNumber_ErasesGenericType ()
	{
		IdNumber<Invoice> typed = 42UL;
		var erased = typed.ToIdNumber();
		erased.Value.Should().Be(typed.Value);
	}

	[Test]
	public void CompareTo_WrongType_Throws ()
	{
		var act = () => new IdNumber<Invoice>(1UL).CompareTo(new IdNumber<Account>(1UL));
		act.Should().Throw<ArgumentException>();
	}
}
