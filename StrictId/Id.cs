using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using StrictId.Json;

namespace StrictId;

/// <summary>
/// A strongly-typed, lexicographically sortable identifier backed by a <see cref="Ulid"/>.
/// Round-trip convertible to <see cref="Ulid"/>, <see cref="Guid"/>, and <see cref="string"/>.
/// For type-safe identifiers that cannot be mixed across entities, prefer <see cref="Id{T}"/>.
/// </summary>
[DebuggerDisplay("{ToString(),nq}"), JsonConverter(typeof(IdJsonConverter))]
public readonly record struct Id (Ulid Value) : IId, IComparable<Id>, ISpanParsable<Id>
{
	/// <summary>Creates an <see cref="Id"/> by parsing the provided ULID or GUID string.</summary>
	/// <param name="value">A ULID (Crockford base32) or GUID string representation.</param>
	/// <exception cref="FormatException">The value is not a valid ULID or GUID string.</exception>
	public Id (string value) : this(Parse(value)) { }

	/// <summary>Creates an <see cref="Id"/> from a <see cref="Guid"/>.</summary>
	/// <param name="guid">The GUID to wrap.</param>
	public Id (Guid guid) : this(new Ulid(guid)) { }

	/// <summary>Creates a copy of an existing <see cref="Id"/>.</summary>
	/// <param name="id">The id to copy.</param>
	public Id (Id id) : this(id.Value) { }

	/// <summary>An empty (all-zero) <see cref="Id"/>.</summary>
	public static Id Empty => new();

	/// <summary>The largest possible <see cref="Id"/> value.</summary>
	public static Id MaxValue => new(Ulid.MaxValue);

	/// <summary>The smallest possible <see cref="Id"/> value. Equivalent to <see cref="Empty"/>.</summary>
	public static Id MinValue => new(Ulid.MinValue);

	/// <summary>Compares this <see cref="Id"/> to another using lexicographic ULID ordering.</summary>
	public int CompareTo (Id other) => Value.CompareTo(other.Value);

	/// <summary>
	/// <see langword="true"/> if this <see cref="Id"/> has a non-empty value,
	/// <see langword="false"/> if it equals <see cref="Empty"/>.
	/// </summary>
	[IgnoreDataMember]
	public bool HasValue => Value != Ulid.Empty;

	/// <summary>The 80-bit random component of the underlying <see cref="Ulid"/>.</summary>
	[IgnoreDataMember]
	public byte[] Random => Value.Random;

	/// <summary>The timestamp component of the underlying <see cref="Ulid"/>.</summary>
	[IgnoreDataMember]
	public DateTimeOffset Time => Value.Time;

	/// <inheritdoc />
	public int CompareTo (object? obj) => Value.CompareTo(obj);

	/// <summary>Formats this <see cref="Id"/> as a string using the given format.</summary>
	public string ToString (string? format, IFormatProvider? formatProvider) => Value.ToString(format, formatProvider);

	/// <summary>Attempts to write this <see cref="Id"/> into the provided character span.</summary>
	public bool TryFormat (
		Span<char> destination,
		out int charsWritten,
		ReadOnlySpan<char> format,
		IFormatProvider? provider
	) => Value.TryFormat(destination, out charsWritten, format, provider);

	/// <summary>Attempts to write this <see cref="Id"/> as UTF-8 bytes into the provided span.</summary>
	public bool TryFormat (
		Span<byte> utf8Destination,
		out int bytesWritten,
		ReadOnlySpan<char> format,
		IFormatProvider? provider
	) => Value.TryFormat(utf8Destination, out bytesWritten, format, provider);

	/// <summary>Converts this <see cref="Id"/> to its equivalent <see cref="Guid"/> representation.</summary>
	public Guid ToGuid () => Value.ToGuid();

	/// <summary>Returns the canonical 26-character Crockford base32 ULID string.</summary>
	public override string ToString () => Value.ToString();

	/// <summary>Returns a Base64 representation of the underlying 16 bytes.</summary>
	public string ToBase64 () => Value.ToBase64();

	/// <summary>Returns the underlying 16-byte representation.</summary>
	public byte[] ToByteArray () => Value.ToByteArray();

	/// <inheritdoc cref="Parse(string)" />
	public static Id Parse (string s, IFormatProvider? provider) => Parse(s);

	/// <inheritdoc cref="TryParse(string?, out Id)" />
	public static bool TryParse (string? s, IFormatProvider? provider, out Id result) => TryParse(s, out result);

	/// <summary>Parses a ULID or GUID span into an <see cref="Id"/>.</summary>
	/// <exception cref="FormatException">The span is not a valid ULID or GUID.</exception>
	public static Id Parse (ReadOnlySpan<char> s, IFormatProvider? provider)
	{
		if (TryParse(s, provider, out var id)) return id;

		throw new FormatException("Could not parse value into a valid Ulid or Guid");
	}

	/// <summary>Attempts to parse a ULID or GUID span into an <see cref="Id"/>.</summary>
	public static bool TryParse (ReadOnlySpan<char> s, IFormatProvider? provider, out Id id)
	{
		if (Ulid.TryParse(s, out var ulid))
		{
			id = new Id(ulid);
			return true;
		}

		if (Guid.TryParse(s, out var guid))
		{
			id = new Id(guid);
			return true;
		}

		id = new Id();
		return false;
	}

	/// <summary>Returns <see langword="true"/> if <paramref name="s"/> can be parsed as an <see cref="Id"/>.</summary>
	public static bool IsValid (string? s) => TryParse(s, out _);

	/// <summary>Parses a ULID or GUID string into an <see cref="Id"/>.</summary>
	/// <exception cref="FormatException">The string is not a valid ULID or GUID.</exception>
	public static Id Parse (string value)
	{
		if (Ulid.TryParse(value, out var ulid)) return new Id(ulid);

		if (Guid.TryParse(value, out var guid)) return new Id(guid);

		throw new FormatException("Could not parse value into a valid Ulid or Guid");
	}

	/// <summary>Parses a ULID from its UTF-8 Crockford base32 byte representation.</summary>
	public static Id Parse (ReadOnlySpan<byte> base32) => new(Ulid.Parse(base32));

	/// <summary>Creates a new <see cref="Id"/> from an existing one. Equivalent to the copy constructor.</summary>
	public static Id From (Id id) => new(id.Value);

	/// <summary>Generates a new random <see cref="Id"/>.</summary>
	public static Id NewId () => new(Ulid.NewUlid());

	/// <summary>Returns the canonical string representation of the given <see cref="Id"/>.</summary>
	public static string ToString (Id id) => id.ToString();

	/// <summary>Creates an <see cref="Id"/> from a <see cref="Guid"/>.</summary>
	public static Id Parse (Guid guid) => new(new Ulid(guid));

	/// <summary>Attempts to parse a ULID or GUID string into an <see cref="Id"/>.</summary>
	public static bool TryParse (string? value, out Id id)
	{
		if (Ulid.TryParse(value, out var ulid))
		{
			id = new Id(ulid);
			return true;
		}

		if (Guid.TryParse(value, out var guid))
		{
			id = new Id(guid);
			return true;
		}

		id = new Id();
		return false;
	}

	/// <summary>Explicitly converts a ULID or GUID string to an <see cref="Id"/>.</summary>
	public static explicit operator Id (string value) => new(value);

	/// <summary>Implicitly converts a <see cref="Ulid"/> to an <see cref="Id"/>.</summary>
	public static implicit operator Id (Ulid value) => new(value);

	/// <summary>Implicitly converts a <see cref="Guid"/> to an <see cref="Id"/>.</summary>
	public static implicit operator Id (Guid value) => new(value);

	/// <summary>Explicitly converts an <see cref="Id"/> to a <see cref="Guid"/>.</summary>
	public static explicit operator Guid (Id value) => value.ToGuid();

	/// <summary>Explicitly converts an <see cref="Id"/> to its canonical string form.</summary>
	public static explicit operator string (Id value) => value.ToString();

	/// <summary>Explicitly converts an <see cref="Id"/> to the underlying <see cref="Ulid"/>.</summary>
	public static explicit operator Ulid (Id value) => value.Value;
}

