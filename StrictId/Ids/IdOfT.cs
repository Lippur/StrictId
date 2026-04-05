using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using StrictId.Internal;
using StrictId.Json;

namespace StrictId;

/// <summary>
/// A strongly-typed, phantom-typed StrictId for entities of type <typeparamref name="T"/>.
/// Values of <see cref="Id{T}"/> cannot be assigned to or compared with identifiers of a
/// different entity type, which prevents accidental mix-ups at compile time. Backed by a
/// <see cref="Ulid"/>; convertible losslessly to and from <see cref="Guid"/>, ULID string,
/// and GUID string.
/// </summary>
/// <typeparam name="T">
/// The entity type this identifier belongs to. Used only as a compile-time tag and as the
/// key for per-type prefix resolution via <see cref="IdPrefixAttribute"/>.
/// </typeparam>
[DebuggerDisplay("{ToString(),nq}"), JsonConverter(typeof(IdTypedJsonConverterFactory))]
public readonly record struct Id<T> (Ulid Value) : IStrictId<Id<T>>, IComparable
{
	/// <summary>Creates an <see cref="Id{T}"/> by parsing the provided string.</summary>
	/// <param name="value">
	/// A bare 26-char ULID, bare 36-char GUID, or — if <typeparamref name="T"/> has an
	/// <see cref="IdPrefixAttribute"/> — a prefixed form <c>prefix_ulid</c> or <c>prefix_guid</c>.
	/// </param>
	/// <exception cref="FormatException">The value is not a valid <see cref="Id{T}"/>.</exception>
	public Id (string value) : this(Parse(value).Value) { }

	/// <summary>Creates an <see cref="Id{T}"/> from a <see cref="Guid"/>.</summary>
	public Id (Guid guid) : this(new Ulid(guid)) { }

	/// <summary>Creates an <see cref="Id{T}"/> from a non-generic <see cref="Id"/>, preserving the underlying value.</summary>
	public Id (Id id) : this(id.Value) { }

	/// <summary>Creates a copy of an existing <see cref="Id{T}"/>.</summary>
	public Id (Id<T> id) : this(id.Value) { }

	/// <summary>The empty (all-zero) <see cref="Id{T}"/>.</summary>
	public static Id<T> Empty => default;

	/// <summary>The largest possible <see cref="Id{T}"/> value.</summary>
	public static Id<T> MaxValue => new(Ulid.MaxValue);

	/// <summary>The smallest possible <see cref="Id{T}"/> value. Equivalent to <see cref="Empty"/>.</summary>
	public static Id<T> MinValue => new(Ulid.MinValue);

	/// <summary>
	/// <see langword="true"/> if this <see cref="Id{T}"/> has a non-empty value,
	/// <see langword="false"/> if it equals <see cref="Empty"/>.
	/// </summary>
	[IgnoreDataMember]
	public bool HasValue => Value != default;

	/// <summary>The 80-bit random component of the underlying <see cref="Ulid"/>.</summary>
	[IgnoreDataMember]
	public byte[] Random => Value.Random;

	/// <summary>The timestamp component of the underlying <see cref="Ulid"/>.</summary>
	[IgnoreDataMember]
	public DateTimeOffset Time => Value.Time;

	/// <summary>Compares this <see cref="Id{T}"/> to another of the same type using lexicographic ULID ordering.</summary>
	public int CompareTo (Id<T> other) => Value.CompareTo(other.Value);

	/// <inheritdoc />
	public int CompareTo (object? obj)
	{
		if (obj is null) return 1;
		if (obj is Id<T> other) return CompareTo(other);
		throw new ArgumentException($"Argument must be of type {nameof(Id)}<{typeof(T).Name}>.", nameof(obj));
	}

	/// <summary>
	/// Returns the canonical string representation: <c>prefix_ulid</c> if <typeparamref name="T"/>
	/// has a registered prefix, otherwise the bare 26-character lowercase Crockford ULID.
	/// </summary>
	public override string ToString () => IdFormatter.Format(Value, StrictIdMetadata<T>.Prefix, default);

	/// <summary>Formats this <see cref="Id{T}"/> using the given format specifier.</summary>
	/// <param name="format">
	/// One of <c>C</c> (canonical, default), <c>B</c> (bare ULID), <c>G</c> (canonical GUID),
	/// <c>BG</c> (bare GUID), or <c>U</c> (uppercase ULID, v2 compatibility).
	/// </param>
	/// <param name="formatProvider">Ignored; StrictIds are culture-invariant.</param>
	/// <exception cref="FormatException">The format specifier is not recognised.</exception>
	public string ToString (string? format, IFormatProvider? formatProvider)
		=> IdFormatter.Format(Value, StrictIdMetadata<T>.Prefix, format.AsSpan());

	/// <summary>Formats this <see cref="Id{T}"/> using the given format specifier.</summary>
	public string ToString (string? format) => ToString(format, null);

	/// <summary>Attempts to write this <see cref="Id{T}"/> into the provided character span.</summary>
	public bool TryFormat (
		Span<char> destination,
		out int charsWritten,
		ReadOnlySpan<char> format,
		IFormatProvider? provider
	) => IdFormatter.TryFormat(Value, StrictIdMetadata<T>.Prefix, destination, out charsWritten, format);

	/// <summary>Attempts to write this <see cref="Id{T}"/> as UTF-8 bytes into the provided span.</summary>
	public bool TryFormat (
		Span<byte> utf8Destination,
		out int bytesWritten,
		ReadOnlySpan<char> format,
		IFormatProvider? provider
	) => IdFormatter.TryFormat(Value, StrictIdMetadata<T>.Prefix, utf8Destination, out bytesWritten, format);

	/// <summary>Returns the underlying <see cref="Ulid"/>.</summary>
	public Ulid ToUlid () => Value;

	/// <summary>Converts this <see cref="Id{T}"/> to its equivalent <see cref="Guid"/> representation.</summary>
	public Guid ToGuid () => Value.ToGuid();

	/// <summary>Returns a Base64 representation of the underlying 16 bytes.</summary>
	public string ToBase64 () => Value.ToBase64();

	/// <summary>Returns the underlying 16-byte representation.</summary>
	public byte[] ToByteArray () => Value.ToByteArray();

	/// <summary>Converts this typed <see cref="Id{T}"/> into a non-generic <see cref="Id"/>, erasing the phantom entity type.</summary>
	public Id ToId () => new(Value);

	/// <summary>Parses a string into an <see cref="Id{T}"/>.</summary>
	/// <exception cref="FormatException">
	/// The string is not a valid <see cref="Id{T}"/>. The exception message includes the offending
	/// input, the expected shape, the registered prefix list for <typeparamref name="T"/>, and a
	/// best-effort diagnosis of the specific failure.
	/// </exception>
	public static Id<T> Parse (string s)
	{
		if (IdParser.TryParseUlid(s.AsSpan(), StrictIdMetadata<T>.Prefix, out var value))
			return new Id<T>(value);
		throw IdParser.BuildParseException(s, StrictIdMetadata<T>.Prefix, $"{nameof(Id)}<{typeof(T).Name}>");
	}

	/// <inheritdoc cref="Parse(string)" />
	public static Id<T> Parse (string s, IFormatProvider? provider) => Parse(s);

	/// <summary>Parses a character span into an <see cref="Id{T}"/>.</summary>
	/// <exception cref="FormatException">The span is not a valid <see cref="Id{T}"/>.</exception>
	public static Id<T> Parse (ReadOnlySpan<char> s, IFormatProvider? provider)
	{
		if (IdParser.TryParseUlid(s, StrictIdMetadata<T>.Prefix, out var value))
			return new Id<T>(value);
		throw IdParser.BuildParseException(s.ToString(), StrictIdMetadata<T>.Prefix, $"{nameof(Id)}<{typeof(T).Name}>");
	}

	/// <summary>Attempts to parse a string into an <see cref="Id{T}"/>.</summary>
	public static bool TryParse (string? s, out Id<T> result)
	{
		if (s is not null && IdParser.TryParseUlid(s.AsSpan(), StrictIdMetadata<T>.Prefix, out var value))
		{
			result = new Id<T>(value);
			return true;
		}
		result = default;
		return false;
	}

	/// <inheritdoc cref="TryParse(string?, out Id{T})" />
	public static bool TryParse (string? s, IFormatProvider? provider, out Id<T> result) => TryParse(s, out result);

	/// <summary>Attempts to parse a character span into an <see cref="Id{T}"/>.</summary>
	public static bool TryParse (ReadOnlySpan<char> s, IFormatProvider? provider, out Id<T> result)
	{
		if (IdParser.TryParseUlid(s, StrictIdMetadata<T>.Prefix, out var value))
		{
			result = new Id<T>(value);
			return true;
		}
		result = default;
		return false;
	}

	/// <summary>Returns <see langword="true"/> if <paramref name="s"/> can be parsed as an <see cref="Id{T}"/>.</summary>
	public static bool IsValid (string? s) => TryParse(s, out _);

	/// <summary>Generates a new <see cref="Id{T}"/>. ULID generation is delegated to <see cref="Ulid.NewUlid()"/>.</summary>
	public static Id<T> NewId () => new(Ulid.NewUlid());

	/// <summary>Generates a new <see cref="Id{T}"/> with the given timestamp and fresh randomness.</summary>
	/// <param name="timestamp">The timestamp to embed in the ULID's high bits.</param>
	public static Id<T> NewId (DateTimeOffset timestamp) => new(Ulid.NewUlid(timestamp));

	/// <summary>Implicitly converts a <see cref="Ulid"/> to an <see cref="Id{T}"/>.</summary>
	public static implicit operator Id<T> (Ulid value) => new(value);

	/// <summary>Implicitly converts a <see cref="Guid"/> to an <see cref="Id{T}"/>.</summary>
	public static implicit operator Id<T> (Guid value) => new(value);

	/// <summary>
	/// Implicitly converts a non-generic <see cref="Id"/> to an <see cref="Id{T}"/>. The
	/// non-generic form carries no entity type, so this conversion does not lose any
	/// information — the caller is simply ascribing a type.
	/// </summary>
	public static implicit operator Id<T> (Id value) => new(value);

	/// <summary>Explicitly converts a string to an <see cref="Id{T}"/> by parsing.</summary>
	public static explicit operator Id<T> (string value) => Parse(value);

	/// <summary>
	/// Explicitly converts an <see cref="Id{T}"/> to a non-generic <see cref="Id"/>,
	/// discarding the phantom entity type.
	/// </summary>
	public static explicit operator Id (Id<T> value) => value.ToId();

	/// <summary>Explicitly converts an <see cref="Id{T}"/> to its underlying <see cref="Ulid"/>.</summary>
	public static explicit operator Ulid (Id<T> value) => value.Value;

	/// <summary>Explicitly converts an <see cref="Id{T}"/> to a <see cref="Guid"/>.</summary>
	public static explicit operator Guid (Id<T> value) => value.ToGuid();

	/// <summary>Explicitly converts an <see cref="Id{T}"/> to its canonical string form.</summary>
	public static explicit operator string (Id<T> value) => value.ToString();
}
