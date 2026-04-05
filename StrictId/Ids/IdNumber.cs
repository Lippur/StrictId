using System.Diagnostics;
using System.Runtime.Serialization;
using StrictId.Internal;

namespace StrictId;

/// <summary>
/// A non-generic, type-erased integer-backed StrictId. Wraps a <see cref="ulong"/>
/// (0..18446744073709551615) and is intended for type-erased numeric-ID scenarios such
/// as logging, diagnostics, and generic plumbing. For type-safe numeric identifiers
/// that cannot be mixed across entities, prefer <see cref="IdNumber{T}"/>.
/// </summary>
/// <remarks>
/// StrictId never invents a numeric ID client-side: there is no <c>NewId()</c> method
/// for the numeric family. Numeric IDs come from the database (via EF Core's identity
/// columns) or from the user's own code.
/// </remarks>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct IdNumber (ulong Value) : IStrictId<IdNumber>, IComparable
{
	/// <summary>Creates an <see cref="IdNumber"/> from a signed 32-bit integer.</summary>
	/// <exception cref="OverflowException"><paramref name="value"/> is negative.</exception>
	public IdNumber (int value) : this(checked((ulong)value)) { }

	/// <summary>Creates an <see cref="IdNumber"/> from a signed 64-bit integer.</summary>
	/// <exception cref="OverflowException"><paramref name="value"/> is negative.</exception>
	public IdNumber (long value) : this(checked((ulong)value)) { }

	/// <summary>Creates an <see cref="IdNumber"/> by parsing the provided string.</summary>
	/// <param name="value">A bare sequence of decimal digits.</param>
	/// <exception cref="FormatException">The value is not a valid bare decimal.</exception>
	public IdNumber (string value) : this(Parse(value).Value) { }

	/// <summary>Creates a copy of an existing <see cref="IdNumber"/>.</summary>
	public IdNumber (IdNumber other) : this(other.Value) { }

	/// <summary>The zero/empty <see cref="IdNumber"/>.</summary>
	public static IdNumber Empty => default;

	/// <summary>The smallest possible <see cref="IdNumber"/> value. Equivalent to <see cref="Empty"/>.</summary>
	public static IdNumber MinValue => new(ulong.MinValue);

	/// <summary>The largest possible <see cref="IdNumber"/> value (<see cref="ulong.MaxValue"/>).</summary>
	public static IdNumber MaxValue => new(ulong.MaxValue);

	/// <summary>
	/// <see langword="true"/> if <see cref="Value"/> is non-zero,
	/// <see langword="false"/> if it equals <see cref="Empty"/>.
	/// </summary>
	[IgnoreDataMember]
	public bool HasValue => Value != 0;

	/// <summary>Compares this <see cref="IdNumber"/> to another by underlying numeric value.</summary>
	public int CompareTo (IdNumber other) => Value.CompareTo(other.Value);

	/// <inheritdoc />
	public int CompareTo (object? obj)
	{
		if (obj is null) return 1;
		if (obj is IdNumber other) return CompareTo(other);
		throw new ArgumentException($"Argument must be of type {nameof(IdNumber)}.", nameof(obj));
	}

	/// <summary>Returns the canonical decimal-digit string form.</summary>
	public override string ToString () => IdNumberFormatter.Format(Value, PrefixInfo.None, default);

	/// <summary>Formats this <see cref="IdNumber"/> using the given format specifier.</summary>
	/// <param name="format">
	/// One of <c>C</c> (canonical, default) or <c>B</c> (bare digits). For the non-generic
	/// <see cref="IdNumber"/> these are equivalent since there is no prefix to strip.
	/// </param>
	/// <param name="formatProvider">Ignored; StrictIds are culture-invariant.</param>
	/// <exception cref="FormatException">The format specifier is not recognised.</exception>
	public string ToString (string? format, IFormatProvider? formatProvider)
		=> IdNumberFormatter.Format(Value, PrefixInfo.None, format.AsSpan());

	/// <summary>Formats this <see cref="IdNumber"/> using the given format specifier.</summary>
	public string ToString (string? format) => ToString(format, null);

	/// <summary>Attempts to write this <see cref="IdNumber"/> into the provided character span.</summary>
	public bool TryFormat (
		Span<char> destination,
		out int charsWritten,
		ReadOnlySpan<char> format,
		IFormatProvider? provider
	) => IdNumberFormatter.TryFormat(Value, PrefixInfo.None, destination, out charsWritten, format);

	/// <summary>Attempts to write this <see cref="IdNumber"/> as UTF-8 bytes into the provided span.</summary>
	public bool TryFormat (
		Span<byte> utf8Destination,
		out int bytesWritten,
		ReadOnlySpan<char> format,
		IFormatProvider? provider
	) => IdNumberFormatter.TryFormat(Value, PrefixInfo.None, utf8Destination, out bytesWritten, format);

	/// <summary>Returns the underlying 64-bit unsigned integer.</summary>
	public ulong ToUInt64 () => Value;

	/// <summary>
	/// Returns the underlying value as a signed 64-bit integer.
	/// </summary>
	/// <exception cref="OverflowException">
	/// The underlying value is greater than <see cref="long.MaxValue"/>.
	/// </exception>
	public long ToInt64 () => checked((long)Value);

	/// <summary>Parses a bare decimal string into an <see cref="IdNumber"/>.</summary>
	/// <exception cref="FormatException">The string is not a valid bare decimal.</exception>
	public static IdNumber Parse (string s)
	{
		if (IdNumberParser.TryParseUInt64(s.AsSpan(), PrefixInfo.None, out var value))
			return new IdNumber(value);
		throw IdNumberParser.BuildParseException(s, PrefixInfo.None, nameof(IdNumber));
	}

	/// <inheritdoc cref="Parse(string)" />
	public static IdNumber Parse (string s, IFormatProvider? provider) => Parse(s);

	/// <summary>Parses a bare decimal span into an <see cref="IdNumber"/>.</summary>
	/// <exception cref="FormatException">The span is not a valid bare decimal.</exception>
	public static IdNumber Parse (ReadOnlySpan<char> s, IFormatProvider? provider)
	{
		if (IdNumberParser.TryParseUInt64(s, PrefixInfo.None, out var value))
			return new IdNumber(value);
		throw IdNumberParser.BuildParseException(s.ToString(), PrefixInfo.None, nameof(IdNumber));
	}

	/// <summary>Attempts to parse a bare decimal string into an <see cref="IdNumber"/>.</summary>
	public static bool TryParse (string? s, out IdNumber result)
	{
		if (s is not null && IdNumberParser.TryParseUInt64(s.AsSpan(), PrefixInfo.None, out var value))
		{
			result = new IdNumber(value);
			return true;
		}
		result = default;
		return false;
	}

	/// <inheritdoc cref="TryParse(string?, out IdNumber)" />
	public static bool TryParse (string? s, IFormatProvider? provider, out IdNumber result) => TryParse(s, out result);

	/// <summary>Attempts to parse a bare decimal span into an <see cref="IdNumber"/>.</summary>
	public static bool TryParse (ReadOnlySpan<char> s, IFormatProvider? provider, out IdNumber result)
	{
		if (IdNumberParser.TryParseUInt64(s, PrefixInfo.None, out var value))
		{
			result = new IdNumber(value);
			return true;
		}
		result = default;
		return false;
	}

	/// <summary>Returns <see langword="true"/> if <paramref name="s"/> can be parsed as an <see cref="IdNumber"/>.</summary>
	public static bool IsValid (string? s) => TryParse(s, out _);

	/// <summary>Implicitly converts a <see cref="ulong"/> to an <see cref="IdNumber"/>.</summary>
	public static implicit operator IdNumber (ulong value) => new(value);

	/// <summary>Implicitly converts a <see cref="long"/> to an <see cref="IdNumber"/>. Throws <see cref="OverflowException"/> on negative values.</summary>
	public static implicit operator IdNumber (long value) => new(value);

	/// <summary>
	/// Implicitly converts an <see cref="int"/> to an <see cref="IdNumber"/>. Throws
	/// <see cref="OverflowException"/> on negative values. Integer widths smaller than
	/// <see cref="int"/> (e.g. <see cref="byte"/>, <see cref="short"/>) may need an
	/// explicit cast to <see cref="int"/>, <see cref="long"/>, or <see cref="ulong"/>
	/// due to how C# resolves chained implicit conversions.
	/// </summary>
	public static implicit operator IdNumber (int value) => new(value);

	/// <summary>Explicitly converts a bare decimal string to an <see cref="IdNumber"/>.</summary>
	public static explicit operator IdNumber (string value) => Parse(value);

	/// <summary>Explicitly converts an <see cref="IdNumber"/> to its underlying <see cref="ulong"/>.</summary>
	public static explicit operator ulong (IdNumber value) => value.Value;

	/// <summary>Explicitly converts an <see cref="IdNumber"/> to a <see cref="long"/>. Throws if the value exceeds <see cref="long.MaxValue"/>.</summary>
	public static explicit operator long (IdNumber value) => checked((long)value.Value);

	/// <summary>Explicitly converts an <see cref="IdNumber"/> to its canonical decimal string form.</summary>
	public static explicit operator string (IdNumber value) => value.ToString();
}
