using System.Text.Json;
using System.Text.Json.Serialization;
using Cysharp.Serialization.Json;

namespace Corpo.Core.Framework.Database.Identifiers;

public class IdJsonConverter : JsonConverter<Id>
{
	private readonly UlidJsonConverter _ulidJsonConverter;

	public IdJsonConverter ()
	{
		_ulidJsonConverter = new UlidJsonConverter();
	}

	public override Id Read (ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
		new(_ulidJsonConverter.Read(ref reader, typeToConvert, options));

	public override void Write (Utf8JsonWriter writer, Id value, JsonSerializerOptions options) =>
		_ulidJsonConverter.Write(writer, value.Value, options);
}