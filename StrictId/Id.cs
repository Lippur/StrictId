using System.Diagnostics;
using System.Text.Json.Serialization;
using StrictId.Json;

// ReSharper disable once CheckNamespace
namespace StrictId;

[DebuggerDisplay("{ToString(),nq}"), JsonConverter(typeof(IdJsonConverter))]
public readonly record struct Id (Ulid Value)
	: IComparable<Id>, IComparable, ISpanFormattable, IUtf8SpanFormattable, ISpanParsable<Id>
{
	public Id (string value) : this(Parse(value)) { }
	public Id () : this(Ulid.Empty) { }
	public Id (Guid guid) : this(new Ulid(guid)) { }
	public Id (Id id) : this(id.Value) { }

	public static Id Empty => new();
	public static Id MaxValue => new(Ulid.MaxValue);
	public static Id MinValue => new(Ulid.MinValue);
	public bool HasValue => Value != Ulid.Empty;

	public byte[] Random => Value.Random;
	public DateTimeOffset Time => Value.Time;
	public int CompareTo (object? obj) => Value.CompareTo(obj);

	public int CompareTo (Id other) => Value.CompareTo(other.Value);

	public string ToString (string? format, IFormatProvider? formatProvider) => Value.ToString(format, formatProvider);

	public bool TryFormat (
		Span<char> destination,
		out int charsWritten,
		ReadOnlySpan<char> format,
		IFormatProvider? provider
	) => Value.TryFormat(destination, out charsWritten, format, provider);

	public static Id Parse (string s, IFormatProvider? provider) => Parse(s);

	public static bool TryParse (string? s, IFormatProvider? provider, out Id result) => TryParse(s, out result);

	public static Id Parse (ReadOnlySpan<char> s, IFormatProvider? provider) => new(Ulid.Parse(s, provider));

	public static bool TryParse (ReadOnlySpan<char> s, IFormatProvider? provider, out Id result)
	{
		var success = Ulid.TryParse(s, provider, out var ulid);
		result = ulid;
		return success;
	}

	public bool TryFormat (
		Span<byte> utf8Destination,
		out int bytesWritten,
		ReadOnlySpan<char> format,
		IFormatProvider? provider
	) => Value.TryFormat(utf8Destination, out bytesWritten, format, provider);

	public static bool IsValid (string? s) => TryParse(s, out _);

	public static Id Parse (string value)
	{
		if (Ulid.TryParse(value, out var ulid)) return new Id(ulid);

		if (Guid.TryParse(value, out var guid)) return new Id(guid);

		throw new ArgumentException("Could not parse value into a valid Ulid or Guid");
	}

	public static Id Parse (ReadOnlySpan<byte> base32) => new(Ulid.Parse(base32));
	public static Id From (Id id) => new(id.Value);
	public static Id NewId () => new(Ulid.NewUlid());

	public static string ToString (Id id) => id.ToString();
	public Guid ToGuid () => Value.ToGuid();

	public static Id Parse (Guid guid) => new(new Ulid(guid));

	public static bool TryParse (string? value, out Id id)
	{
		var result = Ulid.TryParse(value, out var ulid);
		id = new Id(ulid);
		return result;
	}

	public override string ToString () => Value.ToString();

	public string ToBase64 () => Value.ToBase64();

	public byte[] ToByteArray () => Value.ToByteArray();

	public static implicit operator Id (string value) => new(value);
	public static implicit operator Id (Ulid value) => new(value);
	public static implicit operator Id (Guid value) => new(value);
	public static explicit operator Guid (Id value) => value.ToGuid();
	public static explicit operator string (Id value) => value.ToString();
	public static explicit operator Ulid (Id value) => value.Value;
}

[DebuggerDisplay("{ToString(),nq}"), JsonConverter(typeof(IdTypedJsonConverterFactory))]
public readonly record struct Id<T> (Ulid Value)
	: IComparable<Id<T>>,
		IComparable<Id>,
		IComparable,
		ISpanFormattable,
		IUtf8SpanFormattable,
		ISpanParsable<Id<T>>
{
	public Id (string value) : this(Parse(value)) { }

	public Id () : this(Ulid.Empty) { }
	public Id (Guid guid) : this(Parse(guid)) { }
	public Id (Id id) : this(id.Value) { }
	public Id (Id<T> id) : this(id.Value) { }
	public Id IdValue => new(Value);

	public static Id<T> Empty => new();
	public static Id<T> MaxValue => new(Ulid.MaxValue);
	public static Id<T> MinValue => new(Ulid.MinValue);
	public bool HasValue => Value != Ulid.Empty;

	public byte[] Random => Value.Random;
	public DateTimeOffset Time => Value.Time;
	public int CompareTo (object? obj) => Value.CompareTo(obj);

	public int CompareTo (Id<T> other) => Value.CompareTo(other.Value);

	public int CompareTo (Id other) => Value.CompareTo(other.Value);

	public string ToString (string? format, IFormatProvider? formatProvider) => Value.ToString(format, formatProvider);

	public bool TryFormat (
		Span<char> destination,
		out int charsWritten,
		ReadOnlySpan<char> format,
		IFormatProvider? provider
	) => Value.TryFormat(destination, out charsWritten, format, provider);

	static Id<T> IParsable<Id<T>>.Parse (string s, IFormatProvider? provider) => new(Parse(s, provider));

	public static bool TryParse (string? s, IFormatProvider? provider, out Id<T> result) => TryParse(s, out result);

	public static Id<T> Parse (ReadOnlySpan<char> s, IFormatProvider? provider) => new(Ulid.Parse(s, provider));

	public static bool TryParse (ReadOnlySpan<char> s, IFormatProvider? provider, out Id<T> result)
	{
		var success = Ulid.TryParse(s, provider, out var ulid);
		result = ulid;
		return success;
	}

	public bool TryFormat (
		Span<byte> utf8Destination,
		out int bytesWritten,
		ReadOnlySpan<char> format,
		IFormatProvider? provider
	) => Value.TryFormat(utf8Destination, out bytesWritten, format, provider);

	public static bool IsValid (string? s) => TryParse(s, out _);

	public static Id<T> Parse (string value)
	{
		if (Ulid.TryParse(value, out var ulid)) return new Id<T>(ulid);

		if (Guid.TryParse(value, out var guid)) return new Id<T>(guid);

		throw new ArgumentException("Could not parse value into a valid Ulid or Guid");
	}

	public static Id<T> Parse (ReadOnlySpan<byte> base32) => new(Ulid.Parse(base32));
	public static Id<T> From (Id id) => new(id.Value);
	public static Id<T> NewId () => new(Ulid.NewUlid());
	public Guid ToGuid () => Value.ToGuid();

	public static Id<T> Parse (Guid guid) => new(new Ulid(guid));

	public static bool TryParse (string? value, out Id<T> id)
	{
		var result = Ulid.TryParse(value, out var ulid);
		id = new Id<T>(ulid);
		return result;
	}

	public override string ToString () => Value.ToString();

	public string ToBase64 () => Value.ToBase64();

	public byte[] ToByteArray () => Value.ToByteArray();

	public static explicit operator Id<T> (string value) => new(value);
	public static implicit operator Id<T> (Ulid value) => new(value);
	public static implicit operator Id (Id<T> value) => value.IdValue;
	public static implicit operator string (Id<T> value) => value.ToString();
}