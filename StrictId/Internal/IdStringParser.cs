using System.Text;

namespace StrictId.Internal;

/// <summary>
/// Parser for string-backed StrictIds. Matches the start of the input against the
/// type's registered prefixes (longest-match-wins, case-insensitive); if a prefix +
/// separator combination is found, the extracted suffix is validated against the
/// type's <see cref="IdStringOptions"/>. If no prefix matches, the entire input is
/// treated as a bare suffix and validated in the same way.
/// </summary>
internal static class IdStringParser
{
	/// <summary>
	/// Attempts to parse <paramref name="input"/> into a validated suffix string,
	/// honouring <paramref name="prefix"/>'s registered prefix list and applying
	/// <paramref name="options"/>'s case normalization. Returns <see langword="false"/>
	/// on any failure; use <see cref="BuildParseException"/> to obtain a verbose
	/// diagnostic message.
	/// </summary>
	public static bool TryParseString (
		ReadOnlySpan<char> input,
		PrefixInfo prefix,
		IdStringOptions options,
		out string? value
	)
	{
		value = null;
		if (input.IsEmpty) return false;

		// Look for the longest registered prefix that matches the start of the input
		// and is followed by a valid separator character.
		var bestPrefixLen = -1;
		foreach (var alias in prefix.Aliases)
		{
			if (input.Length <= alias.Length + 1) continue;
			if (!input[..alias.Length].Equals(alias.AsSpan(), StringComparison.OrdinalIgnoreCase)) continue;
			if (!IdSeparators.TryFromChar(input[alias.Length], out _)) continue;
			if (alias.Length > bestPrefixLen) bestPrefixLen = alias.Length;
		}

		ReadOnlySpan<char> suffix;
		if (bestPrefixLen >= 0)
			suffix = input[(bestPrefixLen + 1)..];
		else
			suffix = input;

		if (!IdStringValidator.IsValid(suffix, options)) return false;

		value = IdStringValidator.Normalize(suffix.ToString(), options);
		return true;
	}

	/// <summary>
	/// Builds a verbose <see cref="FormatException"/> for a failed string parse. The
	/// message includes the offending input, the expected shape, the registered
	/// prefix list, the separator, and a best-effort diagnosis of the specific
	/// failure (empty input, prefix mismatch, charset violation, length overflow,
	/// etc.).
	/// </summary>
	public static FormatException BuildParseException (
		string input,
		PrefixInfo prefix,
		IdStringOptions options,
		string typeName
	)
	{
		var reason = DiagnoseFailure(input.AsSpan(), prefix, options);
		var message = BuildMessage(input, prefix, options, typeName, reason);
		return new FormatException(message);
	}

	private static string BuildMessage (
		string input,
		PrefixInfo prefix,
		IdStringOptions options,
		string typeName,
		string reason
	)
	{
		var sb = new StringBuilder(256);
		sb.Append("Could not parse '").Append(input).Append("' as ").Append(typeName).Append('.');

		sb.Append("\n  Expected shape: ");
		sb.Append(prefix.HasPrefix
			? "[prefix][separator]<suffix>, or a bare suffix."
			: "an opaque string suffix.");

		sb.Append("\n  Suffix rules: max length ").Append(options.MaxLength)
			.Append(", charset ").Append(options.CharSet)
			.Append(", case-").Append(options.IgnoreCase ? "insensitive" : "sensitive")
			.Append('.');

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

	private static string DiagnoseFailure (
		ReadOnlySpan<char> input,
		PrefixInfo prefix,
		IdStringOptions options
	)
	{
		if (input.IsEmpty) return "input is empty.";

		// Determine whether the input appears to have a prefix, and if so, whether
		// the prefix matches one registered for this type.
		var bestPrefixLen = -1;
		foreach (var alias in prefix.Aliases)
		{
			if (input.Length <= alias.Length + 1) continue;
			if (!input[..alias.Length].Equals(alias.AsSpan(), StringComparison.OrdinalIgnoreCase)) continue;
			if (!IdSeparators.TryFromChar(input[alias.Length], out _)) continue;
			if (alias.Length > bestPrefixLen) bestPrefixLen = alias.Length;
		}

		ReadOnlySpan<char> suffix;
		if (bestPrefixLen >= 0)
			suffix = input[(bestPrefixLen + 1)..];
		else
			suffix = input;

		var reason = IdStringValidator.GetInvalidReason(suffix, options);
		if (reason is not null)
		{
			return bestPrefixLen >= 0
				? $"suffix '{suffix.ToString()}' after the matched prefix is invalid: {reason}"
				: $"bare suffix '{suffix.ToString()}' is invalid: {reason}";
		}

		// This branch should be unreachable — if validation passed here, parsing
		// should have succeeded. Safe fallback for defense-in-depth.
		return "input does not match the expected shape.";
	}
}
