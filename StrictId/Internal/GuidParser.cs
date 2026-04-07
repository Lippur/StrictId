using System.Text;

namespace StrictId.Internal;

/// <summary>
/// Parser for Guid-backed StrictIds. Accepts bare Guids in any standard
/// <see cref="Guid.TryParse(ReadOnlySpan{char}, out Guid)"/>-compatible format (D, N,
/// B, P, X) as well as prefixed forms where the suffix is a 36-character "D" format
/// Guid. Prefix validation honours the type's registered prefix list, and any of the
/// four <see cref="IdSeparator"/> values is accepted in the separator position.
/// </summary>
internal static class GuidParser
{
	/// <summary>
	/// Attempts to parse <paramref name="input"/> into a <see cref="Guid"/>, honouring
	/// <paramref name="prefix"/>'s registered prefix list. Returns <see langword="false"/>
	/// on any failure; use <see cref="BuildParseException"/> to obtain a verbose diagnostic.
	/// </summary>
	/// <param name="input">The character span to parse.</param>
	/// <param name="prefix">The resolved prefix metadata for the target type.</param>
	/// <param name="requirePrefix">
	/// When <see langword="true"/>, bare (unprefixed) values are rejected even if
	/// structurally valid. Passed via <see cref="IdFormat.RequirePrefix"/>.
	/// </param>
	/// <param name="value">The parsed GUID, or <see langword="default"/> on failure.</param>
	public static bool TryParseGuid (ReadOnlySpan<char> input, PrefixInfo prefix, out Guid value, bool requirePrefix = false)
	{
		value = default;
		if (input.IsEmpty) return false;
		var enforcePrefix = requirePrefix && prefix.HasPrefix;

		// Case 1: bare Guid — try all standard formats via Guid.TryParse.
		// Standard lengths: N=32, D=36, B/P=38, X=68. If the input matches any of
		// these and parses as a Guid, accept it without prefix validation.
		if (!enforcePrefix && input.Length is 32 or 36 or 38 or 68)
		{
			if (Guid.TryParse(input, out value))
				return true;
		}

		// Case 2: prefixed form. The suffix is always the 36-char "D" format because
		// it is the canonical Guid string representation and the only one that is
		// unambiguous after a separator character. Minimum prefixed length is
		// 1 (prefix) + 1 (separator) + 36 (D-format) = 38. We only enter this path
		// when the input is longer than 36 to avoid re-parsing a bare D-format Guid
		// that already failed above.
		if (input.Length > 36 && TryParsePrefixed(input, prefix, out value))
			return true;

		// Case 3: length doesn't match any standard format and isn't prefixed.
		// Try Guid.TryParse as a catch-all for any format we might not have length-matched.
		if (!enforcePrefix && Guid.TryParse(input, out value))
			return true;

		return false;
	}

	private static bool TryParsePrefixed (
		ReadOnlySpan<char> input,
		PrefixInfo prefix,
		out Guid value
	)
	{
		value = default;

		const int suffixLen = 36; // "D" format Guid length
		var suffixStart = input.Length - suffixLen;
		var separatorIdx = suffixStart - 1;

		if (separatorIdx < 1) return false; // need at least 1 char for the prefix

		// The separator must be one of the four recognised IdSeparator values.
		if (!IdSeparators.TryFromChar(input[separatorIdx], out _)) return false;

		// The suffix must parse as a Guid.
		var suffix = input[suffixStart..];
		if (!Guid.TryParse(suffix, out var parsed)) return false;

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
		string typeName,
		bool requirePrefix = false
	)
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
			sb.Append("[prefix][separator]<36-char GUID>. Bare values are rejected (IdFormat.RequirePrefix).");
		}
		else
		{
			sb.Append(prefix.HasPrefix
				? "[prefix][separator]<36-char GUID>, or a bare GUID (D/N/B/P/X format)."
				: "A GUID in any standard format (D, N, B, P, or X).");
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

		if (input.Length is 32 or 36 or 38 or 68)
		{
			if (requirePrefix && prefix.HasPrefix && Guid.TryParse(input, out _))
				return "input is a valid bare GUID but a prefix is required.";
			return $"input is {input.Length} characters but is not a valid GUID.";
		}

		if (input.Length > 36)
		{
			var tail36 = input[^36..];
			if (Guid.TryParse(tail36, out _))
				return DiagnosePrefixPortion(input[..^36], prefix);
		}

		return $"input is {input.Length} characters long but does not match a standard GUID format (D=36, N=32, B/P=38, X=68) or a prefixed form.";
	}

	private static string DiagnosePrefixPortion (ReadOnlySpan<char> prefixPortion, PrefixInfo prefix)
	{
		if (prefixPortion.IsEmpty)
			return "found a valid GUID suffix but no prefix before it.";

		var sepChar = prefixPortion[^1];
		if (!IdSeparators.TryFromChar(sepChar, out _))
			return $"expected a separator character (one of _ / . :) before the GUID suffix, but found '{sepChar}'.";

		var prefixText = prefixPortion[..^1];
		if (prefixText.IsEmpty)
			return "found a separator with no prefix before it.";

		if (!prefix.HasPrefix)
			return $"this type has no registered prefix, but the input contains the prefix '{prefixText.ToString()}'.";

		return $"prefix '{prefixText.ToString()}' is not registered for this type.";
	}
}
