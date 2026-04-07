using System.Globalization;
using System.Text;

namespace StrictId.Internal;

/// <summary>
/// Parser for integer-backed StrictIds. Implements the "longest trailing run of
/// decimal digits" rule from the v3 design: the suffix is the tail of the input
/// consisting entirely of ASCII digits, and whatever precedes it (if anything) must
/// be a valid <c>{prefix}{separator}</c> pair registered for the target type.
/// </summary>
internal static class IdNumberParser
{
	/// <summary>
	/// Attempts to parse <paramref name="input"/> into a <see cref="ulong"/>,
	/// honouring <paramref name="prefix"/>'s registered prefix list. Returns
	/// <see langword="false"/> on any failure; use <see cref="BuildParseException"/>
	/// to obtain a verbose diagnostic message.
	/// </summary>
	/// <param name="input">The character span to parse.</param>
	/// <param name="prefix">The resolved prefix metadata for the target type.</param>
	/// <param name="requirePrefix">
	/// When <see langword="true"/>, bare (unprefixed) values are rejected even if
	/// structurally valid. Passed via <see cref="IdFormat.RequirePrefix"/>.
	/// </param>
	/// <param name="value">The parsed value, or <c>0</c> on failure.</param>
	public static bool TryParseUInt64 (ReadOnlySpan<char> input, PrefixInfo prefix, out ulong value, bool requirePrefix = false)
	{
		value = 0;
		if (input.IsEmpty) return false;

		var digitStart = FindDigitBoundary(input);

		// No trailing digits at all → fail.
		if (digitStart == input.Length) return false;

		var digits = input[digitStart..];

		// Case 1: entire input is decimal digits (bare numeric form).
		if (digitStart == 0)
			return !(requirePrefix && prefix.HasPrefix) && ulong.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out value);

		// Case 2: prefixed form. The char immediately before the digits must be a
		// recognised IdSeparator, and the text before that must be a registered prefix.
		var sepChar = input[digitStart - 1];
		if (!IdSeparators.TryFromChar(sepChar, out _)) return false;

		var prefixText = input[..(digitStart - 1)];
		if (prefixText.IsEmpty) return false;
		if (!prefix.IsKnownPrefix(prefixText)) return false;

		return ulong.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out value);
	}

	/// <summary>
	/// Builds a verbose <see cref="FormatException"/> for a failed numeric parse. The
	/// message includes the offending input, the expected shape, the registered
	/// prefix list, the declared separator, and a best-effort diagnosis of the
	/// specific failure.
	/// </summary>
	public static FormatException BuildParseException (string input, PrefixInfo prefix, string typeName, bool requirePrefix = false)
	{
		var reason = DiagnoseFailure(input.AsSpan(), prefix, requirePrefix);
		var message = BuildMessage(input, prefix, typeName, requirePrefix, reason);
		return new FormatException(message);
	}

	private static string BuildMessage (string input, PrefixInfo prefix, string typeName, bool requirePrefix, string reason)
	{
		var sb = new StringBuilder(256);
		sb.Append("Could not parse '").Append(input).Append("' as ").Append(typeName).Append('.');

		sb.Append("\n  Expected shape: ");
		if (requirePrefix && prefix.HasPrefix)
		{
			sb.Append("[prefix][separator]<decimal digits>. Bare values are rejected (IdFormat.RequirePrefix).");
		}
		else
		{
			sb.Append(prefix.HasPrefix
				? "[prefix][separator]<decimal digits>, or bare decimal digits."
				: "decimal digits (non-negative, up to ulong.MaxValue = 18446744073709551615).");
		}

		if (prefix.HasPrefix)
		{
			sb.Append("\n  Registered prefixes for ").Append(typeName).Append(": '")
				.Append(prefix.Canonical).Append("' (canonical)");
			for (var i = 1; i < prefix.Aliases.Length; i++)
				sb.Append(", '").Append(prefix.Aliases[i]).Append('\'');
			sb.Append('.');

			sb.Append("\n  Separator for ").Append(typeName).Append(": ")
				.Append(prefix.Separator).Append(" ('").Append(prefix.Separator.ToChar())
				.Append("'); other IdSeparator values also accepted on parse.");
		}

		sb.Append("\n  Failure: ").Append(reason);
		return sb.ToString();
	}

	private static string DiagnoseFailure (ReadOnlySpan<char> input, PrefixInfo prefix, bool requirePrefix = false)
	{
		if (input.IsEmpty) return "input is empty.";

		var digitStart = FindDigitBoundary(input);

		if (digitStart == input.Length)
			return "input does not end in a decimal digit.";

		var digits = input[digitStart..];

		if (digitStart == 0)
		{
			if (requirePrefix && prefix.HasPrefix)
				return "input is bare decimal digits but a prefix is required.";
			// Entire input is digits but TryParse still failed — must be overflow.
			return $"digit sequence '{digits.ToString()}' is out of range for ulong (maximum 18446744073709551615).";
		}

		var sepChar = input[digitStart - 1];
		if (!IdSeparators.TryFromChar(sepChar, out _))
			return $"expected a separator character (one of _ / . :) before the digits, but found '{sepChar}'.";

		var prefixText = input[..(digitStart - 1)];
		if (prefixText.IsEmpty)
			return "found a separator with no prefix before it.";

		if (!prefix.HasPrefix)
			return $"this type has no registered prefix, but the input contains the prefix '{prefixText.ToString()}'.";

		if (!prefix.IsKnownPrefix(prefixText))
			return $"prefix '{prefixText.ToString()}' is not registered for this type.";

		// Prefix OK, separator OK — must be an overflow on the digit portion.
		return $"digit sequence '{digits.ToString()}' is out of range for ulong (maximum 18446744073709551615).";
	}

	private static int FindDigitBoundary (ReadOnlySpan<char> input)
	{
		var i = input.Length;
		while (i > 0 && input[i - 1] is >= '0' and <= '9')
			i--;
		return i;
	}
}
