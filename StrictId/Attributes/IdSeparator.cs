namespace StrictId;

/// <summary>
/// The set of characters permitted between the prefix and the suffix of a StrictId.
/// </summary>
public enum IdSeparator
{
	/// <summary>Underscore (<c>_</c>). Default. URL-safe.</summary>
	Underscore,

	/// <summary>Forward slash (<c>/</c>). Requires URL-encoding in path segments.</summary>
	Slash,

	/// <summary>Period (<c>.</c>).</summary>
	Period,

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
		IdSeparator.Period => '.',
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
			case '.': separator = IdSeparator.Period; return true;
			case ':': separator = IdSeparator.Colon; return true;
			default: separator = default; return false;
		}
	}
}
