using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StrictId.Json;

/// <summary>
/// <see cref="JsonConverter{T}"/> for the non-generic <see cref="IdString"/>. Reads any form
/// <see cref="IdString.Parse(string)"/> accepts (an opaque string matching the default
/// validation rules); writes <see cref="IdString.Value"/> directly. Supports usage as a
/// JSON object key.
/// </summary>
/// <remarks>
/// A default <see cref="IdString"/> serializes to an empty JSON string <c>""</c>,
/// and an empty JSON string deserializes back to <see langword="default"/>.
/// </remarks>
public sealed class IdStringJsonConverter : JsonConverter<IdString>
{
	// Non-generic IdString defaults to MaxLength = 255; a 256-byte stack buffer fits the
	// canonical form (no prefix is possible on the non-generic) with one byte of slack.
	private const int StackBufferSize = 256;

	/// <inheritdoc />
	public override IdString Read (ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType is not JsonTokenType.String)
			throw new JsonException($"Expected a JSON string token for {nameof(IdString)} but found {reader.TokenType}.");
		var s = reader.GetString();
		return string.IsNullOrEmpty(s) ? default : new IdString(s);
	}

	/// <inheritdoc />
	public override void Write (Utf8JsonWriter writer, IdString value, JsonSerializerOptions options)
	{
		if (value.Value is null)
		{
			writer.WriteStringValue(ReadOnlySpan<byte>.Empty);
			return;
		}

		Span<byte> buffer = stackalloc byte[StackBufferSize];
		if (value.TryFormat(buffer, out var written, default, null))
			writer.WriteStringValue(buffer[..written]);
		else
			writer.WriteStringValue(value.ToString());
	}

	/// <inheritdoc />
	public override IdString ReadAsPropertyName (ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType is not JsonTokenType.PropertyName)
			throw new JsonException($"Expected a property name token for {nameof(IdString)} but found {reader.TokenType}.");
		var s = reader.GetString();
		return string.IsNullOrEmpty(s) ? default : new IdString(s);
	}

	/// <inheritdoc />
	public override void WriteAsPropertyName (Utf8JsonWriter writer, IdString value, JsonSerializerOptions options)
	{
		if (value.Value is null)
		{
			writer.WritePropertyName(ReadOnlySpan<byte>.Empty);
			return;
		}

		Span<byte> buffer = stackalloc byte[StackBufferSize];
		if (value.TryFormat(buffer, out var written, default, null))
			writer.WritePropertyName(buffer[..written]);
		else
			writer.WritePropertyName(value.ToString());
	}
}

/// <summary>
/// Concrete <see cref="JsonConverter{T}"/> for a closed <see cref="IdString{T}"/>.
/// Reads any form <see cref="IdString{T}.Parse(string)"/> accepts; writes the canonical
/// prefixed form when <typeparamref name="T"/> has a registered prefix, otherwise the bare suffix.
/// A default <see cref="IdString{T}"/> serializes as an empty JSON string and round-trips
/// back to <see langword="default"/>.
/// </summary>
/// <typeparam name="T">The entity type of the <see cref="IdString{T}"/>.</typeparam>
public sealed class IdStringTypedJsonConverter<T> : JsonConverter<IdString<T>>
{
	// Max prefix (63) + separator (1) + default suffix (255) + slack = 320 bytes covers
	// every default-configured IdString<T>. Types with a custom MaxLength greater than
	// 255 fall through to the ToString() fallback below — ToString() allocates a managed
	// string and then the Utf8JsonWriter re-encodes it to UTF-8, costing two copies
	// instead of the zero-copy stack path. Consumers who serialize very wide IdString<T>
	// fields at high throughput should consider bumping this constant, keeping in mind
	// the stackalloc is per-call and lives on the serializer thread.
	private const int StackBufferSize = 320;

	/// <inheritdoc />
	public override IdString<T> Read (ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType is not JsonTokenType.String)
			throw new JsonException($"Expected a JSON string token for {nameof(IdString)}<{typeof(T).Name}> but found {reader.TokenType}.");
		var s = reader.GetString();
		return string.IsNullOrEmpty(s) ? default : new IdString<T>(s);
	}

	/// <inheritdoc />
	public override void Write (Utf8JsonWriter writer, IdString<T> value, JsonSerializerOptions options)
	{
		if (value.Value is null)
		{
			writer.WriteStringValue(ReadOnlySpan<byte>.Empty);
			return;
		}

		Span<byte> buffer = stackalloc byte[StackBufferSize];
		if (value.TryFormat(buffer, out var written, default, null))
			writer.WriteStringValue(buffer[..written]);
		else
			writer.WriteStringValue(value.ToString());
	}

	/// <inheritdoc />
	public override IdString<T> ReadAsPropertyName (ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType is not JsonTokenType.PropertyName)
			throw new JsonException($"Expected a property name token for {nameof(IdString)}<{typeof(T).Name}> but found {reader.TokenType}.");
		var s = reader.GetString();
		return string.IsNullOrEmpty(s) ? default : new IdString<T>(s);
	}

	/// <inheritdoc />
	public override void WriteAsPropertyName (Utf8JsonWriter writer, IdString<T> value, JsonSerializerOptions options)
	{
		if (value.Value is null)
		{
			writer.WritePropertyName(ReadOnlySpan<byte>.Empty);
			return;
		}

		Span<byte> buffer = stackalloc byte[StackBufferSize];
		if (value.TryFormat(buffer, out var written, default, null))
			writer.WritePropertyName(buffer[..written]);
		else
			writer.WritePropertyName(value.ToString());
	}
}

/// <summary>
/// <see cref="JsonConverterFactory"/> that produces a <see cref="JsonConverter{T}"/>
/// for each closed <see cref="IdString{T}"/>. Consults <see cref="StrictIdRegistry"/>
/// first for a pre-registered instance; falls back to reflection for types the StrictId
/// source generator did not see.
/// </summary>
public sealed class IdStringTypedJsonConverterFactory : JsonConverterFactory
{
	/// <inheritdoc />
	public override bool CanConvert (Type typeToConvert) =>
		typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(IdString<>);

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

	[RequiresDynamicCode("IdStringTypedJsonConverterFactory falls back to MakeGenericType when no pre-registered converter exists. Decorate the entity type with [IdPrefix] to stay on the AOT-friendly path.")]
	[RequiresUnreferencedCode("IdStringTypedJsonConverterFactory falls back to reflection on IdString<T> closed generic types when no pre-registered converter exists.")]
	private static JsonConverter? CreateConverterViaReflection (Type typeToConvert) =>
		(JsonConverter?)Activator.CreateInstance(
			typeof(IdStringTypedJsonConverter<>).MakeGenericType(typeToConvert.GetGenericArguments()[0])
		);
}
