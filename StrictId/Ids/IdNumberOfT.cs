using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using StrictId.Internal;
using StrictId.Json;

namespace StrictId;

/// <summary>
/// A strongly-typed integer StrictId for entities of type
/// <typeparamref name="T"/>. Backed by a <see cref="ulong"/>. Values of different
/// <typeparamref name="T"/>s cannot be assigned to or compared with each other, which
/// prevents accidentally mixing up identifiers across entities at compile time.
/// </summary>
/// <typeparam name="T">
/// The entity type this identifier belongs to. Used only as a compile-time tag and as
/// the key for per-type prefix resolution via <see cref="IdPrefixAttribute"/>.
/// </typeparam>
/// <remarks>
/// StrictId never invents a numeric ID client-side: there is no <c>NewId()</c> method
/// on this type. Numeric IDs come from the database (via EF Core's identity columns)
/// or from the user's own code.
/// </remarks>
[DebuggerDisplay("{ToString(),nq}"), JsonConverter(typeof(IdNumberTypedJsonConverterFactory))]
public readonly record struct IdNumber<T> (ulong Value) : IStrictId<IdNumber<T>>, IComparable
{
	/// <summary>Creates an <see cref="IdNumber{T}"/> from an 8-bit signed integer.</summary>
	/// <exception cref="OverflowException"><paramref name="value"/> is negative.</exception>
	public IdNumber (sbyte value) : this(checked((ulong)value)) { }

	/// <summary>Creates an <see cref="IdNumber{T}"/> from an 8-bit unsigned integer.</summary>
	public IdNumber (byte value) : this((ulong)value) { }

	/// <summary>Creates an <see cref="IdNumber{T}"/> from a signed 16-bit integer.</summary>
	/// <exception cref="OverflowException"><paramref name="value"/> is negative.</exception>
	public IdNumber (short value) : this(checked((ulong)value)) { }

	/// <summary>Creates an <see cref="IdNumber{T}"/> from an unsigned 16-bit integer.</summary>
	public IdNumber (ushort value) : this((ulong)value) { }

	/// <summary>Creates an <see cref="IdNumber{T}"/> from a signed 32-bit integer.</summary>
	/// <exception cref="OverflowException"><paramref name="value"/> is negative.</exception>
	public IdNumber (int value) : this(checked((ulong)value)) { }

	/// <summary>Creates an <see cref="IdNumber{T}"/> from an unsigned 32-bit integer.</summary>
	public IdNumber (uint value) : this((ulong)value) { }

	/// <summary>Creates an <see cref="IdNumber{T}"/> from a signed 64-bit integer.</summary>
	/// <exception cref="OverflowException"><paramref name="value"/> is negative.</exception>
	public IdNumber (long value) : this(checked((ulong)value)) { }

	/// <summary>Creates an <see cref="IdNumber{T}"/> by parsing the provided string.</summary>
	/// <param name="value">
	/// A bare sequence of decimal digits, or — if <typeparamref name="T"/> has an
	/// <see cref="IdPrefixAttribute"/> — a prefixed form <c>prefix_digits</c>.
	/// </param>
	/// <exception cref="FormatException">The value is not a valid <see cref="IdNumber{T}"/>.</exception>
	public IdNumber (string value) : this(Parse(value).Value) { }

	/// <summary>Creates an <see cref="IdNumber{T}"/> from a non-generic <see cref="IdNumber"/>, preserving the underlying value.</summary>
	public IdNumber (IdNumber other) : this(other.Value) { }

	/// <summary>Creates a copy of an existing <see cref="IdNumber{T}"/>.</summary>
	public IdNumber (IdNumber<T> other) : this(other.Value) { }

	/// <summary>The zero/empty <see cref="IdNumber{T}"/>.</summary>
	public static IdNumber<T> Empty => default;

	/// <summary>The smallest possible <see cref="IdNumber{T}"/> value. Equivalent to <see cref="Empty"/>.</summary>
	public static IdNumber<T> MinValue => new(ulong.MinValue);

	/// <summary>The largest possible <see cref="IdNumber{T}"/> value (<see cref="ulong.MaxValue"/>).</summary>
	public static IdNumber<T> MaxValue => new(ulong.MaxValue);

	/// <summary>
	/// <see langword="true"/> if <see cref="Value"/> is non-zero,
	/// <see langword="false"/> if it equals <see cref="Empty"/>.
	/// </summary>
	[IgnoreDataMember]
	public bool HasValue => Value != 0;

	/// <summary>Compares this <see cref="IdNumber{T}"/> to another of the same type.</summary>
	public int CompareTo (IdNumber<T> other) => Value.CompareTo(other.Value);

	/// <inheritdoc />
	public int CompareTo (object? obj)
	{
		if (obj is null) return 1;
		if (obj is IdNumber<T> other) return CompareTo(other);
		throw new ArgumentException($"Argument must be of type {nameof(IdNumber)}<{typeof(T).Name}>.", nameof(obj));
	}

	/// <summary>
	/// Returns the canonical string representation: <c>prefix_digits</c> if
	/// <typeparamref name="T"/> has a registered prefix, otherwise the bare decimal digits.
	/// </summary>
	public override string ToString () => IdNumberFormatter.Format(Value, StrictIdMetadata<T>.Prefix, default);

	/// <summary>Formats this <see cref="IdNumber{T}"/> using the given format specifier.</summary>
	/// <param name="format">One of <c>C</c> (canonical, default) or <c>B</c> (bare digits).</param>
	/// <param name="formatProvider">Ignored; StrictIds are culture-invariant.</param>
	/// <exception cref="FormatException">The format specifier is not recognised.</exception>
	public string ToString (string? format, IFormatProvider? formatProvider)
		=> IdNumberFormatter.Format(Value, StrictIdMetadata<T>.Prefix, format.AsSpan());

	/// <summary>Formats this <see cref="IdNumber{T}"/> using the given format specifier.</summary>
	public string ToString (string? format) => ToString(format, null);

	/// <summary>Attempts to write this <see cref="IdNumber{T}"/> into the provided character span.</summary>
	public bool TryFormat (
		Span<char> destination,
		out int charsWritten,
		ReadOnlySpan<char> format,
		IFormatProvider? provider
	) => IdNumberFormatter.TryFormat(Value, StrictIdMetadata<T>.Prefix, destination, out charsWritten, format);

	/// <summary>Attempts to write this <see cref="IdNumber{T}"/> as UTF-8 bytes into the provided span.</summary>
	public bool TryFormat (
		Span<byte> utf8Destination,
		out int bytesWritten,
		ReadOnlySpan<char> format,
		IFormatProvider? provider
	) => IdNumberFormatter.TryFormat(Value, StrictIdMetadata<T>.Prefix, utf8Destination, out bytesWritten, format);

	/// <summary>Returns the underlying 64-bit unsigned integer.</summary>
	public ulong ToUInt64 () => Value;

	/// <summary>
	/// Returns the underlying value as a signed 64-bit integer.
	/// </summary>
	/// <exception cref="OverflowException">
	/// The underlying value exceeds <see cref="long.MaxValue"/>.
	/// </exception>
	public long ToInt64 () => checked((long)Value);

	/// <summary>Converts this typed <see cref="IdNumber{T}"/> into a non-generic <see cref="IdNumber"/>, erasing the entity type.</summary>
	public IdNumber ToIdNumber () => new(Value);

	/// <summary>Parses a string into an <see cref="IdNumber{T}"/>.</summary>
	/// <exception cref="FormatException">
	/// The string is not a valid <see cref="IdNumber{T}"/>. The exception message includes the
	/// offending input, the expected shape, the registered prefix list for <typeparamref name="T"/>,
	/// and a best-effort diagnosis of the specific failure.
	/// </exception>
	public static IdNumber<T> Parse (string s)
	{
		if (IdNumberParser.TryParseUInt64(s.AsSpan(), StrictIdMetadata<T>.Prefix, out var value))
			return new IdNumber<T>(value);
		throw IdNumberParser.BuildParseException(s, StrictIdMetadata<T>.Prefix, $"{nameof(IdNumber)}<{typeof(T).Name}>");
	}

	/// <inheritdoc cref="Parse(string)" />
	public static IdNumber<T> Parse (string s, IFormatProvider? provider) => Parse(s);

	/// <summary>Parses a character span into an <see cref="IdNumber{T}"/>.</summary>
	/// <exception cref="FormatException">The span is not a valid <see cref="IdNumber{T}"/>.</exception>
	public static IdNumber<T> Parse (ReadOnlySpan<char> s, IFormatProvider? provider)
	{
		if (IdNumberParser.TryParseUInt64(s, StrictIdMetadata<T>.Prefix, out var value))
			return new IdNumber<T>(value);
		throw IdNumberParser.BuildParseException(s.ToString(), StrictIdMetadata<T>.Prefix, $"{nameof(IdNumber)}<{typeof(T).Name}>");
	}

	/// <summary>Attempts to parse a string into an <see cref="IdNumber{T}"/>.</summary>
	public static bool TryParse (string? s, out IdNumber<T> result)
	{
		if (s is not null && IdNumberParser.TryParseUInt64(s.AsSpan(), StrictIdMetadata<T>.Prefix, out var value))
		{
			result = new IdNumber<T>(value);
			return true;
		}
		result = default;
		return false;
	}

	/// <inheritdoc cref="TryParse(string?, out IdNumber{T})" />
	public static bool TryParse (string? s, IFormatProvider? provider, out IdNumber<T> result) => TryParse(s, out result);

	/// <summary>Attempts to parse a character span into an <see cref="IdNumber{T}"/>.</summary>
	public static bool TryParse (ReadOnlySpan<char> s, IFormatProvider? provider, out IdNumber<T> result)
	{
		if (IdNumberParser.TryParseUInt64(s, StrictIdMetadata<T>.Prefix, out var value))
		{
			result = new IdNumber<T>(value);
			return true;
		}
		result = default;
		return false;
	}

	/// <summary>Returns <see langword="true"/> if <paramref name="s"/> can be parsed as an <see cref="IdNumber{T}"/>.</summary>
	public static bool IsValid (string? s) => TryParse(s, out _);

	/// <summary>Implicitly converts a <see cref="ulong"/> to an <see cref="IdNumber{T}"/>.</summary>
	public static implicit operator IdNumber<T> (ulong value) => new(value);

	/// <summary>Implicitly converts a <see cref="long"/> to an <see cref="IdNumber{T}"/>. Throws <see cref="OverflowException"/> on negative values.</summary>
	public static implicit operator IdNumber<T> (long value) => new(value);

	/// <summary>Implicitly converts a <see cref="uint"/> to an <see cref="IdNumber{T}"/>.</summary>
	public static implicit operator IdNumber<T> (uint value) => new(value);

	/// <summary>Implicitly converts an <see cref="int"/> to an <see cref="IdNumber{T}"/>. Throws <see cref="OverflowException"/> on negative values.</summary>
	public static implicit operator IdNumber<T> (int value) => new(value);

	/// <summary>Implicitly converts a <see cref="ushort"/> to an <see cref="IdNumber{T}"/>.</summary>
	public static implicit operator IdNumber<T> (ushort value) => new(value);

	/// <summary>Implicitly converts a <see cref="short"/> to an <see cref="IdNumber{T}"/>. Throws <see cref="OverflowException"/> on negative values.</summary>
	public static implicit operator IdNumber<T> (short value) => new(value);

	/// <summary>Implicitly converts a <see cref="byte"/> to an <see cref="IdNumber{T}"/>.</summary>
	public static implicit operator IdNumber<T> (byte value) => new(value);

	/// <summary>Implicitly converts an <see cref="sbyte"/> to an <see cref="IdNumber{T}"/>. Throws <see cref="OverflowException"/> on negative values.</summary>
	public static implicit operator IdNumber<T> (sbyte value) => new(value);

	/// <summary>
	/// Implicitly converts a non-generic <see cref="IdNumber"/> to an <see cref="IdNumber{T}"/>.
	/// The non-generic form carries no entity type, so this conversion does not lose any
	/// information — the caller is simply ascribing a type.
	/// </summary>
	public static implicit operator IdNumber<T> (IdNumber value) => new(value.Value);

	/// <summary>Explicitly converts a string to an <see cref="IdNumber{T}"/> by parsing.</summary>
	public static explicit operator IdNumber<T> (string value) => Parse(value);

	/// <summary>
	/// Explicitly converts an <see cref="IdNumber{T}"/> to a non-generic <see cref="IdNumber"/>,
	/// discarding the entity type.
	/// </summary>
	public static explicit operator IdNumber (IdNumber<T> value) => value.ToIdNumber();

	/// <summary>Explicitly converts an <see cref="IdNumber{T}"/> to its underlying <see cref="ulong"/>.</summary>
	public static explicit operator ulong (IdNumber<T> value) => value.Value;

	/// <summary>Explicitly converts an <see cref="IdNumber{T}"/> to a <see cref="long"/>. Throws if the value exceeds <see cref="long.MaxValue"/>.</summary>
	public static explicit operator long (IdNumber<T> value) => checked((long)value.Value);

	/// <summary>Explicitly converts an <see cref="IdNumber{T}"/> to its canonical string form.</summary>
	public static explicit operator string (IdNumber<T> value) => value.ToString();
}
