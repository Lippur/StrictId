using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using StrictId.Internal;
using StrictId.Json;

namespace StrictId;

/// <summary>
/// A strongly-typed, phantom-typed string StrictId for entities of type
/// <typeparamref name="T"/>. Wraps an opaque string intended for third-party IDs
/// (Stripe <c>cus_...</c>, Twilio <c>SM...</c>), legacy IDs, slugs, or SKUs. Values
/// of different <typeparamref name="T"/>s cannot be assigned to or compared with
/// each other, preventing accidental mix-ups across entities at compile time.
/// </summary>
/// <typeparam name="T">
/// The entity type this identifier belongs to. Used as a compile-time tag and as the
/// key for per-type prefix and validation-rule resolution via
/// <see cref="IdPrefixAttribute"/> and <see cref="IdStringAttribute"/>.
/// </typeparam>
/// <remarks>
/// <para>
/// Validation rules come from <see cref="IdStringAttribute"/> on <typeparamref name="T"/>
/// or defaults (max length 255, <see cref="IdStringCharSet.Any"/>, case-sensitive).
/// When <see cref="IdStringAttribute.IgnoreCase"/> is <see langword="true"/> the
/// stored value is normalized to lowercase on construction so that equality and
/// comparison work correctly without fighting the record struct auto-generated
/// members.
/// </para>
/// <para>
/// The <see cref="Value"/> property may be <see langword="null"/> for a
/// default-constructed instance; use <see cref="HasValue"/> to distinguish a real
/// value from the default.
/// </para>
/// </remarks>
[DebuggerDisplay("{ToString(),nq}"), JsonConverter(typeof(IdStringTypedJsonConverterFactory))]
public readonly record struct IdString<T> : IStrictId<IdString<T>>, IComparable
{
	/// <summary>The underlying string suffix value. May be <see langword="null"/> for a default-constructed instance.</summary>
	/// <remarks>
	/// The <c>init</c> accessor is <c>internal</c>: user code cannot bypass the validating
	/// constructor by writing <c>new IdString&lt;T&gt; { Value = "..." }</c>. StrictId's
	/// own internal parsers use the init accessor as an escape hatch to avoid re-validation
	/// of values they have already validated and normalised.
	/// </remarks>
	public string Value { get; internal init; }

	/// <summary>
	/// Creates an <see cref="IdString{T}"/> by parsing and validating the given string
	/// against <typeparamref name="T"/>'s <see cref="IdStringAttribute"/> rules. Accepts
	/// either a bare suffix or a prefixed form <c>prefix_suffix</c> when
	/// <typeparamref name="T"/> has a registered <see cref="IdPrefixAttribute"/>.
	/// </summary>
	/// <exception cref="FormatException">The value fails validation.</exception>
	public IdString (string value)
	{
		if (!IdStringParser.TryParseString(value.AsSpan(), StrictIdMetadata<T>.Prefix, IdStringMetadata<T>.Options, out var parsed))
			throw IdStringParser.BuildParseException(value, StrictIdMetadata<T>.Prefix, IdStringMetadata<T>.Options, $"{nameof(IdString)}<{typeof(T).Name}>");
		Value = parsed!;
	}

	/// <summary>The empty <see cref="IdString{T}"/>. Equivalent to <see langword="default"/>.</summary>
	public static IdString<T> Empty => default;

	/// <summary>
	/// <see langword="true"/> if this <see cref="IdString{T}"/> has a non-null, non-empty
	/// value.
	/// </summary>
	[IgnoreDataMember]
	public bool HasValue => !string.IsNullOrEmpty(Value);

	/// <summary>Compares this <see cref="IdString{T}"/> to another using ordinal string ordering.</summary>
	public int CompareTo (IdString<T> other) => string.CompareOrdinal(Value, other.Value);

	/// <inheritdoc />
	public int CompareTo (object? obj)
	{
		if (obj is null) return 1;
		if (obj is IdString<T> other) return CompareTo(other);
		throw new ArgumentException($"Argument must be of type {nameof(IdString)}<{typeof(T).Name}>.", nameof(obj));
	}

	/// <summary>
	/// Returns the underlying string suffix with no prefix applied, even for types that
	/// declare one. This is the value that round-trips through the <see cref="Value"/>
	/// property; <see cref="ToString()"/> prepends the canonical prefix when one is
	/// declared, but <see cref="ToBareString"/> never does. Returns <see cref="string.Empty"/>
	/// for a default-constructed instance whose <see cref="Value"/> is <see langword="null"/>.
	/// </summary>
	public string ToBareString () => Value ?? string.Empty;

	/// <summary>
	/// Returns the canonical string representation: <c>prefix_suffix</c> if
	/// <typeparamref name="T"/> has a registered prefix, otherwise the bare suffix.
	/// </summary>
	public override string ToString () => IdStringFormatter.Format(Value, StrictIdMetadata<T>.Prefix, default);

	/// <summary>Formats this <see cref="IdString{T}"/> using the given format specifier.</summary>
	/// <param name="format">One of <c>C</c> (canonical, default) or <c>B</c> (bare suffix).</param>
	/// <param name="formatProvider">Ignored; StrictIds are culture-invariant.</param>
	/// <exception cref="FormatException">The format specifier is not recognised.</exception>
	public string ToString (string? format, IFormatProvider? formatProvider)
		=> IdStringFormatter.Format(Value, StrictIdMetadata<T>.Prefix, format.AsSpan());

	/// <summary>Formats this <see cref="IdString{T}"/> using the given format specifier.</summary>
	public string ToString (string? format) => ToString(format, null);

	/// <summary>Attempts to write this <see cref="IdString{T}"/> into the provided character span.</summary>
	public bool TryFormat (
		Span<char> destination,
		out int charsWritten,
		ReadOnlySpan<char> format,
		IFormatProvider? provider
	) => IdStringFormatter.TryFormat(Value, StrictIdMetadata<T>.Prefix, destination, out charsWritten, format);

	/// <summary>Attempts to write this <see cref="IdString{T}"/> as UTF-8 bytes into the provided span.</summary>
	public bool TryFormat (
		Span<byte> utf8Destination,
		out int bytesWritten,
		ReadOnlySpan<char> format,
		IFormatProvider? provider
	) => IdStringFormatter.TryFormat(Value, StrictIdMetadata<T>.Prefix, utf8Destination, out bytesWritten, format);

	/// <summary>
	/// Converts this typed <see cref="IdString{T}"/> into a non-generic <see cref="IdString"/>,
	/// erasing the phantom entity type. The underlying string is copied verbatim — because the
	/// typed form has already validated and normalised it, the non-generic form is constructed
	/// via the internal <c>init</c> accessor rather than routed through the non-generic
	/// validating constructor. This guarantees that erasing the type is infallible, even when
	/// <typeparamref name="T"/>'s <see cref="IdStringAttribute"/> rules (e.g. larger
	/// <see cref="IdStringAttribute.MaxLength"/>) would reject the value under the non-generic
	/// defaults.
	/// </summary>
	public IdString ToIdString () => Value is null ? default : new IdString { Value = Value };

	/// <summary>Parses a string into an <see cref="IdString{T}"/>.</summary>
	/// <exception cref="FormatException">
	/// The string is not a valid <see cref="IdString{T}"/>. The exception message includes the
	/// offending input, the expected shape, the registered prefix list for <typeparamref name="T"/>,
	/// the suffix validation rules, and a best-effort diagnosis of the specific failure.
	/// </exception>
	public static IdString<T> Parse (string s)
	{
		if (IdStringParser.TryParseString(s.AsSpan(), StrictIdMetadata<T>.Prefix, IdStringMetadata<T>.Options, out var value))
			return new IdString<T> { Value = value! };
		throw IdStringParser.BuildParseException(s, StrictIdMetadata<T>.Prefix, IdStringMetadata<T>.Options, $"{nameof(IdString)}<{typeof(T).Name}>");
	}

	/// <inheritdoc cref="Parse(string)" />
	public static IdString<T> Parse (string s, IFormatProvider? provider) => Parse(s);

	/// <summary>Parses a character span into an <see cref="IdString{T}"/>.</summary>
	/// <exception cref="FormatException">The span is not a valid <see cref="IdString{T}"/>.</exception>
	public static IdString<T> Parse (ReadOnlySpan<char> s, IFormatProvider? provider)
	{
		if (IdStringParser.TryParseString(s, StrictIdMetadata<T>.Prefix, IdStringMetadata<T>.Options, out var value))
			return new IdString<T> { Value = value! };
		throw IdStringParser.BuildParseException(s.ToString(), StrictIdMetadata<T>.Prefix, IdStringMetadata<T>.Options, $"{nameof(IdString)}<{typeof(T).Name}>");
	}

	/// <summary>Attempts to parse a string into an <see cref="IdString{T}"/>.</summary>
	public static bool TryParse (string? s, out IdString<T> result)
	{
		if (s is not null && IdStringParser.TryParseString(s.AsSpan(), StrictIdMetadata<T>.Prefix, IdStringMetadata<T>.Options, out var value))
		{
			result = new IdString<T> { Value = value! };
			return true;
		}
		result = default;
		return false;
	}

	/// <inheritdoc cref="TryParse(string?, out IdString{T})" />
	public static bool TryParse (string? s, IFormatProvider? provider, out IdString<T> result) => TryParse(s, out result);

	/// <summary>Attempts to parse a character span into an <see cref="IdString{T}"/>.</summary>
	public static bool TryParse (ReadOnlySpan<char> s, IFormatProvider? provider, out IdString<T> result)
	{
		if (IdStringParser.TryParseString(s, StrictIdMetadata<T>.Prefix, IdStringMetadata<T>.Options, out var value))
		{
			result = new IdString<T> { Value = value! };
			return true;
		}
		result = default;
		return false;
	}

	/// <summary>Returns <see langword="true"/> if <paramref name="s"/> can be parsed as an <see cref="IdString{T}"/>.</summary>
	public static bool IsValid (string? s) => TryParse(s, out _);

	/// <summary>
	/// Implicitly converts a <see cref="string"/> to an <see cref="IdString{T}"/>,
	/// validating and normalizing the value against <typeparamref name="T"/>'s rules.
	/// Throws <see cref="FormatException"/> on invalid input.
	/// </summary>
	public static implicit operator IdString<T> (string value) => new(value);

	/// <summary>
	/// Implicitly converts a non-generic <see cref="IdString"/> to an
	/// <see cref="IdString{T}"/>, re-validating the value against <typeparamref name="T"/>'s
	/// rules. A default (null) input produces a default <see cref="IdString{T}"/>.
	/// </summary>
	public static implicit operator IdString<T> (IdString value)
		=> value.Value is null ? default : new IdString<T>(value.Value);

	/// <summary>
	/// Explicitly converts an <see cref="IdString{T}"/> to a non-generic <see cref="IdString"/>,
	/// discarding the phantom entity type.
	/// </summary>
	public static explicit operator IdString (IdString<T> value) => value.ToIdString();

	/// <summary>
	/// Explicitly converts an <see cref="IdString{T}"/> to its canonical string form.
	/// </summary>
	public static explicit operator string (IdString<T> value) => value.ToString();
}