/// <summary>
/// A strongly-typed, phantom-typed identifier for entities of type <typeparamref name="T"/>.
/// Values of <see cref="Id{T}"/> cannot be assigned or compared across different <typeparamref name="T"/>s,
/// which prevents accidentally mixing up identifiers for different entities.
/// </summary>
/// <typeparam name="T">The entity type this identifier belongs to. Used only as a compile-time tag.</typeparam>
[DebuggerDisplay("{ToString(),nq}"), JsonConverter(typeof(IdTypedJsonConverterFactory))]
public readonly record struct Id<T> (Ulid Value)
	: IId,
		IComparable<Id<T>>,
		IComparable<Id>,
		ISpanParsable<Id<T>>
{
	/// <summary>Creates an <see cref="Id{T}"/> by parsing the provided ULID or GUID string.</summary>
	/// <exception cref="FormatException">The value is not a valid ULID or GUID string.</exception>
	public Id (string value) : this(Parse(value)) { }

	/// <summary>Creates an <see cref="Id{T}"/> from a <see cref="Guid"/>.</summary>
	public Id (Guid guid) : this(Parse(guid)) { }

	/// <summary>Creates an <see cref="Id{T}"/> from a non-generic <see cref="Id"/>, preserving the underlying value.</summary>
	public Id (Id id) : this(id.Value) { }

	/// <summary>Creates a copy of an existing <see cref="Id{T}"/>.</summary>
	public Id (Id<T> id) : this(id.Value) { }

	/// <summary>An empty (all-zero) <see cref="Id{T}"/>.</summary>
	public static Id<T> Empty => new();

	/// <summary>The largest possible <see cref="Id{T}"/> value.</summary>
	public static Id<T> MaxValue => new(Ulid.MaxValue);

	/// <summary>The smallest possible <see cref="Id{T}"/> value. Equivalent to <see cref="Empty"/>.</summary>
	public static Id<T> MinValue => new(Ulid.MinValue);

	/// <summary>Compares this <see cref="Id{T}"/> to another of the same type using lexicographic ULID ordering.</summary>
	public int CompareTo (Id<T> other) => Value.CompareTo(other.Value);

	/// <summary>Compares this <see cref="Id{T}"/> to a non-generic <see cref="Id"/> by underlying value.</summary>
	public int CompareTo (Id other) => Value.CompareTo(other.Value);

	/// <summary>
	/// <see langword="true"/> if this <see cref="Id{T}"/> has a non-empty value,
	/// <see langword="false"/> if it equals <see cref="Empty"/>.
	/// </summary>
	[IgnoreDataMember]
	public bool HasValue => Value != Ulid.Empty;

	/// <summary>The 80-bit random component of the underlying <see cref="Ulid"/>.</summary>
	[IgnoreDataMember]
	public byte[] Random => Value.Random;

	/// <summary>The timestamp component of the underlying <see cref="Ulid"/>.</summary>
	[IgnoreDataMember]
	public DateTimeOffset Time => Value.Time;

	/// <inheritdoc />
	public int CompareTo (object? obj) => Value.CompareTo(obj);

	/// <summary>Formats this <see cref="Id{T}"/> as a string using the given format.</summary>
	public string ToString (string? format, IFormatProvider? formatProvider) => Value.ToString(format, formatProvider);

	/// <summary>Attempts to write this <see cref="Id{T}"/> into the provided character span.</summary>
	public bool TryFormat (
		Span<char> destination,
		out int charsWritten,
		ReadOnlySpan<char> format,
		IFormatProvider? provider
	) => Value.TryFormat(destination, out charsWritten, format, provider);

	/// <summary>Attempts to write this <see cref="Id{T}"/> as UTF-8 bytes into the provided span.</summary>
	public bool TryFormat (
		Span<byte> utf8Destination,
		out int bytesWritten,
		ReadOnlySpan<char> format,
		IFormatProvider? provider
	) => Value.TryFormat(utf8Destination, out bytesWritten, format, provider);

	/// <summary>Converts this <see cref="Id{T}"/> to its equivalent <see cref="Guid"/> representation.</summary>
	public Guid ToGuid () => Value.ToGuid();

	/// <summary>Returns the canonical 26-character Crockford base32 ULID string.</summary>
	public override string ToString () => Value.ToString();

	/// <summary>Returns a Base64 representation of the underlying 16 bytes.</summary>
	public string ToBase64 () => Value.ToBase64();

	/// <summary>Returns the underlying 16-byte representation.</summary>
	public byte[] ToByteArray () => Value.ToByteArray();

	static Id<T> IParsable<Id<T>>.Parse (string s, IFormatProvider? provider) => new(Parse(s, provider));

	/// <inheritdoc cref="TryParse(string?, out Id{T})" />
	public static bool TryParse (string? s, IFormatProvider? provider, out Id<T> result) => TryParse(s, out result);

	/// <summary>Parses a ULID or GUID span into an <see cref="Id{T}"/>.</summary>
	/// <exception cref="FormatException">The span is not a valid ULID or GUID.</exception>
	public static Id<T> Parse (ReadOnlySpan<char> s, IFormatProvider? provider)
	{
		if (TryParse(s, provider, out var id)) return id;

		throw new FormatException("Could not parse value into a valid Ulid or Guid");
	}

	/// <summary>Attempts to parse a ULID or GUID span into an <see cref="Id{T}"/>.</summary>
	public static bool TryParse (ReadOnlySpan<char> s, IFormatProvider? provider, out Id<T> id)
	{
		if (Ulid.TryParse(s, out var ulid))
		{
			id = new Id<T>(ulid);
			return true;
		}

		if (Guid.TryParse(s, out var guid))
		{
			id = new Id<T>(guid);
			return true;
		}

		id = new Id<T>();
		return false;
	}

	/// <summary>Returns <see langword="true"/> if <paramref name="s"/> can be parsed as an <see cref="Id{T}"/>.</summary>
	public static bool IsValid (string? s) => TryParse(s, out _);

	/// <summary>Converts this typed <see cref="Id{T}"/> into a non-generic <see cref="Id"/>.</summary>
	public Id ToId () => new(Value);

	/// <summary>Parses a ULID or GUID string into an <see cref="Id{T}"/>.</summary>
	/// <exception cref="FormatException">The string is not a valid ULID or GUID.</exception>
	public static Id<T> Parse (string value)
	{
		if (Ulid.TryParse(value, out var ulid)) return new Id<T>(ulid);

		if (Guid.TryParse(value, out var guid)) return new Id<T>(guid);

		throw new FormatException("Could not parse value into a valid Ulid or Guid");
	}

	/// <summary>Parses a ULID from its UTF-8 Crockford base32 byte representation.</summary>
	public static Id<T> Parse (ReadOnlySpan<byte> base32) => new(Ulid.Parse(base32));

	/// <summary>Creates an <see cref="Id{T}"/> from a non-generic <see cref="Id"/>.</summary>
	public static Id<T> From (Id id) => new(id.Value);

	/// <summary>Generates a new random <see cref="Id{T}"/>.</summary>
	public static Id<T> NewId () => new(Ulid.NewUlid());

	/// <summary>Creates an <see cref="Id{T}"/> from a <see cref="Guid"/>.</summary>
	public static Id<T> Parse (Guid guid) => new(new Ulid(guid));

	/// <summary>Attempts to parse a ULID or GUID string into an <see cref="Id{T}"/>.</summary>
	public static bool TryParse (string? value, out Id<T> id)
	{
		if (Ulid.TryParse(value, out var ulid))
		{
			id = new Id<T>(ulid);
			return true;
		}

		if (Guid.TryParse(value, out var guid))
		{
			id = new Id<T>(guid);
			return true;
		}

		id = new Id<T>();
		return false;
	}

	/// <summary>Explicitly converts a ULID or GUID string to an <see cref="Id{T}"/>.</summary>
	public static explicit operator Id<T> (string value) => new(value);

	/// <summary>Implicitly converts a <see cref="Ulid"/> to an <see cref="Id{T}"/>.</summary>
	public static implicit operator Id<T> (Ulid value) => new(value);

	/// <summary>Explicitly converts an <see cref="Id{T}"/> to a non-generic <see cref="Id"/>.</summary>
	public static explicit operator Id (Id<T> value) => value.ToId();

	/// <summary>Explicitly converts an <see cref="Id{T}"/> to its canonical string form.</summary>
	public static explicit operator string (Id<T> value) => value.ToString();

	/// <summary>Implicitly converts a <see cref="Guid"/> to an <see cref="Id{T}"/>.</summary>
	public static implicit operator Id<T> (Guid value) => new(value);

	/// <summary>Explicitly converts an <see cref="Id{T}"/> to a <see cref="Guid"/>.</summary>
	public static explicit operator Guid (Id<T> value) => value.ToGuid();

	/// <summary>Implicitly converts a non-generic <see cref="Id"/> to an <see cref="Id{T}"/>.</summary>
	public static implicit operator Id<T> (Id value) => new(value);
}
