using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StrictId.Json;

/// <summary>
/// <see cref="JsonConverter{T}"/> for the non-generic <see cref="IdNumber"/>. Reads any form
/// <see cref="IdNumber.Parse(string)"/> accepts (a bare sequence of decimal digits); writes
/// the canonical base-10 digit string. Supports usage as a JSON object key.
/// </summary>
/// <remarks>
/// IdNumbers are serialized as JSON strings, not JSON numbers. This preserves any
/// prefix on the typed <see cref="IdNumber{T}"/> and avoids the 53-bit precision
/// limit of JSON numbers in JavaScript clients.
/// </remarks>
public sealed class IdNumberJsonConverter : JsonConverter<IdNumber>
{
	// Non-generic IdNumber has no prefix; ulong.MaxValue is 20 decimal digits.
	// 24-byte stack buffer covers every canonical form with slack.
	private const int StackBufferSize = 24;

	/// <inheritdoc />
	public override IdNumber Read (ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType is not JsonTokenType.String)
			throw new JsonException($"Expected a JSON string token for {nameof(IdNumber)} but found {reader.TokenType}.");
		var s = reader.GetString();
		return s is null ? default : IdNumber.Parse(s);
	}

	/// <inheritdoc />
	public override void Write (Utf8JsonWriter writer, IdNumber value, JsonSerializerOptions options)
	{
		Span<byte> buffer = stackalloc byte[StackBufferSize];
		if (value.TryFormat(buffer, out var written, default, null))
			writer.WriteStringValue(buffer[..written]);
		else
			writer.WriteStringValue(value.ToString());
	}

	/// <inheritdoc />
	public override IdNumber ReadAsPropertyName (ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType is not JsonTokenType.PropertyName)
			throw new JsonException($"Expected a property name token for {nameof(IdNumber)} but found {reader.TokenType}.");
		return IdNumber.Parse(reader.GetString()!);
	}

	/// <inheritdoc />
	public override void WriteAsPropertyName (Utf8JsonWriter writer, IdNumber value, JsonSerializerOptions options)
	{
		Span<byte> buffer = stackalloc byte[StackBufferSize];
		if (value.TryFormat(buffer, out var written, default, null))
			writer.WritePropertyName(buffer[..written]);
		else
			writer.WritePropertyName(value.ToString());
	}
}

/// <summary>
/// Concrete <see cref="JsonConverter{T}"/> for a closed <see cref="IdNumber{T}"/>.
/// Reads any form <see cref="IdNumber{T}.Parse(string)"/> accepts; writes the canonical
/// prefixed form when <typeparamref name="T"/> has a registered prefix, otherwise bare digits.
/// </summary>
/// <typeparam name="T">The entity type of the <see cref="IdNumber{T}"/>.</typeparam>
public sealed class IdNumberTypedJsonConverter<T> : JsonConverter<IdNumber<T>>
{
	// Max prefix (63) + separator (1) + ulong digits (20) + slack = 88 bytes covers every canonical form.
	private const int StackBufferSize = 88;

	/// <inheritdoc />
	public override IdNumber<T> Read (ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType is not JsonTokenType.String)
			throw new JsonException($"Expected a JSON string token for {nameof(IdNumber)}<{typeof(T).Name}> but found {reader.TokenType}.");
		var s = reader.GetString();
		return s is null ? default : IdNumber<T>.Parse(s);
	}

	/// <inheritdoc />
	public override void Write (Utf8JsonWriter writer, IdNumber<T> value, JsonSerializerOptions options)
	{
		Span<byte> buffer = stackalloc byte[StackBufferSize];
		if (value.TryFormat(buffer, out var written, default, null))
			writer.WriteStringValue(buffer[..written]);
		else
			writer.WriteStringValue(value.ToString());
	}

	/// <inheritdoc />
	public override IdNumber<T> ReadAsPropertyName (ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType is not JsonTokenType.PropertyName)
			throw new JsonException($"Expected a property name token for {nameof(IdNumber)}<{typeof(T).Name}> but found {reader.TokenType}.");
		return IdNumber<T>.Parse(reader.GetString()!);
	}

	/// <inheritdoc />
	public override void WriteAsPropertyName (Utf8JsonWriter writer, IdNumber<T> value, JsonSerializerOptions options)
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
/// for each closed <see cref="IdNumber{T}"/>. Consults <see cref="StrictIdRegistry"/>
/// first for a pre-registered instance; falls back to reflection for types the StrictId
/// source generator did not see.
/// </summary>
public sealed class IdNumberTypedJsonConverterFactory : JsonConverterFactory
{
	/// <inheritdoc />
	public override bool CanConvert (Type typeToConvert) =>
		typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(IdNumber<>);

	/// <inheritdoc />
	// See IdJsonConverter.cs for why this pair of unconditional suppressions is
	// required: CreateConverter is an override and cannot carry [RequiresDynamicCode],
	// but it calls a fallback that does. The StrictIdRegistry guard that precedes
	// the call is the AOT-friendly path that the source generator populates.
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

	[RequiresDynamicCode("IdNumberTypedJsonConverterFactory falls back to MakeGenericType when no pre-registered converter exists. Decorate the entity type with [IdPrefix] to stay on the AOT-friendly path.")]
	[RequiresUnreferencedCode("IdNumberTypedJsonConverterFactory falls back to reflection on IdNumber<T> closed generic types when no pre-registered converter exists.")]
	private static JsonConverter? CreateConverterViaReflection (Type typeToConvert) =>
		(JsonConverter?)Activator.CreateInstance(
			typeof(IdNumberTypedJsonConverter<>).MakeGenericType(typeToConvert.GetGenericArguments()[0])
		);
}
