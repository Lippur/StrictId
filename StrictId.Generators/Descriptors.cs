namespace StrictId.Generators;

/// <summary>
/// A snapshot of an <see cref="IdPrefixAttribute"/>-decorated user type, captured
/// during the incremental-generator scan. Flows through the pipeline as an equatable
/// record so the incremental cache can short-circuit when the user only touches
/// unrelated code. A descriptor with an empty <see cref="FullyQualifiedName"/>
/// represents a type that was filtered out (either inaccessible from generated code,
/// or malformed — the analyzer surfaces the malformed case as STRID003).
/// </summary>
internal sealed record PrefixDescriptor (
	string FullyQualifiedName,
	EquatableArray<PrefixDeclaration> Prefixes,
	string SeparatorEnumMember
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
