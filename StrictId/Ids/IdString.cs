using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using StrictId.Internal;
using StrictId.Json;

namespace StrictId;

/// <summary>
/// A non-generic, type-erased string-backed StrictId. Wraps an opaque
/// <see cref="string"/> value intended for third-party IDs (Stripe <c>cus_...</c>,
/// Twilio <c>SM...</c>), legacy string IDs, slugs, or SKUs. For type-safe
/// string identifiers that cannot be mixed across entities, prefer
/// <see cref="IdString{T}"/>.
/// </summary>
/// <remarks>
/// <para>
/// Uses the default <see cref="IdStringOptions"/>: maximum length 255, character set
/// <see cref="IdStringCharSet.AlphanumericDashUnderscore"/>, and case-sensitive comparison.
/// The non-generic form cannot be configured via attribute — attach an
/// <see cref="IdStringAttribute"/> to a marker type and use <see cref="IdString{T}"/>
/// if you need custom rules.
/// </para>
/// <para>
/// The <see cref="Value"/> property may be <see langword="null"/> for a
/// default-constructed instance; use <see cref="HasValue"/> to distinguish a real
/// value from the default.
/// </para>
/// </remarks>
[DebuggerDisplay("{ToString(),nq}"), JsonConverter(typeof(IdStringJsonConverter))]
public readonly record struct IdString : IStrictId<IdString>, IComparable
{
	/// <summary>The underlying string value. May be <see langword="null"/> for a default-constructed instance.</summary>
	/// <remarks>
	/// The <c>init</c> accessor is <c>internal</c>: user code cannot bypass the validating
	/// constructor by writing <c>new IdString { Value = "..." }</c>. StrictId's own
	/// internal parsers use the init accessor as an escape hatch to avoid re-validation of
	/// values they have already validated and normalised.
	/// </remarks>
	public string Value { get; internal init; }

	/// <summary>
	/// Creates an <see cref="IdString"/> by validating the given string against the
	/// default rules (non-empty, length ≤ 255, no whitespace, no separator chars).
	/// </summary>
	/// <param name="value">The opaque string to wrap. May be a prefixed form if another StrictId type is encoded into it.</param>
	/// <exception cref="FormatException">The value fails validation.</exception>
	public IdString (string value)
	{
		if (!IdStringParser.TryParseString(value.AsSpan(), PrefixInfo.None, IdStringOptions.Default, out var parsed))
			throw IdStringParser.BuildParseException(value, PrefixInfo.None, IdStringOptions.Default, nameof(IdString));
		Value = parsed!;
	}

	/// <summary>The empty <see cref="IdString"/>. Equivalent to <see langword="default"/>.</summary>
	public static IdString Empty => default;

	/// <summary>
	/// <see langword="true"/> if this <see cref="IdString"/> has a non-null, non-empty
	/// value.
	/// </summary>
	[IgnoreDataMember]
	public bool HasValue => !string.IsNullOrEmpty(Value);

	/// <summary>Compares this <see cref="IdString"/> to another using ordinal string ordering.</summary>
	public int CompareTo (IdString other) => string.CompareOrdinal(Value, other.Value);

	/// <inheritdoc />
	public int CompareTo (object? obj)
	{
		if (obj is null) return 1;
		if (obj is IdString other) return CompareTo(other);
		throw new ArgumentException($"Argument must be of type {nameof(IdString)}.", nameof(obj));
	}

	/// <summary>
	/// Returns the underlying string suffix with no prefix applied. For the non-generic
	/// <see cref="IdString"/> this is equivalent to <see cref="ToString()"/> because there
	/// is no prefix, but the helper exists so that code written against the shared
	/// <c>IdString</c> / <c>IdString&lt;T&gt;</c> surface can always say "give me the bare
	/// suffix" without branching on the generic parameter. Returns <see cref="string.Empty"/>
	/// for a default-constructed instance whose <see cref="Value"/> is <see langword="null"/>.
	/// </summary>
	public string ToBareString () => Value ?? string.Empty;

	/// <summary>Returns the canonical string form of this <see cref="IdString"/>.</summary>
	public override string ToString () => IdStringFormatter.Format(Value, PrefixInfo.None, default);

	/// <summary>Formats this <see cref="IdString"/> using the given format specifier.</summary>
	/// <param name="format">
	/// One of <c>C</c> (canonical, default) or <c>B</c> (bare suffix). For the non-generic
	/// <see cref="IdString"/> these are equivalent since there is no prefix to strip.
	/// </param>
	/// <param name="formatProvider">Ignored; StrictIds are culture-invariant.</param>
	/// <exception cref="FormatException">The format specifier is not recognised.</exception>
	public string ToString (string? format, IFormatProvider? formatProvider)
		=> IdStringFormatter.Format(Value, PrefixInfo.None, format.AsSpan());

	/// <summary>Formats this <see cref="IdString"/> using the given format specifier.</summary>
	public string ToString (string? format) => ToString(format, null);

	/// <summary>Attempts to write this <see cref="IdString"/> into the provided character span.</summary>
	public bool TryFormat (
		Span<char> destination,
		out int charsWritten,
		ReadOnlySpan<char> format,
		IFormatProvider? provider
	) => IdStringFormatter.TryFormat(Value, PrefixInfo.None, destination, out charsWritten, format);

	/// <summary>Attempts to write this <see cref="IdString"/> as UTF-8 bytes into the provided span.</summary>
	public bool TryFormat (
		Span<byte> utf8Destination,
		out int bytesWritten,
		ReadOnlySpan<char> format,
		IFormatProvider? provider
	) => IdStringFormatter.TryFormat(Value, PrefixInfo.None, utf8Destination, out bytesWritten, format);

	/// <summary>Parses a string into an <see cref="IdString"/>.</summary>
	/// <exception cref="FormatException">The string is not a valid <see cref="IdString"/>.</exception>
	public static IdString Parse (string s)
	{
		if (IdStringParser.TryParseString(s.AsSpan(), PrefixInfo.None, IdStringOptions.Default, out var value))
			return new IdString { Value = value! };
		throw IdStringParser.BuildParseException(s, PrefixInfo.None, IdStringOptions.Default, nameof(IdString));
	}

	/// <inheritdoc cref="Parse(string)" />
	public static IdString Parse (string s, IFormatProvider? provider) => Parse(s);

	/// <summary>Parses a character span into an <see cref="IdString"/>.</summary>
	/// <exception cref="FormatException">The span is not a valid <see cref="IdString"/>.</exception>
	public static IdString Parse (ReadOnlySpan<char> s, IFormatProvider? provider)
	{
		if (IdStringParser.TryParseString(s, PrefixInfo.None, IdStringOptions.Default, out var value))
			return new IdString { Value = value! };
		throw IdStringParser.BuildParseException(s.ToString(), PrefixInfo.None, IdStringOptions.Default, nameof(IdString));
	}

	/// <summary>Attempts to parse a string into an <see cref="IdString"/>.</summary>
	public static bool TryParse (string? s, out IdString result)
	{
		if (s is not null && IdStringParser.TryParseString(s.AsSpan(), PrefixInfo.None, IdStringOptions.Default, out var value))
		{
			result = new IdString { Value = value! };
			return true;
		}
		result = default;
		return false;
	}

	/// <inheritdoc cref="TryParse(string?, out IdString)" />
	public static bool TryParse (string? s, IFormatProvider? provider, out IdString result) => TryParse(s, out result);

	/// <summary>Attempts to parse a character span into an <see cref="IdString"/>.</summary>
	public static bool TryParse (ReadOnlySpan<char> s, IFormatProvider? provider, out IdString result)
	{
		if (IdStringParser.TryParseString(s, PrefixInfo.None, IdStringOptions.Default, out var value))
		{
			result = new IdString { Value = value! };
			return true;
		}
		result = default;
		return false;
	}

	/// <summary>Returns <see langword="true"/> if <paramref name="s"/> can be parsed as an <see cref="IdString"/>.</summary>
	public static bool IsValid (string? s) => TryParse(s, out _);

	/// <summary>
	/// Implicitly converts a <see cref="string"/> to an <see cref="IdString"/>, validating
	/// the value against the default rules. Throws <see cref="FormatException"/> on
	/// invalid input.
	/// </summary>
	public static implicit operator IdString (string value) => new(value);

	/// <summary>
	/// Explicitly converts an <see cref="IdString"/> to its canonical string form. Returns
	/// an empty string if <see cref="Value"/> is <see langword="null"/>.
	/// </summary>
	public static explicit operator string (IdString value) => value.ToString();
}
