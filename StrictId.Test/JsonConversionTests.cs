using System.Text.Json;
using FluentAssertions;

namespace StrictId.Test;

[TestFixture]
public class JsonConversionTests
{
	private class Entity;
	
	[Test]
	public void CanUseIdAsDictionaryKey ()
	{
		var dictionary = new Dictionary<Id, int>
		{
			{ Id.NewId(), 1 },
		};

		var serialized = JsonSerializer.Serialize(dictionary);
		var deserialized = JsonSerializer.Deserialize<Dictionary<Id, int>>(serialized);

		deserialized.Should().Equal(dictionary);
	}
	
	[Test]
	public void CanUseTypedIdAsDictionaryKey ()
	{
		var dictionary = new Dictionary<Id<Entity>, int>
		{
			{ Id<Entity>.NewId(), 1 },
		};

		var serialized = JsonSerializer.Serialize(dictionary);
		var deserialized = JsonSerializer.Deserialize<Dictionary<Id<Entity>, int>>(serialized);

		deserialized.Should().Equal(dictionary);
	}
}