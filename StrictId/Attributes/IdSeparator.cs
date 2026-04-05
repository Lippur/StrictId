namespace StrictId;

/// <summary>
/// The set of characters permitted between the prefix and the suffix of a StrictId.
/// The set is deliberately closed: every value is unambiguous against the grammar of
/// all three ID families, visually distinct, keyboard-reachable, and collision-free
/// against every suffix form.
/// </summary>
public enum IdSeparator
{
	/// <summary>
	/// Underscore (<c>_</c>). Default. URL-safe, and the most
	/// greppable of the four options.
	/// </summary>
	Underscore,

	/// <summary>
	/// Forward slash (<c>/</c>). Path-like. Avoid for IDs that appear in URL path
	/// segments.
	/// </summary>
	Slash,

	/// <summary>
	/// Backslash (<c>\</c>). Niche. Useful when colon or slash would collide with
	/// another surrounding format.
	/// </summary>
	Backslash,

	/// <summary>
	/// Colon (<c>:</c>). Common in namespaced IDs such as <c>user:42</c>.
	/// </summary>
	Colon,
}

/// <summary>
/// Helpers for <see cref="IdSeparator"/> — conversion to and from the corresponding
/// single-character form.
/// </summary>
public static class IdSeparators
{
	/// <summary>Returns the single character that represents <paramref name="separator"/>.</summary>
	/// <param name="separator">The separator to convert.</param>
	/// <exception cref="ArgumentOutOfRangeException">
	/// <paramref name="separator"/> is not one of the four defined <see cref="IdSeparator"/> values.
	/// </exception>
	public static char ToChar (this IdSeparator separator) => separator switch
	{
		IdSeparator.Underscore => '_',
		IdSeparator.Slash => '/',
		IdSeparator.Backslash => '\\',
		IdSeparator.Colon => ':',
		_ => throw new ArgumentOutOfRangeException(nameof(separator), separator, "Not a valid IdSeparator value."),
	};

	/// <summary>
	/// Attempts to recognise <paramref name="c"/> as one of the four <see cref="IdSeparator"/>
	/// characters.
	/// </summary>
	/// <param name="c">The character to classify.</param>
	/// <param name="separator">
	/// On success, the matching <see cref="IdSeparator"/> value. On failure, <c>default</c>.
	/// </param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="c"/> is a recognised separator; otherwise
	/// <see langword="false"/>.
	/// </returns>
	public static bool TryFromChar (char c, out IdSeparator separator)
	{
		switch (c)
		{
			case '_': separator = IdSeparator.Underscore; return true;
			case '/': separator = IdSeparator.Slash; return true;
			case '\\': separator = IdSeparator.Backslash; return true;
			case ':': separator = IdSeparator.Colon; return true;
			default: separator = default; return false;
		}
	}
}
