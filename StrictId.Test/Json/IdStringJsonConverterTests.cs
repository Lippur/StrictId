using System.Text.Json;
using FluentAssertions;

namespace StrictId.Test.Json;

[TestFixture]
public class IdStringJsonConverterTests
{
	// ───── Test entity types ─────────────────────────────────────────────────

	private class NoPrefix;

	[IdPrefix("cus")]
	[IdString(MaxLength = 32, CharSet = IdStringCharSet.AlphanumericUnderscore)]
	private class Customer;

	[IdPrefix("slug")]
	[IdString(MaxLength = 64, CharSet = IdStringCharSet.AlphanumericDash, IgnoreCase = true)]
	private class Slug;

	[IdPrefix("ref", IsDefault = true)]
	[IdPrefix("r")]
	[IdString(MaxLength = 10, CharSet = IdStringCharSet.Alphanumeric)]
	private class Reference;

	// ═════ Non-generic IdString ══════════════════════════════════════════════

	[Test]
	public void IdString_SerializesAsJsonString ()
	{
		var id = new IdString("abc123");
		JsonSerializer.Serialize(id).Should().Be("\"abc123\"");
	}

	[Test]
	public void IdString_RoundTrip ()
	{
		var original = new IdString("L8x9Kq4YZ");
		var json = JsonSerializer.Serialize(original);
		JsonSerializer.Deserialize<IdString>(json).Should().Be(original);
	}

	[Test]
	public void IdString_DefaultSerializesAsEmptyString ()
	{
		JsonSerializer.Serialize(default(IdString)).Should().Be("\"\"");
	}

	[Test]
	public void IdString_EmptyStringDeserializesToDefault ()
	{
		var roundTripped = JsonSerializer.Deserialize<IdString>("\"\"");
		roundTripped.Should().Be(default(IdString));
		roundTripped.HasValue.Should().BeFalse();
	}

	[Test]
	public void IdString_DefaultRoundTrip ()
	{
		var json = JsonSerializer.Serialize(default(IdString));
		JsonSerializer.Deserialize<IdString>(json).Should().Be(default(IdString));
	}

	[Test]
	public void IdString_DeserializeFromNonStringToken_Throws ()
	{
		var act = () => JsonSerializer.Deserialize<IdString>("42");
		act.Should().Throw<JsonException>();
	}

	[Test]
	public void IdString_DeserializeInvalidString_Throws ()
	{
		// Default 'Any' charset rejects whitespace.
		var act = () => JsonSerializer.Deserialize<IdString>("\"has spaces\"");
		act.Should().Throw<FormatException>();
	}

	// ═════ Dictionary keys (non-generic) ═════════════════════════════════════

	[Test]
	public void IdString_AsDictionaryKey_RoundTrip ()
	{
		var dict = new Dictionary<IdString, int>
		{
			{ new IdString("alpha"), 1 },
			{ new IdString("beta"), 2 },
		};
		var json = JsonSerializer.Serialize(dict);
		var roundTripped = JsonSerializer.Deserialize<Dictionary<IdString, int>>(json);
		roundTripped.Should().Equal(dict);
	}

	// ═════ Typed IdString<T> without prefix ══════════════════════════════════

	[Test]
	public void IdStringOfT_NoPrefix_SerializesAsBareSuffix ()
	{
		var id = new IdString<NoPrefix>("abc123");
		JsonSerializer.Serialize(id).Should().Be("\"abc123\"");
	}

	[Test]
	public void IdStringOfT_NoPrefix_RoundTrip ()
	{
		var original = new IdString<NoPrefix>("xyz789");
		var json = JsonSerializer.Serialize(original);
		JsonSerializer.Deserialize<IdString<NoPrefix>>(json).Should().Be(original);
	}

	// ═════ Typed IdString<T> with prefix ═════════════════════════════════════

	[Test]
	public void IdStringOfT_WithPrefix_SerializesAsPrefixedForm ()
	{
		var id = new IdString<Customer>("L8x9Kq4YZ");
		JsonSerializer.Serialize(id).Should().Be("\"cus_L8x9Kq4YZ\"");
	}

	[Test]
	public void IdStringOfT_WithPrefix_RoundTrip ()
	{
		var original = new IdString<Customer>("cus_ABC_123");
		var json = JsonSerializer.Serialize(original);
		JsonSerializer.Deserialize<IdString<Customer>>(json).Should().Be(original);
	}

