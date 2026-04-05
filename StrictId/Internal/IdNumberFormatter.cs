using System.Globalization;

namespace StrictId.Internal;

/// <summary>
/// Canonical formatter for integer-backed StrictIds. Writes prefix-aware canonical or
/// bare decimal-digit forms into a destination span; the <c>string</c>-returning
/// overload uses a stack buffer internally.
/// </summary>
internal static class IdNumberFormatter
{
	// 63-char prefix + 1-char separator + 20 digits (max ulong = 18446744073709551615) = 84.
	private const int MaxFormattedLength = 84;

	/// <summary>
	/// Formats <paramref name="value"/> into a newly-allocated string using the supplied
	/// prefix metadata and format specifier. <paramref name="format"/> accepts
	/// <c>C</c> (canonical, default) or <c>B</c> (bare digits); any other value throws
	/// <see cref="FormatException"/>.
	/// </summary>
	public static string Format (ulong value, PrefixInfo prefix, ReadOnlySpan<char> format)
	{
		Span<char> buffer = stackalloc char[MaxFormattedLength];
		if (!TryFormat(value, prefix, buffer, out var charsWritten, format))
			throw new InvalidOperationException("Formatted IdNumber exceeded the maximum buffer length of 84 characters.");
		return new string(buffer[..charsWritten]);
	}

	/// <summary>
	/// Writes <paramref name="value"/> into <paramref name="destination"/> using the
	/// supplied prefix metadata and format specifier. Returns <see langword="false"/>
	/// without partial writes if the destination is too small.
	/// </summary>
	/// <exception cref="FormatException">The format specifier is not recognised.</exception>
	public static bool TryFormat (
		ulong value,
		PrefixInfo prefix,
		Span<char> destination,
		out int charsWritten,
		ReadOnlySpan<char> format
	)
	{
		if (format.IsEmpty || format.SequenceEqual("C"))
			return TryWriteCanonical(value, prefix, destination, out charsWritten);
		if (format.SequenceEqual("B"))
			return TryWriteBare(value, destination, out charsWritten);

		throw new FormatException(
			$"Unknown format specifier '{format.ToString()}' for an IdNumber. Valid specifiers: " +
			"'C' (canonical, default) and 'B' (bare decimal digits).");
	}

	/// <summary>
	/// UTF-8 entry point. All characters in a canonical <c>IdNumber</c> form are ASCII
	/// (decimal digits plus the separator), so we format into a <c>char</c> buffer and
	/// widen 1:1 into the UTF-8 span.
	/// </summary>
	public static bool TryFormat (
		ulong value,
		PrefixInfo prefix,
		Span<byte> utf8Destination,
		out int bytesWritten,
		ReadOnlySpan<char> format
	)
	{
		Span<char> temp = stackalloc char[MaxFormattedLength];
		if (!TryFormat(value, prefix, temp, out var charsWritten, format))
		{
			bytesWritten = 0;
			return false;
		}

		if (utf8Destination.Length < charsWritten)
		{
			bytesWritten = 0;
			return false;
		}

		for (var i = 0; i < charsWritten; i++)
			utf8Destination[i] = (byte)temp[i];

		bytesWritten = charsWritten;
		return true;
	}

	private static bool TryWriteCanonical (
		ulong value,
		PrefixInfo prefix,
		Span<char> destination,
		out int charsWritten
	)
	{
		if (!prefix.HasPrefix)
			return TryWriteBare(value, destination, out charsWritten);

		var prefixText = prefix.Canonical!;
		var prefixLen = prefixText.Length;

		if (destination.Length < prefixLen + 1)
		{
			charsWritten = 0;
			return false;
		}

		prefixText.AsSpan().CopyTo(destination);
		destination[prefixLen] = prefix.Separator.ToChar();

		if (!value.TryFormat(destination[(prefixLen + 1)..], out var digitsWritten, default, CultureInfo.InvariantCulture))
		{
			charsWritten = 0;
			return false;
		}

		charsWritten = prefixLen + 1 + digitsWritten;
		return true;
	}

	private static bool TryWriteBare (ulong value, Span<char> destination, out int charsWritten)
	{
		return value.TryFormat(destination, out charsWritten, default, CultureInfo.InvariantCulture);
	}
}
