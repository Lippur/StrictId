using System.Text.Json;
using FluentAssertions;

namespace StrictId.Test.Json;

[TestFixture]
public class GuidJsonConverterTests
{
	[IdPrefix("user")]
	private class User;

	private class NoPrefix;

	private static readonly Guid SampleGuid = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");

	// ═════ Serialize / Deserialize ═══════════════════════════════════════════

	[Test]
	public void Serialize_WithPrefix_WritesCanonicalForm ()
	{
		var id = new Guid<User>(SampleGuid);
		var json = JsonSerializer.Serialize(id);
		json.Should().Be("\"user_550e8400-e29b-41d4-a716-446655440000\"");
	}

	[Test]
	public void Serialize_WithoutPrefix_WritesBareDFormat ()
	{
		var id = new Guid<NoPrefix>(SampleGuid);
		var json = JsonSerializer.Serialize(id);
		json.Should().Be("\"550e8400-e29b-41d4-a716-446655440000\"");
	}

	[Test]
	public void Deserialize_WithPrefix_ParsesPrefixed ()
	{
		var json = "\"user_550e8400-e29b-41d4-a716-446655440000\"";
		var result = JsonSerializer.Deserialize<Guid<User>>(json);
		result.Value.Should().Be(SampleGuid);
	}

	[Test]
	public void Deserialize_BareGuid_ParsedForPrefixedType ()
	{
		var json = "\"550e8400-e29b-41d4-a716-446655440000\"";
		var result = JsonSerializer.Deserialize<Guid<User>>(json);
		result.Value.Should().Be(SampleGuid);
	}

	[Test]
	public void RoundTrip_PreservesValue ()
	{
		var original = Guid<User>.NewId();
		var json = JsonSerializer.Serialize(original);
		var deserialized = JsonSerializer.Deserialize<Guid<User>>(json);
		deserialized.Should().Be(original);
	}

	[Test]
	public void Deserialize_Null_ReturnsDefault ()
	{
		var json = "null";
		var result = JsonSerializer.Deserialize<Guid<User>?>(json);
		result.Should().BeNull();
	}

	// ═════ Dictionary key (PropertyName) ════════════════════════════════════

	[Test]
	public void DictionaryKey_RoundTrip ()
	{
		var key = Guid<User>.NewId();
		var dict = new Dictionary<Guid<User>, string> { [key] = "test" };
		var json = JsonSerializer.Serialize(dict);
		var deserialized = JsonSerializer.Deserialize<Dictionary<Guid<User>, string>>(json);
		deserialized.Should().ContainKey(key);
		deserialized![key].Should().Be("test");
	}

	[Test]
	public void Deserialize_WrongTokenType_ThrowsJsonException ()
	{
		var json = "42";
		var act = () => JsonSerializer.Deserialize<Guid<User>>(json);
		act.Should().Throw<JsonException>();
	}
}
