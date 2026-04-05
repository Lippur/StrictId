namespace StrictId.Internal;

/// <summary>
/// The resolved prefix metadata for a StrictId entity type: the canonical prefix used on
/// output, the full set of prefixes accepted on parse (canonical first, then declaration
/// order), and the separator character used between prefix and suffix.
/// </summary>
internal readonly struct PrefixInfo
{
	/// <summary>
	/// The canonical prefix for this type, or <see langword="null"/> if no
	/// <see cref="IdPrefixAttribute"/> is declared. When <see langword="null"/>, IDs of
	/// this type are serialized bare (no prefix, no separator).
	/// </summary>
	public string? Canonical { get; init; }

	/// <summary>
	/// All registered prefixes for this type. Canonical first, then any remaining
	/// declared aliases in their original declaration order. Empty when <see cref="Canonical"/>
	/// is <see langword="null"/>.
	/// </summary>
	public string[] Aliases { get; init; }

	/// <summary>The separator declared for this type, or <see cref="IdSeparator.Underscore"/> by default.</summary>
	public IdSeparator Separator { get; init; }

	/// <summary>
	/// <see langword="true"/> if this type has at least one <see cref="IdPrefixAttribute"/>
	/// declared (on itself or inherited from a base type).
	/// </summary>
	public bool HasPrefix => Canonical is not null;

	/// <summary>
	/// The fallback <see cref="PrefixInfo"/> for a type with no prefix and no separator override:
	/// no prefixes, <see cref="IdSeparator.Underscore"/>.
	/// </summary>
	public static PrefixInfo None { get; } = new()
	{
		Canonical = null,
		Aliases = [],
		Separator = IdSeparator.Underscore,
	};

	/// <summary>
	/// Returns <see langword="true"/> if <paramref name="candidate"/> case-insensitively
	/// matches any of this type's registered prefixes.
	/// </summary>
	public bool IsKnownPrefix (ReadOnlySpan<char> candidate)
	{
		foreach (var alias in Aliases)
		{
			if (candidate.Equals(alias.AsSpan(), StringComparison.OrdinalIgnoreCase))
				return true;
		}
		return false;
	}
}
