using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StrictId.Json;

/// <summary>
/// <see cref="JsonConverter{T}"/> for the non-generic <see cref="Id"/>. Reads any form
/// <see cref="Id.Parse(string)"/> accepts (bare ULID or bare GUID); writes the canonical
/// lowercase 26-character Crockford base32 ULID string. Supports usage as a JSON object key.
/// </summary>
public sealed class IdJsonConverter : JsonConverter<Id>
{
	// Non-generic Id has no prefix; the canonical form is exactly 26 ASCII chars.
	// 32-byte stack buffer covers the canonical case with slack.
	private const int StackBufferSize = 32;

	/// <inheritdoc />
	public override Id Read (ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType is not JsonTokenType.String)
			throw new JsonException($"Expected a JSON string token for {nameof(Id)} but found {reader.TokenType}.");
		var s = reader.GetString();
		return s is null ? default : Id.Parse(s);
	}

	/// <inheritdoc />
	public override void Write (Utf8JsonWriter writer, Id value, JsonSerializerOptions options)
	{
		Span<byte> buffer = stackalloc byte[StackBufferSize];
		if (value.TryFormat(buffer, out var written, default, null))
			writer.WriteStringValue(buffer[..written]);
		else
			writer.WriteStringValue(value.ToString());
	}

	/// <inheritdoc />
	public override Id ReadAsPropertyName (ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType is not JsonTokenType.PropertyName)
			throw new JsonException($"Expected a property name token for {nameof(Id)} but found {reader.TokenType}.");
		return Id.Parse(reader.GetString()!);
	}

	/// <inheritdoc />
	public override void WriteAsPropertyName (Utf8JsonWriter writer, Id value, JsonSerializerOptions options)
	{
		Span<byte> buffer = stackalloc byte[StackBufferSize];
		if (value.TryFormat(buffer, out var written, default, null))
			writer.WritePropertyName(buffer[..written]);
		else
			writer.WritePropertyName(value.ToString());
	}
}

/// <summary>
/// <see cref="JsonConverterFactory"/> that produces a <see cref="JsonConverter{T}"/> for each
/// closed <see cref="Id{T}"/>. Reads any form <see cref="Id{T}.Parse(string)"/> accepts
/// (bare ULID, bare GUID, or the prefixed canonical form); writes the canonical prefixed
/// form when the entity type has a registered <see cref="IdPrefixAttribute"/>, otherwise
/// the bare ULID. Supports usage as a JSON object key.
/// </summary>
/// <remarks>
/// <para>
/// This factory relies on <see cref="Type.MakeGenericType"/> and
/// <see cref="Activator.CreateInstance(Type)"/> to construct the converter for each closed
/// generic type, which is not compatible with native AOT compilation. AOT consumers should
/// rely on the StrictId source generator (planned for a later phase), which emits concrete
/// per-closed-generic converters and bypasses this reflection path entirely.
/// </para>
/// </remarks>
[RequiresDynamicCode("IdTypedJsonConverterFactory uses MakeGenericType to construct JsonConverter<Id<T>> for each closed generic type. Use the StrictId source generator for AOT scenarios.")]
[RequiresUnreferencedCode("IdTypedJsonConverterFactory uses reflection on Id<T> closed generic types. Use the StrictId source generator for trim-safe scenarios.")]
public sealed class IdTypedJsonConverterFactory : JsonConverterFactory
{
	/// <inheritdoc />
	public override bool CanConvert (Type typeToConvert) =>
		typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(Id<>);

	/// <inheritdoc />
	public override JsonConverter? CreateConverter (Type typeToConvert, JsonSerializerOptions options) =>
		(JsonConverter?)Activator.CreateInstance(
			typeof(IdTypedJsonConverter<>).MakeGenericType(typeToConvert.GetGenericArguments()[0])
		);

	private sealed class IdTypedJsonConverter<T> : JsonConverter<Id<T>>
	{
		// Max prefix (63) + separator (1) + ULID (26) + slack = 96 bytes covers every canonical form.
		private const int StackBufferSize = 96;

		public override Id<T> Read (ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType is not JsonTokenType.String)
				throw new JsonException($"Expected a JSON string token for {nameof(Id)}<{typeof(T).Name}> but found {reader.TokenType}.");
			var s = reader.GetString();
			return s is null ? default : Id<T>.Parse(s);
		}

		public override void Write (Utf8JsonWriter writer, Id<T> value, JsonSerializerOptions options)
		{
			Span<byte> buffer = stackalloc byte[StackBufferSize];
			if (value.TryFormat(buffer, out var written, default, null))
				writer.WriteStringValue(buffer[..written]);
			else
				writer.WriteStringValue(value.ToString());
		}

		public override Id<T> ReadAsPropertyName (ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType is not JsonTokenType.PropertyName)
				throw new JsonException($"Expected a property name token for {nameof(Id)}<{typeof(T).Name}> but found {reader.TokenType}.");
			return Id<T>.Parse(reader.GetString()!);
		}

		public override void WriteAsPropertyName (Utf8JsonWriter writer, Id<T> value, JsonSerializerOptions options)
		{
			Span<byte> buffer = stackalloc byte[StackBufferSize];
			if (value.TryFormat(buffer, out var written, default, null))
				writer.WritePropertyName(buffer[..written]);
			else
				writer.WritePropertyName(value.ToString());
		}
	}
}
