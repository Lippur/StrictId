using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using StrictId.Internal;
using StrictId.Json;

namespace StrictId;

/// <summary>
/// A non-generic, type-erased StrictId backed by a <see cref="Ulid"/>.
/// For type-safe identifiers, use <see cref="Id{T}"/>.
/// </summary>
[DebuggerDisplay("{ToString(),nq}"), JsonConverter(typeof(IdJsonConverter))]
public readonly record struct Id (Ulid Value) : IStrictId<Id>, IComparable
{
	/// <summary>Creates an <see cref="Id"/> from a <see cref="Guid"/>.</summary>
	/// <param name="guid">The GUID to wrap.</param>
	public Id (Guid guid) : this(new Ulid(guid)) { }

	/// <summary>Creates a copy of an existing <see cref="Id"/>.</summary>
	/// <param name="id">The id to copy.</param>
	public Id (Id id) : this(id.Value) { }

	/// <summary>The empty (all-zero) <see cref="Id"/>.</summary>
	public static Id Empty => default;

	/// <summary>The largest possible <see cref="Id"/> value.</summary>
	public static Id MaxValue => new(Ulid.MaxValue);

	/// <summary>The smallest possible <see cref="Id"/> value. Equivalent to <see cref="Empty"/>.</summary>
	public static Id MinValue => new(Ulid.MinValue);

	/// <summary>
	/// <see langword="true"/> if this <see cref="Id"/> has a non-empty value,
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

	/// <summary>Compares this <see cref="Id"/> to another using lexicographic ULID ordering.</summary>
	public int CompareTo (Id other) => Value.CompareTo(other.Value);

	/// <inheritdoc />
	public int CompareTo (object? obj)
	{
		if (obj is null) return 1;
		if (obj is Id other) return CompareTo(other);
		throw new ArgumentException($"Argument must be of type {nameof(Id)}.", nameof(obj));
	}

	/// <summary>Returns the canonical lowercase 26-character Crockford base32 ULID string.</summary>
	public override string ToString () => IdFormatter.Format(Value, PrefixInfo.None, default);

	/// <summary>Formats this <see cref="Id"/> using the given format specifier.</summary>
	/// <param name="format">
	/// One of <c>C</c> (canonical, default), <c>B</c> (bare ULID), <c>G</c> (canonical GUID),
	/// <c>BG</c> (bare GUID), or <c>U</c> (uppercase ULID, v2 compatibility).
	/// </param>
	/// <param name="formatProvider">Ignored; StrictIds are culture-invariant.</param>
	/// <exception cref="FormatException">The format specifier is not recognised.</exception>
	public string ToString (string? format, IFormatProvider? formatProvider)
		=> IdFormatter.Format(Value, PrefixInfo.None, format.AsSpan());

	/// <summary>Formats this <see cref="Id"/> using the given format specifier.</summary>
	public string ToString (string? format) => ToString(format, null);

	/// <summary>Attempts to write this <see cref="Id"/> into the provided character span.</summary>
	public bool TryFormat (
		Span<char> destination,
		out int charsWritten,
		ReadOnlySpan<char> format,
		IFormatProvider? provider
	) => IdFormatter.TryFormat(Value, PrefixInfo.None, destination, out charsWritten, format);

	/// <summary>Attempts to write this <see cref="Id"/> as UTF-8 bytes into the provided span.</summary>
	public bool TryFormat (
		Span<byte> utf8Destination,
		out int bytesWritten,
		ReadOnlySpan<char> format,
		IFormatProvider? provider
	) => IdFormatter.TryFormat(Value, PrefixInfo.None, utf8Destination, out bytesWritten, format);

	/// <summary>Returns the underlying <see cref="Ulid"/>.</summary>
	public Ulid ToUlid () => Value;

	/// <summary>Converts this <see cref="Id"/> to its equivalent <see cref="Guid"/> representation.</summary>
	public Guid ToGuid () => Value.ToGuid();

	/// <summary>Returns a Base64 representation of the underlying 16 bytes.</summary>
	public string ToBase64 () => Value.ToBase64();

	/// <summary>Returns the underlying 16-byte representation.</summary>
	public byte[] ToByteArray () => Value.ToByteArray();

	/// <summary>Parses a bare ULID or GUID string into an <see cref="Id"/>.</summary>
	/// <exception cref="FormatException">The string is not a valid bare ULID or GUID.</exception>
	public static Id Parse (string s)
	{
		if (IdParser.TryParseUlid(s.AsSpan(), PrefixInfo.None, out var value))
			return new Id(value);
		throw IdParser.BuildParseException(s, PrefixInfo.None, nameof(Id));
	}

	/// <inheritdoc cref="Parse(string)" />
	public static Id Parse (string s, IFormatProvider? provider)
	{
		var strict = IdFormat.IsPrefixRequired(provider);
		if (IdParser.TryParseUlid(s.AsSpan(), PrefixInfo.None, out var value, strict))
			return new Id(value);
		throw IdParser.BuildParseException(s, PrefixInfo.None, nameof(Id), strict);
	}

	/// <summary>Parses a bare ULID or GUID span into an <see cref="Id"/>.</summary>
	/// <exception cref="FormatException">The span is not a valid bare ULID or GUID.</exception>
	public static Id Parse (ReadOnlySpan<char> s, IFormatProvider? provider)
	{
		var strict = IdFormat.IsPrefixRequired(provider);
		if (IdParser.TryParseUlid(s, PrefixInfo.None, out var value, strict))
			return new Id(value);
		throw IdParser.BuildParseException(s.ToString(), PrefixInfo.None, nameof(Id), strict);
	}

	/// <summary>Attempts to parse a bare ULID or GUID string into an <see cref="Id"/>.</summary>
	public static bool TryParse (string? s, out Id result)
	{
		if (s is not null && IdParser.TryParseUlid(s.AsSpan(), PrefixInfo.None, out var value))
		{
			result = new Id(value);
			return true;
		}
		result = default;
		return false;
	}

	/// <inheritdoc cref="TryParse(string?, out Id)" />
	public static bool TryParse (string? s, IFormatProvider? provider, out Id result)
	{
		var strict = IdFormat.IsPrefixRequired(provider);
		if (s is not null && IdParser.TryParseUlid(s.AsSpan(), PrefixInfo.None, out var value, strict))
		{
			result = new Id(value);
			return true;
		}
		result = default;
		return false;
	}

	/// <summary>Attempts to parse a bare ULID or GUID span into an <see cref="Id"/>.</summary>
	public static bool TryParse (ReadOnlySpan<char> s, IFormatProvider? provider, out Id result)
	{
		var strict = IdFormat.IsPrefixRequired(provider);
		if (IdParser.TryParseUlid(s, PrefixInfo.None, out var value, strict))
		{
			result = new Id(value);
			return true;
		}
		result = default;
		return false;
	}

	/// <summary>Returns <see langword="true"/> if <paramref name="s"/> can be parsed as an <see cref="Id"/>.</summary>
	public static bool IsValid (string? s) => TryParse(s, out _);

	/// <summary>Generates a new <see cref="Id"/>. The underlying ULID generation is delegated to <see cref="Ulid.NewUlid()"/>.</summary>
	public static Id NewId () => new(Ulid.NewUlid());

	/// <summary>Generates a new <see cref="Id"/> with the given timestamp and fresh randomness.</summary>
	/// <param name="timestamp">The timestamp to embed in the ULID's high bits.</param>
	public static Id NewId (DateTimeOffset timestamp) => new(Ulid.NewUlid(timestamp));

	/// <summary>Implicitly converts a <see cref="Ulid"/> to an <see cref="Id"/>.</summary>
	public static implicit operator Id (Ulid value) => new(value);

	/// <summary>Implicitly converts a <see cref="Guid"/> to an <see cref="Id"/>.</summary>
	public static implicit operator Id (Guid value) => new(value);

	/// <summary>Explicitly converts a bare ULID or GUID string to an <see cref="Id"/>.</summary>
	public static explicit operator Id (string value) => Parse(value);

	/// <summary>Explicitly converts an <see cref="Id"/> to its underlying <see cref="Ulid"/>.</summary>
	public static explicit operator Ulid (Id value) => value.Value;

	/// <summary>Explicitly converts an <see cref="Id"/> to its equivalent <see cref="Guid"/>.</summary>
	public static explicit operator Guid (Id value) => value.Value.ToGuid();

	/// <summary>Explicitly converts an <see cref="Id"/> to its canonical string form.</summary>
	public static explicit operator string (Id value) => value.ToString();
}
