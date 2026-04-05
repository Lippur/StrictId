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
/// Concrete <see cref="JsonConverter{T}"/> for a closed <see cref="Id{T}"/>. Reads any
/// form <see cref="Id{T}.Parse(string)"/> accepts; writes the canonical prefixed form
/// when <typeparamref name="T"/> has a registered <see cref="IdPrefixAttribute"/>,
/// otherwise the bare ULID.
/// </summary>
/// <remarks>
/// This type is instantiated directly by generated code (StrictId source generator)
/// and registered into <see cref="StrictIdRegistry"/> so the
/// <see cref="IdTypedJsonConverterFactory"/> can resolve it without
/// <see cref="Type.MakeGenericType(Type[])"/>. Users who hand-register a converter
/// can also construct it explicitly: <c>new IdTypedJsonConverter&lt;User&gt;()</c>.
/// </remarks>
/// <typeparam name="T">The phantom entity type of the <see cref="Id{T}"/>.</typeparam>
public sealed class IdTypedJsonConverter<T> : JsonConverter<Id<T>>
{
	// Max prefix (63) + separator (1) + ULID (26) + slack = 96 bytes covers every canonical form.
	private const int StackBufferSize = 96;

	/// <inheritdoc />
	public override Id<T> Read (ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType is not JsonTokenType.String)
			throw new JsonException($"Expected a JSON string token for {nameof(Id)}<{typeof(T).Name}> but found {reader.TokenType}.");
		var s = reader.GetString();
		return s is null ? default : Id<T>.Parse(s);
	}

	/// <inheritdoc />
	public override void Write (Utf8JsonWriter writer, Id<T> value, JsonSerializerOptions options)
	{
		Span<byte> buffer = stackalloc byte[StackBufferSize];
		if (value.TryFormat(buffer, out var written, default, null))
			writer.WriteStringValue(buffer[..written]);
		else
			writer.WriteStringValue(value.ToString());
	}

	/// <inheritdoc />
	public override Id<T> ReadAsPropertyName (ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType is not JsonTokenType.PropertyName)
			throw new JsonException($"Expected a property name token for {nameof(Id)}<{typeof(T).Name}> but found {reader.TokenType}.");
		return Id<T>.Parse(reader.GetString()!);
	}

	/// <inheritdoc />
	public override void WriteAsPropertyName (Utf8JsonWriter writer, Id<T> value, JsonSerializerOptions options)
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
/// for each closed <see cref="Id{T}"/>. Consults <see cref="StrictIdRegistry"/> first
/// for a pre-registered instance (emitted by the StrictId source generator for every
/// <c>[IdPrefix]</c>-decorated type visible at compile time); falls back to
/// <see cref="Activator.CreateInstance(Type)"/> on a runtime reflection path for types
/// the generator did not see.
/// </summary>
/// <remarks>
/// <para>
/// The reflection fallback is annotated with <see cref="RequiresDynamicCodeAttribute"/>
/// and <see cref="RequiresUnreferencedCodeAttribute"/> because it depends on
/// <see cref="Type.MakeGenericType(Type[])"/>. Consumers targeting AOT should ensure
/// every <c>Id&lt;T&gt;</c> they serialize is registered — the generator does this
/// automatically for any type that declares <c>[IdPrefix]</c>.
/// </para>
/// </remarks>
public sealed class IdTypedJsonConverterFactory : JsonConverterFactory
{
	/// <inheritdoc />
	public override bool CanConvert (Type typeToConvert) =>
		typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(Id<>);

	/// <inheritdoc />
	public override JsonConverter? CreateConverter (Type typeToConvert, JsonSerializerOptions options)
	{
		if (StrictIdRegistry.TryGetJsonConverter(typeToConvert, out var cached))
			return cached;
		return CreateConverterViaReflection(typeToConvert);
	}

	[RequiresDynamicCode("IdTypedJsonConverterFactory falls back to MakeGenericType when the StrictId source generator did not produce a concrete JsonConverter<Id<T>> for the requested type. Decorate the entity type with [IdPrefix] (or register manually via StrictIdRegistry.RegisterJsonConverter) to stay on the AOT-friendly path.")]
	[RequiresUnreferencedCode("IdTypedJsonConverterFactory falls back to reflection on Id<T> closed generic types when no pre-registered converter exists.")]
	private static JsonConverter? CreateConverterViaReflection (Type typeToConvert) =>
		(JsonConverter?)Activator.CreateInstance(
			typeof(IdTypedJsonConverter<>).MakeGenericType(typeToConvert.GetGenericArguments()[0])
		);
}
