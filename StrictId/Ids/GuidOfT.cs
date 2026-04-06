using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using StrictId.Internal;
using StrictId.Json;

namespace StrictId;

/// <summary>
/// A strongly-typed Guid identifier for entities of type
/// <typeparamref name="T"/>. Designed as a drop-in replacement for <see cref="Guid"/>:
/// the API surface mirrors <see cref="Guid"/> as closely as possible so that changing
/// <c>Guid</c> to <c>Guid&lt;T&gt;</c> requires minimal code changes. When no
/// <see cref="IdPrefixAttribute"/> is declared on <typeparamref name="T"/>, formatting and
/// parsing behaviour is identical to <see cref="Guid"/>.
/// </summary>
/// <typeparam name="T">
/// The entity type this identifier belongs to. Used only as a compile-time tag and as the
/// key for per-type prefix resolution via <see cref="IdPrefixAttribute"/>.
/// </typeparam>
[DebuggerDisplay("{ToString(),nq}"), JsonConverter(typeof(GuidTypedJsonConverterFactory))]
public readonly record struct Guid<T> (Guid Value) : IStrictId<Guid<T>>, IComparable
{
	/// <summary>The empty (all-zero) <see cref="Guid{T}"/>. Equivalent to <c>default</c>.</summary>
	public static Guid<T> Empty => default;

	/// <summary>
	/// <see langword="true"/> if this <see cref="Guid{T}"/> has a non-empty value,
	/// <see langword="false"/> if it equals <see cref="Empty"/>.
	/// </summary>
	[IgnoreDataMember]
	public bool HasValue => Value != Guid.Empty;

	/// <summary>Compares this <see cref="Guid{T}"/> to another of the same type.</summary>
	public int CompareTo (Guid<T> other) => Value.CompareTo(other.Value);

	/// <inheritdoc />
	public int CompareTo (object? obj)
	{
		if (obj is null) return 1;
		if (obj is Guid<T> other) return CompareTo(other);
		throw new ArgumentException($"Argument must be of type Guid<{typeof(T).Name}>.", nameof(obj));
	}

	/// <summary>
	/// Returns the canonical string representation: <c>prefix_guid</c> if
	/// <typeparamref name="T"/> has a registered prefix, otherwise the standard 36-character
	/// hyphenated GUID ("D" format) — identical to <see cref="Guid.ToString()"/>.
	/// </summary>
	public override string ToString () => GuidFormatter.Format(Value, StrictIdMetadata<T>.Prefix, default);

	/// <summary>Formats this <see cref="Guid{T}"/> using the given format specifier.</summary>
	/// <param name="format">
	/// One of <c>C</c> (canonical, default — includes prefix when declared), or any standard
	/// <see cref="Guid"/> format specifier (<c>D</c>, <c>N</c>, <c>B</c>, <c>P</c>, <c>X</c>)
	/// which always produces bare (unprefixed) output matching <see cref="Guid.ToString(string)"/>.
	/// </param>
	/// <param name="formatProvider">Ignored; StrictIds are culture-invariant.</param>
	/// <exception cref="FormatException">The format specifier is not recognised.</exception>
	public string ToString (string? format, IFormatProvider? formatProvider)
		=> GuidFormatter.Format(Value, StrictIdMetadata<T>.Prefix, format.AsSpan());

	/// <summary>Formats this <see cref="Guid{T}"/> using the given format specifier.</summary>
	public string ToString (string? format) => ToString(format, null);

	/// <summary>Attempts to write this <see cref="Guid{T}"/> into the provided character span.</summary>
	public bool TryFormat (
		Span<char> destination,
		out int charsWritten,
		ReadOnlySpan<char> format,
		IFormatProvider? provider
	) => GuidFormatter.TryFormat(Value, StrictIdMetadata<T>.Prefix, destination, out charsWritten, format);

	/// <summary>Attempts to write this <see cref="Guid{T}"/> as UTF-8 bytes into the provided span.</summary>
	public bool TryFormat (
		Span<byte> utf8Destination,
		out int bytesWritten,
		ReadOnlySpan<char> format,
		IFormatProvider? provider
	) => GuidFormatter.TryFormat(Value, StrictIdMetadata<T>.Prefix, utf8Destination, out bytesWritten, format);

	/// <summary>Returns the underlying <see cref="Guid"/>.</summary>
	public Guid ToGuid () => Value;

	/// <summary>Returns the underlying 16-byte representation.</summary>
	public byte[] ToByteArray () => Value.ToByteArray();

	/// <summary>Parses a string into a <see cref="Guid{T}"/>.</summary>
	/// <exception cref="FormatException">
	/// The string is not a valid <see cref="Guid{T}"/>. The exception message includes the
	/// offending input, the expected shape, and a best-effort diagnosis of the failure.
	/// </exception>
	public static Guid<T> Parse (string s)
	{
		if (GuidParser.TryParseGuid(s.AsSpan(), StrictIdMetadata<T>.Prefix, out var value))
			return new Guid<T>(value);
		throw GuidParser.BuildParseException(s, StrictIdMetadata<T>.Prefix, $"Guid<{typeof(T).Name}>");
	}

	/// <inheritdoc cref="Parse(string)" />
	public static Guid<T> Parse (string s, IFormatProvider? provider) => Parse(s);

	/// <summary>Parses a character span into a <see cref="Guid{T}"/>.</summary>
	/// <exception cref="FormatException">The span is not a valid <see cref="Guid{T}"/>.</exception>
	public static Guid<T> Parse (ReadOnlySpan<char> s, IFormatProvider? provider)
	{
		if (GuidParser.TryParseGuid(s, StrictIdMetadata<T>.Prefix, out var value))
			return new Guid<T>(value);
		throw GuidParser.BuildParseException(s.ToString(), StrictIdMetadata<T>.Prefix, $"Guid<{typeof(T).Name}>");
	}

	/// <summary>Attempts to parse a string into a <see cref="Guid{T}"/>.</summary>
	public static bool TryParse (string? s, out Guid<T> result)
	{
		if (s is not null && GuidParser.TryParseGuid(s.AsSpan(), StrictIdMetadata<T>.Prefix, out var value))
		{
			result = new Guid<T>(value);
			return true;
		}
		result = default;
		return false;
	}

	/// <inheritdoc cref="TryParse(string?, out Guid{T})" />
	public static bool TryParse (string? s, IFormatProvider? provider, out Guid<T> result) => TryParse(s, out result);

	/// <summary>Attempts to parse a character span into a <see cref="Guid{T}"/>.</summary>
	public static bool TryParse (ReadOnlySpan<char> s, IFormatProvider? provider, out Guid<T> result)
	{
		if (GuidParser.TryParseGuid(s, StrictIdMetadata<T>.Prefix, out var value))
		{
			result = new Guid<T>(value);
			return true;
		}
		result = default;
		return false;
	}

	/// <summary>Returns <see langword="true"/> if <paramref name="s"/> can be parsed as a <see cref="Guid{T}"/>.</summary>
	public static bool IsValid (string? s) => TryParse(s, out _);

	/// <summary>
	/// Generates a new <see cref="Guid{T}"/> using <see cref="Guid.CreateVersion7()"/>,
	/// producing a time-sortable UUIDv7. This is the StrictId convention for generating
	/// new identifiers.
	/// </summary>
	public static Guid<T> NewId () => new(Guid.CreateVersion7());

	/// <summary>
	/// Generates a new <see cref="Guid{T}"/> using <see cref="Guid.NewGuid()"/>,
	/// producing a random UUIDv4. Matches the <see cref="Guid.NewGuid"/> API for
	/// drop-in compatibility.
	/// </summary>
	public static Guid<T> NewGuid () => new(Guid.NewGuid());

	/// <summary>
	/// Generates a new <see cref="Guid{T}"/> using <see cref="Guid.CreateVersion7()"/>,
	/// producing a time-sortable UUIDv7. Mirrors the <see cref="Guid.CreateVersion7()"/>
	/// API for drop-in compatibility.
	/// </summary>
	public static Guid<T> CreateVersion7 () => new(Guid.CreateVersion7());

	/// <summary>
	/// Generates a new <see cref="Guid{T}"/> using <see cref="Guid.CreateVersion7(DateTimeOffset)"/>
	/// with the given timestamp. Mirrors the <see cref="Guid.CreateVersion7(DateTimeOffset)"/>
	/// API for drop-in compatibility.
	/// </summary>
	/// <param name="timestamp">The timestamp to embed in the UUIDv7.</param>
	public static Guid<T> CreateVersion7 (DateTimeOffset timestamp) => new(Guid.CreateVersion7(timestamp));

	/// <summary>Implicitly converts a <see cref="Guid"/> to a <see cref="Guid{T}"/>.</summary>
	public static implicit operator Guid<T> (Guid value) => new(value);

	/// <summary>Explicitly converts a <see cref="Guid{T}"/> to its underlying <see cref="Guid"/>.</summary>
	public static explicit operator Guid (Guid<T> value) => value.Value;

	/// <summary>Explicitly converts a <see cref="Guid{T}"/> to its canonical string form.</summary>
	public static explicit operator string (Guid<T> value) => value.ToString();

	/// <summary>Explicitly converts a string to a <see cref="Guid{T}"/> by parsing.</summary>
	public static explicit operator Guid<T> (string value) => Parse(value);
}
