namespace StrictId.Internal;

/// <summary>
/// Canonical formatter for string-backed StrictIds. Writes prefix-aware canonical or
/// bare forms into a destination span. Since the suffix length is arbitrary (bounded
/// only by the type's <see cref="IdStringOptions.MaxLength"/>), the <c>string</c>-
/// returning overload uses direct concatenation rather than a stack buffer.
/// </summary>
internal static class IdStringFormatter
{
	/// <summary>
	/// Formats <paramref name="value"/> into a newly-allocated string using the supplied
	/// prefix metadata and format specifier. <paramref name="format"/> accepts
	/// <c>C</c> (canonical, default) or <c>B</c> (bare suffix). Returns an empty string
	/// when <paramref name="value"/> is <see langword="null"/>.
	/// </summary>
	public static string Format (string? value, PrefixInfo prefix, ReadOnlySpan<char> format)
	{
		if (value is null) return string.Empty;

		if (format.IsEmpty || format.SequenceEqual("C"))
		{
			if (!prefix.HasPrefix) return value;
			return string.Concat(prefix.Canonical, prefix.Separator.ToChar().ToString(), value);
		}
		if (format.SequenceEqual("B"))
			return value;

		throw new FormatException(
			$"Unknown format specifier '{format.ToString()}' for an IdString. Valid specifiers: " +
			"'C' (canonical, default) and 'B' (bare suffix).");
	}

	/// <summary>
	/// Writes <paramref name="value"/> into <paramref name="destination"/> using the
	/// supplied prefix metadata and format specifier. Returns <see langword="false"/>
	/// without partial writes if the destination is too small. A <see langword="null"/>
	/// <paramref name="value"/> writes zero characters and returns <see langword="true"/>.
	/// </summary>
	/// <exception cref="FormatException">The format specifier is not recognised.</exception>
	public static bool TryFormat (
		string? value,
		PrefixInfo prefix,
		Span<char> destination,
		out int charsWritten,
		ReadOnlySpan<char> format
	)
	{
		if (value is null)
		{
			charsWritten = 0;
			return true;
		}

		bool canonical;
		if (format.IsEmpty || format.SequenceEqual("C")) canonical = true;
		else if (format.SequenceEqual("B")) canonical = false;
		else throw new FormatException(
			$"Unknown format specifier '{format.ToString()}' for an IdString. Valid specifiers: " +
			"'C' (canonical, default) and 'B' (bare suffix).");

		if (canonical && prefix.HasPrefix)
		{
			var prefixText = prefix.Canonical!;
			var totalLen = prefixText.Length + 1 + value.Length;
			if (destination.Length < totalLen)
			{
				charsWritten = 0;
				return false;
			}

			prefixText.AsSpan().CopyTo(destination);
			destination[prefixText.Length] = prefix.Separator.ToChar();
			value.AsSpan().CopyTo(destination[(prefixText.Length + 1)..]);
			charsWritten = totalLen;
			return true;
		}

		if (destination.Length < value.Length)
		{
			charsWritten = 0;
			return false;
		}

		value.AsSpan().CopyTo(destination);
		charsWritten = value.Length;
		return true;
	}

	/// <summary>
	/// UTF-8 entry point. For the common ASCII-only case, widens the char buffer 1:1
	/// into the UTF-8 span. For non-ASCII suffix content (e.g., Unicode slugs), falls
	/// back to <see cref="System.Text.Encoding.UTF8"/>.
	/// </summary>
	public static bool TryFormat (
		string? value,
		PrefixInfo prefix,
		Span<byte> utf8Destination,
		out int bytesWritten,
		ReadOnlySpan<char> format
	)
	{
		if (value is null)
		{
			bytesWritten = 0;
			return true;
		}

		// Conservative upper bound: prefix + separator + suffix chars; UTF-8 can be
		// up to 4x this for non-ASCII, but the overwhelmingly common case is ASCII.
		var charLen = (prefix.HasPrefix ? prefix.Canonical!.Length + 1 : 0) + value.Length;
		Span<char> temp = charLen <= 512 ? stackalloc char[charLen] : new char[charLen];

		if (!TryFormat(value, prefix, temp, out var charsWritten, format))
		{
			bytesWritten = 0;
			return false;
		}

		// Fast path: if every char is ASCII, widen 1:1.
		if (IsAllAscii(temp[..charsWritten]))
		{
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

		// Slow path: Unicode content, use UTF-8 encoder.
		return System.Text.Encoding.UTF8.TryGetBytes(temp[..charsWritten], utf8Destination, out bytesWritten);
	}

	private static bool IsAllAscii (ReadOnlySpan<char> span)
	{
		foreach (var c in span)
		{
			if (c > 0x7F) return false;
		}
		return true;
	}
}
