namespace StrictId.Internal;

/// <summary>
/// Canonical formatter for Guid-backed StrictIds. Writes prefix-aware canonical forms
/// or bare standard Guid format specifiers to a destination span.
/// </summary>
internal static class GuidFormatter
{
	// Maximum possible formatted length: 63-char prefix + 1-char separator + 68-char Guid "X" format = 132.
	private const int MaxFormattedLength = 132;

	/// <summary>
	/// Formats <paramref name="value"/> into a newly-allocated string using the supplied
	/// prefix metadata and format specifier. Empty or <c>C</c> produces the canonical
	/// form (prefix + separator + "D" format); <c>D</c>, <c>N</c>, <c>B</c>, <c>P</c>,
	/// and <c>X</c> produce bare Guid output matching <see cref="Guid.ToString(string)"/>.
	/// </summary>
	public static string Format (Guid value, PrefixInfo prefix, ReadOnlySpan<char> format)
	{
		Span<char> buffer = stackalloc char[MaxFormattedLength];
		if (!TryFormat(value, prefix, buffer, out var charsWritten, format))
		{
			throw new InvalidOperationException("Formatted StrictId Guid exceeded the maximum buffer length.");
		}
		return new string(buffer[..charsWritten]);
	}

	/// <summary>
	/// Writes <paramref name="value"/> into <paramref name="destination"/> using the
	/// supplied prefix metadata and format specifier. Returns <see langword="false"/>
	/// without any partial writes if the destination is too small.
	/// </summary>
	/// <exception cref="FormatException">
	/// The format specifier is not one of <c>C</c>, <c>D</c>, <c>N</c>, <c>B</c>, <c>P</c>, <c>X</c>.
	/// </exception>
	public static bool TryFormat (
		Guid value,
		PrefixInfo prefix,
		Span<char> destination,
		out int charsWritten,
		ReadOnlySpan<char> format
	)
	{
		if (format.IsEmpty || format.SequenceEqual("C"))
			return TryWriteCanonical(value, prefix, destination, out charsWritten);

		// Standard Guid format specifiers — always bare (no prefix), matching System.Guid behaviour.
		if (format.SequenceEqual("D") || format.SequenceEqual("N") ||
		    format.SequenceEqual("B") || format.SequenceEqual("P") ||
		    format.SequenceEqual("X"))
			return TryWriteBareGuid(value, destination, out charsWritten, format);

		throw new FormatException(
			$"Unknown format specifier '{format.ToString()}' for a StrictId Guid. Valid specifiers: " +
			"'C' (canonical, default), 'D' (bare dashes), 'N' (bare no dashes), 'B' (bare braces), 'P' (bare parens), 'X' (bare hex).");
	}

	/// <summary>
	/// UTF-8 formatting entry point. All characters in a Guid canonical form are ASCII,
	/// so we format into a char buffer and widen 1:1 into the UTF-8 span.
	/// </summary>
	public static bool TryFormat (
		Guid value,
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
		Guid value,
		PrefixInfo prefix,
		Span<char> destination,
		out int charsWritten
	)
	{
		if (!prefix.HasPrefix)
			return TryWriteBareGuid(value, destination, out charsWritten, "D".AsSpan());

		var prefixText = prefix.Canonical!;
		var totalLen = prefixText.Length + 1 + 36; // prefix + separator + D-format (36 chars)

		if (destination.Length < totalLen)
		{
			charsWritten = 0;
			return false;
		}

		prefixText.AsSpan().CopyTo(destination);
		destination[prefixText.Length] = prefix.Separator.ToChar();
		value.TryFormat(destination.Slice(prefixText.Length + 1, 36), out _, "D".AsSpan());

		charsWritten = totalLen;
		return true;
	}

	private static bool TryWriteBareGuid (
		Guid value,
		Span<char> destination,
		out int charsWritten,
		ReadOnlySpan<char> guidFormat
	)
	{
		if (value.TryFormat(destination, out charsWritten, guidFormat))
			return true;

		charsWritten = 0;
		return false;
	}
}
