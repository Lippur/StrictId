namespace StrictId;

/// <summary>
/// Restricts the character set permitted inside the suffix of an <c>IdString&lt;T&gt;</c>.
/// Whitespace is always rejected regardless of which value is chosen.
/// </summary>
public enum IdStringCharSet
{
	/// <summary>
	/// Any printable, non-whitespace, non-separator character is allowed.
	/// </summary>
	Any,

	/// <summary>ASCII letters and digits only: <c>[A-Za-z0-9]</c>.</summary>
	Alphanumeric,

	/// <summary>ASCII letters, digits, and dash: <c>[A-Za-z0-9-]</c>.</summary>
	AlphanumericDash,

	/// <summary>ASCII letters, digits, and underscore: <c>[A-Za-z0-9_]</c>.</summary>
	AlphanumericUnderscore,

	/// <summary>ASCII letters, digits, dash, and underscore: <c>[A-Za-z0-9_-]</c>. The default.</summary>
	AlphanumericDashUnderscore,
}
