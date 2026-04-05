namespace StrictId.Internal;

/// <summary>
/// Validates the suffix of an <c>IdString&lt;T&gt;</c> against its declared
/// <see cref="IdStringOptions"/> — length, character set, and whitespace rules. Used
/// by both the string constructor (to enforce constraints at construction time) and
/// the parser (to enforce them on extracted suffixes).
/// </summary>
internal static class IdStringValidator
{
	/// <summary>
	/// Returns <see langword="true"/> if <paramref name="value"/> is a valid suffix
	/// under <paramref name="options"/>. A valid suffix is non-empty, within the
	/// maximum length, contains no whitespace, and uses only characters permitted by
	/// the configured <see cref="IdStringCharSet"/>.
	/// </summary>
	public static bool IsValid (ReadOnlySpan<char> value, IdStringOptions options)
		=> GetInvalidReason(value, options) is null;

	/// <summary>
	/// Inspects <paramref name="value"/> against <paramref name="options"/>. Returns
	/// <see langword="null"/> if the value is valid, otherwise a human-readable
	/// description of the first rule it violates.
	/// </summary>
	public static string? GetInvalidReason (ReadOnlySpan<char> value, IdStringOptions options)
	{
		if (value.IsEmpty) return "value is empty.";
		if (value.Length > options.MaxLength)
			return $"length {value.Length} exceeds the maximum of {options.MaxLength} characters.";

		for (var i = 0; i < value.Length; i++)
		{
			var c = value[i];
			if (char.IsWhiteSpace(c))
				return $"contains whitespace at position {i}.";
			if (!IsCharAllowed(c, options.CharSet))
				return $"contains '{c}' at position {i}, which is not allowed by charset {options.CharSet}.";
		}

		return null;
	}

	/// <summary>
	/// Applies case normalization according to <paramref name="options"/>: returns a
	/// lowercase copy of <paramref name="value"/> when <see cref="IdStringOptions.IgnoreCase"/>
	/// is <see langword="true"/>, otherwise returns the original string unchanged.
	/// </summary>
	public static string Normalize (string value, IdStringOptions options)
		=> options.IgnoreCase ? value.ToLowerInvariant() : value;

	private static bool IsCharAllowed (char c, IdStringCharSet charSet) => charSet switch
	{
		IdStringCharSet.Any => IsPrintableNonSeparator(c),
		IdStringCharSet.Alphanumeric => IsAsciiAlphanumeric(c),
		IdStringCharSet.AlphanumericDash => IsAsciiAlphanumeric(c) || c == '-',
		IdStringCharSet.AlphanumericUnderscore => IsAsciiAlphanumeric(c) || c == '_',
		_ => false,
	};

	private static bool IsPrintableNonSeparator (char c)
	{
		if (char.IsControl(c) || char.IsWhiteSpace(c)) return false;
		// Exclude the four IdSeparator characters so the suffix can never collide
		// with its own separator during round-tripping.
		return c is not ('_' or '/' or '.' or ':');
	}

	private static bool IsAsciiAlphanumeric (char c)
		=> c is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or (>= '0' and <= '9');
}
