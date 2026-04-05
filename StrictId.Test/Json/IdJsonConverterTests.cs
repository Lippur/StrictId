using System.Text.Json;
using FluentAssertions;

namespace StrictId.Test.Json;

[TestFixture]
public class IdJsonConverterTests
{
	// ───── Test entity types ─────────────────────────────────────────────────

	private class NoPrefix;

	[IdPrefix("user")]
	private class User;

	// ═════ Non-generic Id ════════════════════════════════════════════════════

	[Test]
	public void Id_SerializesAsJsonString ()
	{
		var id = Id.Parse("01hv0000000000000000000000");
		JsonSerializer.Serialize(id).Should().Be("\"01hv0000000000000000000000\"");
	}

	[Test]
	public void Id_RoundTrip ()
	{
		var original = Id.NewId();
		var json = JsonSerializer.Serialize(original);
		JsonSerializer.Deserialize<Id>(json).Should().Be(original);
	}

	[Test]
	public void Id_DefaultRoundTrip ()
	{
		var json = JsonSerializer.Serialize(default(Id));
		var roundTripped = JsonSerializer.Deserialize<Id>(json);
		roundTripped.Should().Be(default(Id));
	}

	[Test]
	public void Id_DeserializeFromGuidForm ()
	{
		// Id.Parse accepts bare GUID; the converter should forward to Parse.
		var id = Id.NewId();
		var json = $"\"{id.ToGuid():D}\"";
		JsonSerializer.Deserialize<Id>(json).Should().Be(id);
	}

	[Test]
	public void Id_DeserializeFromNonStringToken_Throws ()
	{
		var act = () => JsonSerializer.Deserialize<Id>("42");
		act.Should().Throw<JsonException>();
	}

	[Test]
	public void Id_DeserializeInvalidString_Throws ()
	{
		var act = () => JsonSerializer.Deserialize<Id>("\"not-an-id\"");
		act.Should().Throw<FormatException>();
	}

	// ═════ Dictionary keys (non-generic) ═════════════════════════════════════

	[Test]
	public void Id_AsDictionaryKey_RoundTrip ()
	{
		var dict = new Dictionary<Id, int> { { Id.NewId(), 1 }, { Id.NewId(), 2 } };
		var json = JsonSerializer.Serialize(dict);
		var roundTripped = JsonSerializer.Deserialize<Dictionary<Id, int>>(json);
		roundTripped.Should().Equal(dict);
	}

	// ═════ Typed Id<T> without prefix ════════════════════════════════════════

	[Test]
	public void IdOfT_NoPrefix_SerializesAsBareUlid ()
	{
		var id = Id<NoPrefix>.Parse("01hv0000000000000000000000");
		JsonSerializer.Serialize(id).Should().Be("\"01hv0000000000000000000000\"");
	}

	[Test]
	public void IdOfT_NoPrefix_RoundTrip ()
	{
		var original = Id<NoPrefix>.NewId();
		var json = JsonSerializer.Serialize(original);
		JsonSerializer.Deserialize<Id<NoPrefix>>(json).Should().Be(original);
	}

	// ═════ Typed Id<T> with prefix ═══════════════════════════════════════════

	[Test]
	public void IdOfT_WithPrefix_SerializesAsPrefixedForm ()
	{
		var id = Id<User>.Parse("user_01hv0000000000000000000000");
		var json = JsonSerializer.Serialize(id);
		json.Should().Be("\"user_01hv0000000000000000000000\"");
	}

	[Test]
	public void IdOfT_WithPrefix_RoundTrip ()
	{
		var original = Id<User>.NewId();
		var json = JsonSerializer.Serialize(original);
		JsonSerializer.Deserialize<Id<User>>(json).Should().Be(original);
	}

	[Test]
	public void IdOfT_WithPrefix_DeserializeFromBareForm ()
	{
		// Parse accepts a bare ULID even for a prefixed type; the converter forwards.
		var bare = "\"01hv0000000000000000000000\"";
		var id = JsonSerializer.Deserialize<Id<User>>(bare);
		id.ToString("B").Should().Be("01hv0000000000000000000000");
	}

	[Test]
	public void IdOfT_DefaultRoundTrip ()
	{
		var json = JsonSerializer.Serialize(default(Id<User>));
		var roundTripped = JsonSerializer.Deserialize<Id<User>>(json);
		roundTripped.Should().Be(default(Id<User>));
	}

	[Test]
	public void IdOfT_DeserializeFromNonStringToken_Throws ()
	{
		var act = () => JsonSerializer.Deserialize<Id<User>>("42");
		act.Should().Throw<JsonException>();
	}

	[Test]
	public void IdOfT_DeserializeFromWrongPrefix_Throws ()
	{
		var act = () => JsonSerializer.Deserialize<Id<User>>("\"order_01hv0000000000000000000000\"");
		act.Should().Throw<FormatException>();
	}

	// ═════ Dictionary keys (typed) ═══════════════════════════════════════════

	[Test]
	public void IdOfT_AsDictionaryKey_RoundTrip ()
	{
		var dict = new Dictionary<Id<User>, string>
		{
			{ Id<User>.NewId(), "alice" },
			{ Id<User>.NewId(), "bob" },
		};
		var json = JsonSerializer.Serialize(dict);
		var roundTripped = JsonSerializer.Deserialize<Dictionary<Id<User>, string>>(json);
		roundTripped.Should().Equal(dict);
	}

	[Test]
	public void IdOfT_WithPrefix_DictionaryKey_JsonContainsPrefix ()
	{
		var id = Id<User>.NewId();
		var dict = new Dictionary<Id<User>, int> { { id, 1 } };
		var json = JsonSerializer.Serialize(dict);
		json.Should().Contain("user_");
	}

	// ═════ As a property on a DTO ════════════════════════════════════════════

	private record UserDto (Id<User> Id, string Name);

	[Test]
	public void IdOfT_AsDtoProperty_RoundTrip ()
	{
		var original = new UserDto(Id<User>.NewId(), "Alice");
		var json = JsonSerializer.Serialize(original);
		var roundTripped = JsonSerializer.Deserialize<UserDto>(json);
		roundTripped.Should().Be(original);
		json.Should().Contain("user_");
	}
}
