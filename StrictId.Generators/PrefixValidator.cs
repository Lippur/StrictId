namespace StrictId.Generators;

/// <summary>
/// Shared grammar + cardinality rules for <c>[IdPrefix]</c>. Used by both
/// <see cref="StrictIdGenerator"/> (to filter invalid declarations before emission)
/// and <c>StrictIdAttributeAnalyzer</c> (to surface the same failures as user-facing
/// STRID003 diagnostics). Kept free of Roslyn type dependencies so it can be reused
/// from any pipeline that has already extracted the primitive attribute arguments.
/// </summary>
internal static class PrefixValidator
{
	/// <summary>Maximum permitted prefix length, in characters.</summary>
	public const int MaxPrefixLength = 63;

	/// <summary>
	/// Validates the grammar of a single prefix literal. Returns <see langword="null"/>
	/// on success; otherwise a human-readable reason string describing the first rule
	/// the prefix violates. The format tracks <c>^[a-z][a-z0-9_]{0,62}$</c>.
	/// </summary>
	/// <param name="prefix">The prefix literal to validate.</param>
	public static string? ValidateGrammar (string prefix)
	{
		if (prefix.Length == 0) return "prefix is empty";
		if (prefix.Length > MaxPrefixLength)
			return $"prefix is {prefix.Length} characters long (max {MaxPrefixLength})";

		var first = prefix[0];
		if (first is < 'a' or > 'z')
			return $"first character '{first}' is not a lowercase ASCII letter";

		for (var i = 1; i < prefix.Length; i++)
		{
			var c = prefix[i];
			if (c is >= 'a' and <= 'z' or >= '0' and <= '9' or '_') continue;
			return $"contains '{c}' at position {i}";
		}

		return null;
	}
}
