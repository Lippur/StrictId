namespace StrictId.Internal;

/// <summary>
/// The resolved <see cref="IdStringAttribute"/> configuration for a StrictId entity
/// type: the maximum suffix length, the allowed character set, and whether comparisons
/// are case-insensitive.
/// </summary>
internal readonly struct IdStringOptions
{
	/// <summary>Maximum permitted suffix length, in characters.</summary>
	public int MaxLength { get; init; }

	/// <summary>The character set constraint applied to the suffix.</summary>
	public IdStringCharSet CharSet { get; init; }

	/// <summary>
	/// <see langword="true"/> if differently-cased suffixes should be treated as equal.
	/// </summary>
	public bool IgnoreCase { get; init; }

	/// <summary>
	/// The fallback configuration for a type with no <see cref="IdStringAttribute"/>:
	/// <see cref="MaxLength"/> = 255,
	/// <see cref="CharSet"/> = <see cref="IdStringCharSet.AlphanumericDashUnderscore"/>,
	/// <see cref="IgnoreCase"/> = <see langword="false"/>.
	/// </summary>
	public static IdStringOptions Default { get; } = new()
	{
		MaxLength = 255,
		CharSet = IdStringCharSet.AlphanumericDashUnderscore,
		IgnoreCase = false,
	};
}
