using System.Text;

namespace StrictId.Internal;

/// <summary>
/// Parser for ULID-backed StrictIds. Implements the walk-back algorithm from the v3
/// design: bare ULID or GUID is always accepted; prefixed forms are recognised by the
/// length of the trailing suffix (26 for ULID, 36 for GUID) and validated against the
/// type's registered prefix list. Parsing is tolerant of any of the four
/// <see cref="IdSeparator"/> values in the separator position regardless of which one
/// is declared as canonical.
/// </summary>
internal static class IdParser
{
	/// <summary>
	/// Attempts to parse <paramref name="input"/> into a <see cref="Ulid"/>, honouring
	/// <paramref name="prefix"/>'s registered prefix list. Returns <see langword="false"/>
	/// on any failure; use <see cref="BuildParseException"/> to obtain a verbose
	/// diagnostic message.
	/// </summary>
	public static bool TryParseUlid (ReadOnlySpan<char> input, PrefixInfo prefix, out Ulid value)
	{
		value = default;
		if (input.IsEmpty) return false;

		// Case 1: bare ULID (exactly 26 chars).
		if (input.Length == 26)
			return Ulid.TryParse(input, out value);

		// Case 2: bare GUID (exactly 36 chars, hyphenated "D" form).
		if (input.Length == 36)
		{
			if (Guid.TryParse(input, out var guid))
			{
				value = new Ulid(guid);
				return true;
			}
			return false;
		}

		// Case 3: prefixed form. Minimum prefixed length is 1 (prefix) + 1 (separator)
		// + 26 (ULID) = 28. Try ULID suffix first (more common), then GUID suffix.
		if (input.Length >= 28 && TryParsePrefixed(input, suffixLen: 26, prefix, out value))
			return true;

		if (input.Length >= 38 && TryParsePrefixed(input, suffixLen: 36, prefix, out value))
			return true;

		return false;
	}

	private static bool TryParsePrefixed (
		ReadOnlySpan<char> input,
		int suffixLen,
		PrefixInfo prefix,
		out Ulid value
	)
	{
		value = default;

		var suffixStart = input.Length - suffixLen;
		var separatorIdx = suffixStart - 1;

		// The separator must be one of the four recognised IdSeparator values.
		if (!IdSeparators.TryFromChar(input[separatorIdx], out _)) return false;

		// The suffix must parse as a ULID or GUID.
		var suffix = input[suffixStart..];
		Ulid parsed;
		if (suffixLen == 26)
		{
			if (!Ulid.TryParse(suffix, out parsed)) return false;
		}
		else
		{
			if (!Guid.TryParse(suffix, out var guid)) return false;
			parsed = new Ulid(guid);
		}

		// The prefix text must be one of this type's registered prefixes (case-insensitive).
		var prefixText = input[..separatorIdx];
		if (!prefix.IsKnownPrefix(prefixText)) return false;

		value = parsed;
		return true;
	}

	/// <summary>
	/// Builds a verbose <see cref="FormatException"/> for a failed parse. The message
	/// includes the offending input, the expected shape, the registered prefix list,
	/// the declared separator, and a best-effort diagnosis of the specific failure.
	/// </summary>
	public static FormatException BuildParseException (
		string input,
		PrefixInfo prefix,
		string typeName
	)
	{
		var reason = DiagnoseFailure(input.AsSpan(), prefix);
		var message = BuildMessage(input, prefix, typeName, reason);
		return new FormatException(message);
	}

	private static string BuildMessage (string input, PrefixInfo prefix, string typeName, string reason)
	{
		var sb = new StringBuilder(256);
		sb.Append("Could not parse '").Append(input).Append("' as ").Append(typeName).Append('.');

		sb.Append("\n  Expected shape: ");
		sb.Append(prefix.HasPrefix
			? "[prefix][separator]<26-char ULID or 36-char GUID>, or a bare 26-char ULID / 36-char GUID."
			: "26-char ULID (Crockford base32) or 36-char GUID.");

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

	private static string DiagnoseFailure (ReadOnlySpan<char> input, PrefixInfo prefix)
	{
		if (input.IsEmpty) return "input is empty.";

		if (input.Length == 26)
		{
			for (var i = 0; i < input.Length; i++)
			{
				if (!IsCrockfordChar(input[i]))
					return $"input is 26 characters but contains '{input[i]}' at position {i}, which is not in the Crockford base32 alphabet.";
			}
			return "input is 26 characters but is not a valid ULID.";
		}

		if (input.Length == 36)
			return "input is 36 characters but is not a valid GUID.";

		if (input.Length > 26)
		{
			var tail26 = input[^26..];
			if (Ulid.TryParse(tail26, out _))
				return DiagnosePrefixPortion(input[..^26], prefix);
		}

		if (input.Length > 36)
		{
			var tail36 = input[^36..];
			if (Guid.TryParse(tail36, out _))
				return DiagnosePrefixPortion(input[..^36], prefix);
		}

		return $"input is {input.Length} characters long but does not match a bare ULID (26 chars), bare GUID (36 chars), or a prefixed form.";
	}

	private static string DiagnosePrefixPortion (ReadOnlySpan<char> prefixPortion, PrefixInfo prefix)
	{
		if (prefixPortion.IsEmpty)
			return "found a valid suffix but no prefix before it.";

		var sepChar = prefixPortion[^1];
		if (!IdSeparators.TryFromChar(sepChar, out _))
			return $"expected a separator character (one of _ / . :) before the suffix, but found '{sepChar}'.";

		var prefixText = prefixPortion[..^1];
		if (prefixText.IsEmpty)
			return "found a separator with no prefix before it.";

		if (!prefix.HasPrefix)
			return $"this type has no registered prefix, but the input contains the prefix '{prefixText.ToString()}'.";

		return $"prefix '{prefixText.ToString()}' is not registered for this type.";
	}

	private static bool IsCrockfordChar (char c)
	{
		// Crockford base32: 0-9, A-Z excluding I, L, O, U (case-insensitive).
		if (c is >= '0' and <= '9') return true;
		if (c is >= 'a' and <= 'z') return c is not ('i' or 'l' or 'o' or 'u');
		if (c is >= 'A' and <= 'Z') return c is not ('I' or 'L' or 'O' or 'U');
		return false;
	}
}
