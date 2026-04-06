namespace StrictId.Generators;

/// <summary>
/// A snapshot of an <c>[IdPrefix]</c>-decorated user type from the incremental-generator
/// scan. An empty <see cref="FullyQualifiedName"/> means the type was filtered out
/// (inaccessible or malformed).
/// </summary>
/// <param name="SeparatorEnumMember">
/// The separator enum member name from a type-level <c>[IdSeparator]</c>, or
/// <see langword="null"/> to use the assembly-level fallback.
/// </param>
internal sealed record PrefixDescriptor (
	string FullyQualifiedName,
	EquatableArray<PrefixDeclaration> Prefixes,
	string? SeparatorEnumMember
);

/// <summary>
/// A single <c>[IdPrefix]</c> attribute application. Declaration order is preserved by
/// the index in the parent descriptor's <c>Prefixes</c> array.
/// </summary>
internal sealed record PrefixDeclaration (string Prefix, bool IsDefault) : IEquatable<PrefixDeclaration>;

/// <summary>
/// A snapshot of an <see cref="IdStringAttribute"/>-decorated user type.
/// </summary>
internal sealed record StringOptionsDescriptor (
	string FullyQualifiedName,
	int MaxLength,
	string CharSetEnumMember,
	bool IgnoreCase
);
