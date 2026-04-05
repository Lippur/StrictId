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
/// <para>
/// <b>Design note — default round-trip convention.</b> A default <see cref="IdString"/>
/// (whose <see cref="IdString.Value"/> is <see langword="null"/>) serializes to an empty
/// JSON string <c>""</c>, and an empty JSON string deserializes back to
/// <see langword="default"/>. This convention is unambiguous because a *constructed*
/// <see cref="IdString"/> can never have an empty suffix — the validating constructor
/// rejects <c>""</c> — so the empty-string wire form is reserved for the default.
/// <see cref="IdString"/> is the only StrictId family where the default-vs-constructed
/// distinction is observable via JSON, because only <see cref="IdString"/> can be
/// <see langword="null"/>-backed; the ULID and numeric families use
/// <see langword="default"/>-as-zero and serialize accordingly.
/// </para>
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
/// <see cref="JsonConverterFactory"/> that produces a <see cref="JsonConverter{T}"/> for each
/// closed <see cref="IdString{T}"/>. Reads any form <see cref="IdString{T}.Parse(string)"/>
/// accepts (bare suffix or prefixed canonical form, re-validated against the entity type's
/// <see cref="IdStringAttribute"/> rules); writes the canonical prefixed form when the entity
/// type has a registered <see cref="IdPrefixAttribute"/>, otherwise the bare suffix. Supports
/// usage as a JSON object key.
/// </summary>
/// <remarks>
/// <para>
/// A default <see cref="IdString{T}"/> (<see cref="IdString{T}.Value"/> is
/// <see langword="null"/>) serializes as an empty JSON string and round-trips back to
/// <see langword="default"/>.
/// </para>
/// <para>
/// This factory relies on <see cref="Type.MakeGenericType"/> and
/// <see cref="Activator.CreateInstance(Type)"/> to construct the converter for each closed
/// generic type, which is not compatible with native AOT compilation. AOT consumers should
/// rely on the StrictId source generator (planned for a later phase), which emits concrete
/// per-closed-generic converters and bypasses this reflection path entirely.
/// </para>
/// </remarks>
[RequiresDynamicCode("IdStringTypedJsonConverterFactory uses MakeGenericType to construct JsonConverter<IdString<T>> for each closed generic type. Use the StrictId source generator for AOT scenarios.")]
[RequiresUnreferencedCode("IdStringTypedJsonConverterFactory uses reflection on IdString<T> closed generic types. Use the StrictId source generator for trim-safe scenarios.")]
public sealed class IdStringTypedJsonConverterFactory : JsonConverterFactory
{
	/// <inheritdoc />
	public override bool CanConvert (Type typeToConvert) =>
		typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(IdString<>);

	/// <inheritdoc />
	public override JsonConverter? CreateConverter (Type typeToConvert, JsonSerializerOptions options) =>
		(JsonConverter?)Activator.CreateInstance(
			typeof(IdStringTypedJsonConverter<>).MakeGenericType(typeToConvert.GetGenericArguments()[0])
		);

	private sealed class IdStringTypedJsonConverter<T> : JsonConverter<IdString<T>>
	{
		// Max prefix (63) + separator (1) + default suffix (255) + slack = 320 bytes covers
		// every default-configured IdString<T>. Types with a custom MaxLength greater than
		// 255 fall through to the ToString() fallback below — ToString() allocates a managed
		// string and then the Utf8JsonWriter re-encodes it to UTF-8, costing two copies
		// instead of the zero-copy stack path. Consumers who serialize very wide IdString<T>
		// fields at high throughput should consider bumping this constant, keeping in mind
		// the stackalloc is per-call and lives on the serializer thread.
		private const int StackBufferSize = 320;

		public override IdString<T> Read (ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType is not JsonTokenType.String)
				throw new JsonException($"Expected a JSON string token for {nameof(IdString)}<{typeof(T).Name}> but found {reader.TokenType}.");
			var s = reader.GetString();
			return string.IsNullOrEmpty(s) ? default : new IdString<T>(s);
		}

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

		public override IdString<T> ReadAsPropertyName (ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType is not JsonTokenType.PropertyName)
				throw new JsonException($"Expected a property name token for {nameof(IdString)}<{typeof(T).Name}> but found {reader.TokenType}.");
			var s = reader.GetString();
			return string.IsNullOrEmpty(s) ? default : new IdString<T>(s);
		}

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
}
