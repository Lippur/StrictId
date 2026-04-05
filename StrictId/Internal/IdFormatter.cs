namespace StrictId.Internal;

/// <summary>
/// Canonical formatter for ULID-backed StrictIds. Writes prefix-aware canonical or
/// bare forms (in either ULID or GUID encoding, lowercase or uppercase) to a
/// destination span, with a matching <c>string</c>-returning overload built on top of
/// a stack buffer.
/// </summary>
internal static class IdFormatter
{
	// Maximum possible formatted length: 63-char prefix + 1-char separator + 36-char GUID = 100.
	private const int MaxFormattedLength = 100;

	/// <summary>
	/// Formats <paramref name="value"/> into a newly-allocated string using the supplied
	/// prefix metadata and format specifier. <paramref name="format"/> may be any of
	/// <c>C</c>, <c>B</c>, <c>G</c>, <c>BG</c>, <c>U</c>, or empty (which defaults to <c>C</c>).
	/// </summary>
	public static string Format (Ulid value, PrefixInfo prefix, ReadOnlySpan<char> format)
	{
		Span<char> buffer = stackalloc char[MaxFormattedLength];
		if (!TryFormat(value, prefix, buffer, out var charsWritten, format))
		{
			// The only way this fails with a stack buffer this size is an unknown format
			// specifier, which is thrown from TryFormat. If we somehow get here, treat
			// it as a library bug.
			throw new InvalidOperationException("Formatted StrictId exceeded the maximum buffer length of 100 characters.");
		}
		return new string(buffer[..charsWritten]);
	}

	/// <summary>
	/// Writes <paramref name="value"/> into <paramref name="destination"/> using the
	/// supplied prefix metadata and format specifier. Returns <see langword="false"/>
	/// without any partial writes if the destination is too small.
	/// </summary>
	/// <exception cref="FormatException">
	/// The format specifier is not one of <c>C</c>, <c>B</c>, <c>G</c>, <c>BG</c>, <c>U</c>.
	/// </exception>
	public static bool TryFormat (
		Ulid value,
		PrefixInfo prefix,
		Span<char> destination,
		out int charsWritten,
		ReadOnlySpan<char> format
	)
	{
		if (format.IsEmpty || format.SequenceEqual("C"))
			return TryWriteCanonical(value, prefix, destination, out charsWritten, asGuid: false, lowercase: true);
		if (format.SequenceEqual("B"))
			return TryWriteBare(value, destination, out charsWritten, asGuid: false, lowercase: true);
		if (format.SequenceEqual("G"))
			return TryWriteCanonical(value, prefix, destination, out charsWritten, asGuid: true, lowercase: true);
		if (format.SequenceEqual("BG"))
			return TryWriteBare(value, destination, out charsWritten, asGuid: true, lowercase: true);
		if (format.SequenceEqual("U"))
			return TryWriteBare(value, destination, out charsWritten, asGuid: false, lowercase: false);

		throw new FormatException(
			$"Unknown format specifier '{format.ToString()}' for a StrictId. Valid specifiers: " +
			"'C' (canonical, default), 'B' (bare ULID), 'G' (canonical GUID), 'BG' (bare GUID), 'U' (uppercase ULID, v2 compat).");
	}

	/// <summary>
	/// UTF-8 formatting entry point. Every character produced by a StrictId canonical
	/// form is ASCII (Crockford base32, lowercase hex, underscore/slash/period/colon),
	/// so we can format into a <c>char</c> buffer and then widen 1:1 into the UTF-8 span.
	/// </summary>
	public static bool TryFormat (
		Ulid value,
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
		Ulid value,
		PrefixInfo prefix,
		Span<char> destination,
		out int charsWritten,
		bool asGuid,
		bool lowercase
	)
	{
		if (!prefix.HasPrefix)
			return TryWriteBare(value, destination, out charsWritten, asGuid, lowercase);

		var suffixLen = asGuid ? 36 : 26;
		var prefixText = prefix.Canonical!;
		var totalLen = prefixText.Length + 1 + suffixLen;

		if (destination.Length < totalLen)
		{
			charsWritten = 0;
			return false;
		}

		prefixText.AsSpan().CopyTo(destination);
		destination[prefixText.Length] = prefix.Separator.ToChar();
		WriteSuffix(value, destination.Slice(prefixText.Length + 1, suffixLen), asGuid, lowercase);

		charsWritten = totalLen;
		return true;
	}

	private static bool TryWriteBare (
		Ulid value,
		Span<char> destination,
		out int charsWritten,
		bool asGuid,
		bool lowercase
	)
	{
		var len = asGuid ? 36 : 26;
		if (destination.Length < len)
		{
			charsWritten = 0;
			return false;
		}

		WriteSuffix(value, destination[..len], asGuid, lowercase);
		charsWritten = len;
		return true;
	}

	/// <summary>
	/// Writes the ULID or GUID suffix into the destination span. The <paramref name="lowercase"/>
	/// flag only affects the ULID branch; the GUID branch always emits lowercase because
	/// <see cref="Guid"/>'s <c>TryFormat</c> with the <c>"D"</c> specifier is already
	/// lowercase, and there is no caller that asks for an uppercase GUID form. If a future
	/// caller needs uppercase GUIDs, add an explicit format specifier rather than re-wiring
	/// this helper.
	/// </summary>
	private static void WriteSuffix (Ulid value, Span<char> destination, bool asGuid, bool lowercase)
	{
		if (asGuid)
		{
			value.ToGuid().TryFormat(destination, out _, "D".AsSpan());
			return;
		}

		// Cysharp's Ulid.TryFormat emits uppercase Crockford base32.
		value.TryFormat(destination, out _, default, null);
		if (lowercase) ToLowerAscii(destination);
	}

	private static void ToLowerAscii (Span<char> span)
	{
		for (var i = 0; i < span.Length; i++)
		{
			var c = span[i];
			if (c is >= 'A' and <= 'Z')
				span[i] = (char)(c + 32);
		}
	}
}
