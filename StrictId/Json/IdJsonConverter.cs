using System.Text.Json;
using System.Text.Json.Serialization;
using Cysharp.Serialization.Json;

namespace StrictId.Json;

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

public class IdTypedJsonConverterFactory : JsonConverterFactory
{
	public override bool CanConvert (Type typeToConvert) =>
		typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(Id<>);

	public override JsonConverter? CreateConverter (Type typeToConvert, JsonSerializerOptions options) =>
		(JsonConverter?)Activator.CreateInstance(
			typeof(IdTypedJsonConverter<>).MakeGenericType(typeToConvert.GetGenericArguments().First())
		);

	private class IdTypedJsonConverter<T> : JsonConverter<Id<T>>
	{
		private readonly UlidJsonConverter _ulidJsonConverter = new();

		public override Id<T> Read (ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
			new(_ulidJsonConverter.Read(ref reader, typeToConvert, options));

		public override void Write (Utf8JsonWriter writer, Id<T> value, JsonSerializerOptions options) =>
			_ulidJsonConverter.Write(writer, value.Value, options);
	}
}