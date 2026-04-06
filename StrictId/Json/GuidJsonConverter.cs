using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StrictId.Json;

/// <summary>
/// Concrete <see cref="JsonConverter{T}"/> for a closed <see cref="Guid{T}"/>. Reads any
/// form <see cref="Guid{T}.Parse(string)"/> accepts; writes the canonical prefixed form
/// when <typeparamref name="T"/> has a registered <see cref="IdPrefixAttribute"/>,
/// otherwise the standard 36-character hyphenated GUID.
/// </summary>
/// <typeparam name="T">The entity type of the <see cref="Guid{T}"/>.</typeparam>
public sealed class GuidTypedJsonConverter<T> : JsonConverter<Guid<T>>
{
	// Max prefix (63) + separator (1) + Guid "D" format (36) + slack = 104 bytes.
	private const int StackBufferSize = 104;

	/// <inheritdoc />
	public override Guid<T> Read (ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType is not JsonTokenType.String)
			throw new JsonException($"Expected a JSON string token for Guid<{typeof(T).Name}> but found {reader.TokenType}.");
		var s = reader.GetString();
		return s is null ? default : Guid<T>.Parse(s);
	}

	/// <inheritdoc />
	public override void Write (Utf8JsonWriter writer, Guid<T> value, JsonSerializerOptions options)
	{
		Span<byte> buffer = stackalloc byte[StackBufferSize];
		if (value.TryFormat(buffer, out var written, default, null))
			writer.WriteStringValue(buffer[..written]);
		else
			writer.WriteStringValue(value.ToString());
	}

	/// <inheritdoc />
	public override Guid<T> ReadAsPropertyName (ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType is not JsonTokenType.PropertyName)
			throw new JsonException($"Expected a property name token for Guid<{typeof(T).Name}> but found {reader.TokenType}.");
		return Guid<T>.Parse(reader.GetString()!);
	}

	/// <inheritdoc />
	public override void WriteAsPropertyName (Utf8JsonWriter writer, Guid<T> value, JsonSerializerOptions options)
	{
		Span<byte> buffer = stackalloc byte[StackBufferSize];
		if (value.TryFormat(buffer, out var written, default, null))
			writer.WritePropertyName(buffer[..written]);
		else
			writer.WritePropertyName(value.ToString());
	}
}

/// <summary>
/// <see cref="JsonConverterFactory"/> that produces a <see cref="JsonConverter{T}"/>
/// for each closed <see cref="Guid{T}"/>. Consults <see cref="StrictIdRegistry"/> first
/// for a pre-registered instance; falls back to reflection for types the StrictId
/// source generator did not see.
/// </summary>
public sealed class GuidTypedJsonConverterFactory : JsonConverterFactory
{
	/// <inheritdoc />
	public override bool CanConvert (Type typeToConvert) =>
		typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(Guid<>);

	/// <inheritdoc />
	[UnconditionalSuppressMessage("AOT", "IL3050",
		Justification = "The reflection fallback only runs when the StrictId source generator did not emit a registration for this closed generic. Source-gen-visible types hit the StrictIdRegistry cache and never reach this code path at runtime.")]
	[UnconditionalSuppressMessage("Trimming", "IL2026",
		Justification = "Same guard as IL3050 — the reflection fallback is gated by a StrictIdRegistry lookup populated at module init by the source generator.")]
	public override JsonConverter? CreateConverter (Type typeToConvert, JsonSerializerOptions options)
	{
		if (StrictIdRegistry.TryGetJsonConverter(typeToConvert, out var cached))
			return cached;
		return CreateConverterViaReflection(typeToConvert);
	}

	[RequiresDynamicCode("GuidTypedJsonConverterFactory falls back to MakeGenericType when no pre-registered converter exists. Decorate the entity type with [IdPrefix] to stay on the AOT-friendly path.")]
	[RequiresUnreferencedCode("GuidTypedJsonConverterFactory falls back to reflection on Guid<T> closed generic types when no pre-registered converter exists.")]
	private static JsonConverter? CreateConverterViaReflection (Type typeToConvert) =>
		(JsonConverter?)Activator.CreateInstance(
			typeof(GuidTypedJsonConverter<>).MakeGenericType(typeToConvert.GetGenericArguments()[0])
		);
}
