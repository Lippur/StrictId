﻿using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using StrictId.Json;

namespace StrictId;

[DebuggerDisplay("{ToString(),nq}"), JsonConverter(typeof(IdJsonConverter))]
public readonly record struct Id (Ulid Value) : IId, IComparable<Id>, ISpanParsable<Id>
{
	public Id (string value) : this(Parse(value)) { }
	public Id () : this(Ulid.Empty) { }
	public Id (Guid guid) : this(new Ulid(guid)) { }
	public Id (Id id) : this(id.Value) { }

	public static Id Empty => new();
	public static Id MaxValue => new(Ulid.MaxValue);
	public static Id MinValue => new(Ulid.MinValue);

	public int CompareTo (Id other) => Value.CompareTo(other.Value);

	[IgnoreDataMember]
	public bool HasValue => Value != Ulid.Empty;

	[IgnoreDataMember]
	public byte[] Random => Value.Random;

	[IgnoreDataMember]
	public DateTimeOffset Time => Value.Time;

	public int CompareTo (object? obj) => Value.CompareTo(obj);
	public string ToString (string? format, IFormatProvider? formatProvider) => Value.ToString(format, formatProvider);

	public bool TryFormat (
		Span<char> destination,
		out int charsWritten,
		ReadOnlySpan<char> format,
		IFormatProvider? provider
	) => Value.TryFormat(destination, out charsWritten, format, provider);

	public bool TryFormat (
		Span<byte> utf8Destination,
		out int bytesWritten,
		ReadOnlySpan<char> format,
		IFormatProvider? provider
	) => Value.TryFormat(utf8Destination, out bytesWritten, format, provider);

	public Guid ToGuid () => Value.ToGuid();

	public override string ToString () => Value.ToString();

	public string ToBase64 () => Value.ToBase64();

	public byte[] ToByteArray () => Value.ToByteArray();

	public static Id Parse (string s, IFormatProvider? provider) => Parse(s);

	public static bool TryParse (string? s, IFormatProvider? provider, out Id result) => TryParse(s, out result);

	public static Id Parse (ReadOnlySpan<char> s, IFormatProvider? provider)
	{
		if (TryParse(s, provider, out var id)) return id;

		throw new ArgumentException("Could not parse value into a valid Ulid or Guid");
	}

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

	public static Id Parse (Guid guid) => new(new Ulid(guid));

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

	public static explicit operator Id (string value) => new(value);
	public static implicit operator Id (Ulid value) => new(value);
	public static implicit operator Id (Guid value) => new(value);
	public static explicit operator Guid (Id value) => value.ToGuid();
	public static explicit operator string (Id value) => value.ToString();
	public static explicit operator Ulid (Id value) => value.Value;
}

[DebuggerDisplay("{ToString(),nq}"), JsonConverter(typeof(IdTypedJsonConverterFactory))]
public readonly record struct Id<T> (Ulid Value)
	: IId,
		IComparable<Id<T>>,
		IComparable<Id>,
		ISpanParsable<Id<T>>
{
	public Id (string value) : this(Parse(value)) { }

	public Id () : this(Ulid.Empty) { }
	public Id (Guid guid) : this(Parse(guid)) { }
	public Id (Id id) : this(id.Value) { }
	public Id (Id<T> id) : this(id.Value) { }

	public static Id<T> Empty => new();
	public static Id<T> MaxValue => new(Ulid.MaxValue);
	public static Id<T> MinValue => new(Ulid.MinValue);
	public int CompareTo (Id<T> other) => Value.CompareTo(other.Value);
	public int CompareTo (Id other) => Value.CompareTo(other.Value);

	[IgnoreDataMember]
	public bool HasValue => Value != Ulid.Empty;

	[IgnoreDataMember]
	public byte[] Random => Value.Random;

	[IgnoreDataMember]
	public DateTimeOffset Time => Value.Time;

	public int CompareTo (object? obj) => Value.CompareTo(obj);

	public string ToString (string? format, IFormatProvider? formatProvider) => Value.ToString(format, formatProvider);

	public bool TryFormat (
		Span<char> destination,
		out int charsWritten,
		ReadOnlySpan<char> format,
		IFormatProvider? provider
	) => Value.TryFormat(destination, out charsWritten, format, provider);

	public bool TryFormat (
		Span<byte> utf8Destination,
		out int bytesWritten,
		ReadOnlySpan<char> format,
		IFormatProvider? provider
	) => Value.TryFormat(utf8Destination, out bytesWritten, format, provider);

	public Guid ToGuid () => Value.ToGuid();

	public override string ToString () => Value.ToString();
	public string ToBase64 () => Value.ToBase64();

	public byte[] ToByteArray () => Value.ToByteArray();

	static Id<T> IParsable<Id<T>>.Parse (string s, IFormatProvider? provider) => new(Parse(s, provider));

	public static bool TryParse (string? s, IFormatProvider? provider, out Id<T> result) => TryParse(s, out result);

	public static Id<T> Parse (ReadOnlySpan<char> s, IFormatProvider? provider)
	{
		if (TryParse(s, provider, out var id)) return id;

		throw new ArgumentException("Could not parse value into a valid Ulid or Guid");
	}

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

	public static bool IsValid (string? s) => TryParse(s, out _);

	public Id ToId () => new(Value);

	public static Id<T> Parse (string value)
	{
		if (Ulid.TryParse(value, out var ulid)) return new Id<T>(ulid);

		if (Guid.TryParse(value, out var guid)) return new Id<T>(guid);

		throw new ArgumentException("Could not parse value into a valid Ulid or Guid");
	}

	public static Id<T> Parse (ReadOnlySpan<byte> base32) => new(Ulid.Parse(base32));
	public static Id<T> From (Id id) => new(id.Value);
	public static Id<T> NewId () => new(Ulid.NewUlid());

	public static Id<T> Parse (Guid guid) => new(new Ulid(guid));

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

	public static explicit operator Id<T> (string value) => new(value);
	public static implicit operator Id<T> (Ulid value) => new(value);
	public static explicit operator Id (Id<T> value) => value.ToId();
	public static explicit operator string (Id<T> value) => value.ToString();
	public static implicit operator Id<T> (Guid value) => new(value);
	public static explicit operator Guid (Id<T> value) => value.ToGuid();
	public static implicit operator Id<T> (Id value) => new(value);
}