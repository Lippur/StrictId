using System.Text.Json;
using FluentAssertions;

namespace StrictId.Test.Json;

[TestFixture]
public class IdNumberJsonConverterTests
{
	// ───── Test entity types ─────────────────────────────────────────────────

	private class NoPrefix;

	[IdPrefix("inv")]
	private class Invoice;

	[IdPrefix("acct", IsDefault = true)]
	[IdPrefix("a")]
	private class Account;

	// ═════ Non-generic IdNumber ══════════════════════════════════════════════

	[Test]
	public void IdNumber_SerializesAsJsonString ()
	{
		var id = new IdNumber(42);
		JsonSerializer.Serialize(id).Should().Be("\"42\"");
	}

	[Test]
	public void IdNumber_SerializesAsString_NotJsonNumber ()
	{
		// Consistency with the other two families: always a JSON string, never a JSON number.
		// Prevents JavaScript 53-bit truncation for values above 2^53.
		var id = new IdNumber(ulong.MaxValue);
		var json = JsonSerializer.Serialize(id);
		json.Should().StartWith("\"").And.EndWith("\"");
		json.Should().Be("\"18446744073709551615\"");
	}

	[Test]
	public void IdNumber_RoundTrip ()
	{
		var original = new IdNumber(1234567890);
		var json = JsonSerializer.Serialize(original);
		JsonSerializer.Deserialize<IdNumber>(json).Should().Be(original);
	}

	[Test]
	public void IdNumber_DefaultRoundTrip ()
	{
		var json = JsonSerializer.Serialize(default(IdNumber));
		json.Should().Be("\"0\"");
		JsonSerializer.Deserialize<IdNumber>(json).Should().Be(default(IdNumber));
	}

	[Test]
	public void IdNumber_MaxValueRoundTrip ()
	{
		var original = IdNumber.MaxValue;
		var json = JsonSerializer.Serialize(original);
		JsonSerializer.Deserialize<IdNumber>(json).Should().Be(original);
	}

	[Test]
	public void IdNumber_DeserializeFromNonStringToken_Throws ()
	{
		var act = () => JsonSerializer.Deserialize<IdNumber>("42");
		act.Should().Throw<JsonException>();
	}

	[Test]
	public void IdNumber_DeserializeInvalidString_Throws ()
	{
		var act = () => JsonSerializer.Deserialize<IdNumber>("\"not-a-number\"");
		act.Should().Throw<FormatException>();
	}

	// ═════ Dictionary keys (non-generic) ═════════════════════════════════════

	[Test]
	public void IdNumber_AsDictionaryKey_RoundTrip ()
	{
		var dict = new Dictionary<IdNumber, string>
		{
			{ new IdNumber(1), "one" },
			{ new IdNumber(2), "two" },
			{ new IdNumber(ulong.MaxValue), "max" },
		};
		var json = JsonSerializer.Serialize(dict);
		var roundTripped = JsonSerializer.Deserialize<Dictionary<IdNumber, string>>(json);
		roundTripped.Should().Equal(dict);
	}

	// ═════ Typed IdNumber<T> without prefix ══════════════════════════════════

	[Test]
	public void IdNumberOfT_NoPrefix_SerializesAsBareDigits ()
	{
		var id = new IdNumber<NoPrefix>(42);
		JsonSerializer.Serialize(id).Should().Be("\"42\"");
	}

	[Test]
	public void IdNumberOfT_NoPrefix_RoundTrip ()
	{
		var original = new IdNumber<NoPrefix>(1234567890);
		var json = JsonSerializer.Serialize(original);
		JsonSerializer.Deserialize<IdNumber<NoPrefix>>(json).Should().Be(original);
	}

	// ═════ Typed IdNumber<T> with prefix ═════════════════════════════════════

	[Test]
	public void IdNumberOfT_WithPrefix_SerializesAsPrefixedForm ()
	{
		var id = new IdNumber<Invoice>(42);
		JsonSerializer.Serialize(id).Should().Be("\"inv_42\"");
	}

	[Test]
	public void IdNumberOfT_WithPrefix_RoundTrip ()
	{
		var original = new IdNumber<Invoice>(99999);
		var json = JsonSerializer.Serialize(original);
		JsonSerializer.Deserialize<IdNumber<Invoice>>(json).Should().Be(original);
	}

	[Test]
	public void IdNumberOfT_WithPrefix_DeserializeFromBareForm ()
	{
		// IdNumber<T>.Parse accepts bare digits too, not just prefixed; forwarder preserves this.
		var id = JsonSerializer.Deserialize<IdNumber<Invoice>>("\"42\"");
		id.Should().Be(new IdNumber<Invoice>(42));
	}

	[Test]
	public void IdNumberOfT_WithPrefix_DeserializeFromAlias ()
	{
		var id = JsonSerializer.Deserialize<IdNumber<Account>>("\"a_42\"");
		id.Should().Be(new IdNumber<Account>(42));
	}

	[Test]
	public void IdNumberOfT_WithPrefix_SerializesCanonicalPrefixNotAlias ()
	{
		// Account has canonical 'acct' and alias 'a' — the canonical form is always written.
		var id = new IdNumber<Account>(42);
		JsonSerializer.Serialize(id).Should().Be("\"acct_42\"");
	}

	[Test]
	public void IdNumberOfT_DefaultRoundTrip ()
	{
		var json = JsonSerializer.Serialize(default(IdNumber<Invoice>));
		json.Should().Be("\"inv_0\"");
		JsonSerializer.Deserialize<IdNumber<Invoice>>(json).Should().Be(default(IdNumber<Invoice>));
	}

	[Test]
	public void IdNumberOfT_DeserializeFromNonStringToken_Throws ()
	{
		var act = () => JsonSerializer.Deserialize<IdNumber<Invoice>>("42");
		act.Should().Throw<JsonException>();
	}

	// ═════ Dictionary keys (typed) ═══════════════════════════════════════════

	[Test]
	public void IdNumberOfT_AsDictionaryKey_RoundTrip ()
	{
		var dict = new Dictionary<IdNumber<Invoice>, decimal>
		{
			{ new IdNumber<Invoice>(1), 100.00m },
			{ new IdNumber<Invoice>(2), 250.50m },
		};
		var json = JsonSerializer.Serialize(dict);
		var roundTripped = JsonSerializer.Deserialize<Dictionary<IdNumber<Invoice>, decimal>>(json);
		roundTripped.Should().Equal(dict);
	}

	[Test]
	public void IdNumberOfT_WithPrefix_DictionaryKey_JsonContainsPrefix ()
	{
		var dict = new Dictionary<IdNumber<Invoice>, int> { { new IdNumber<Invoice>(42), 1 } };
		JsonSerializer.Serialize(dict).Should().Contain("inv_42");
	}

	// ═════ As a property on a DTO ════════════════════════════════════════════

	private record InvoiceDto (IdNumber<Invoice> Id, decimal Total);

	[Test]
	public void IdNumberOfT_AsDtoProperty_RoundTrip ()
	{
		var original = new InvoiceDto(new IdNumber<Invoice>(100), 1999.99m);
		var json = JsonSerializer.Serialize(original);
		var roundTripped = JsonSerializer.Deserialize<InvoiceDto>(json);
		roundTripped.Should().Be(original);
		json.Should().Contain("inv_100");
	}
}