	[Test]
	public void IdStringOfT_WithPrefix_DeserializeFromBareForm ()
	{
		// Parse accepts bare suffix even for prefixed types.
		var id = JsonSerializer.Deserialize<IdString<Customer>>("\"L8x9Kq4YZ\"");
		id.Value.Should().Be("L8x9Kq4YZ");
	}

	[Test]
	public void IdStringOfT_WithPrefix_DeserializeFromAlias ()
	{
		var id = JsonSerializer.Deserialize<IdString<Reference>>("\"r_ABC123\"");
		id.Value.Should().Be("ABC123");
	}

	[Test]
	public void IdStringOfT_WithPrefix_SerializesCanonicalPrefixNotAlias ()
	{
		// Reference has canonical 'ref' (IsDefault) and alias 'r' — the canonical form is always written.
		var id = new IdString<Reference>("ABC123");
		JsonSerializer.Serialize(id).Should().Be("\"ref_ABC123\"");
	}

	[Test]
	public void IdStringOfT_CaseInsensitive_NormalizesOnRead ()
	{
		// Slug has IgnoreCase = true; values are lowercased on construction.
		var id = JsonSerializer.Deserialize<IdString<Slug>>("\"slug_Hello-World\"");
		id.Value.Should().Be("hello-world");
	}

	[Test]
	public void IdStringOfT_DefaultSerializesAsEmptyString ()
	{
		JsonSerializer.Serialize(default(IdString<Customer>)).Should().Be("\"\"");
	}

	[Test]
	public void IdStringOfT_EmptyStringDeserializesToDefault ()
	{
		var roundTripped = JsonSerializer.Deserialize<IdString<Customer>>("\"\"");
		roundTripped.Should().Be(default(IdString<Customer>));
		roundTripped.HasValue.Should().BeFalse();
	}

	[Test]
	public void IdStringOfT_DefaultRoundTrip ()
	{
		var json = JsonSerializer.Serialize(default(IdString<Customer>));
		JsonSerializer.Deserialize<IdString<Customer>>(json).Should().Be(default(IdString<Customer>));
	}

	[Test]
	public void IdStringOfT_DeserializeInvalid_Throws ()
	{
		// Customer uses AlphanumericUnderscore; a dash is not allowed.
		var act = () => JsonSerializer.Deserialize<IdString<Customer>>("\"abc-def\"");
		act.Should().Throw<FormatException>();
	}

	[Test]
	public void IdStringOfT_DeserializeFromNonStringToken_Throws ()
	{
		var act = () => JsonSerializer.Deserialize<IdString<Customer>>("42");
		act.Should().Throw<JsonException>();
	}

	// ═════ Dictionary keys (typed) ═══════════════════════════════════════════

	[Test]
	public void IdStringOfT_AsDictionaryKey_RoundTrip ()
	{
		var dict = new Dictionary<IdString<Customer>, string>
		{
			{ new IdString<Customer>("alpha"), "a" },
			{ new IdString<Customer>("beta"), "b" },
		};
		var json = JsonSerializer.Serialize(dict);
		var roundTripped = JsonSerializer.Deserialize<Dictionary<IdString<Customer>, string>>(json);
		roundTripped.Should().Equal(dict);
	}

	[Test]
	public void IdStringOfT_WithPrefix_DictionaryKey_JsonContainsPrefix ()
	{
		var dict = new Dictionary<IdString<Customer>, int> { { new IdString<Customer>("alpha"), 1 } };
		JsonSerializer.Serialize(dict).Should().Contain("cus_alpha");
	}

	// ═════ As a property on a DTO ════════════════════════════════════════════

	private record CustomerDto (IdString<Customer> Id, string Name);

	[Test]
	public void IdStringOfT_AsDtoProperty_RoundTrip ()
	{
		var original = new CustomerDto(new IdString<Customer>("alpha_1"), "Alice");
		var json = JsonSerializer.Serialize(original);
		var roundTripped = JsonSerializer.Deserialize<CustomerDto>(json);
		roundTripped.Should().Be(original);
		json.Should().Contain("cus_alpha_1");
	}

	[Test]
	public void IdStringOfT_AsDtoProperty_DefaultRoundTrip ()
	{
		// Default IdString<T> property value serializes as empty string, reads back as default.
		var original = new CustomerDto(default, "Alice");
		var json = JsonSerializer.Serialize(original);
		var roundTripped = JsonSerializer.Deserialize<CustomerDto>(json);
		roundTripped.Should().Be(original);
		roundTripped!.Id.HasValue.Should().BeFalse();
	}
}
