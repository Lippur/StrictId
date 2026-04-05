namespace StrictId.Generators;

/// <summary>
/// A snapshot of an <see cref="IdPrefixAttribute"/>-decorated user type, captured
/// during the incremental-generator scan. Flows through the pipeline as an equatable
/// record so the incremental cache can short-circuit when the user only touches
/// unrelated code.
/// </summary>
internal sealed record PrefixDescriptor (
	string FullyQualifiedName,
	string EscapedIdentifier,
	EquatableArray<PrefixDeclaration> Prefixes,
	string SeparatorEnumMember,
	EquatableArray<DiagnosticData> Diagnostics
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
	string EscapedIdentifier,
	int MaxLength,
	string CharSetEnumMember,
	bool IgnoreCase
);

/// <summary>
/// A serialisable form of a <see cref="Microsoft.CodeAnalysis.Diagnostic"/> that can
/// flow through the incremental pipeline with value equality. The generator rehydrates
/// these into real diagnostics inside <c>RegisterSourceOutput</c>.
/// </summary>
internal sealed record DiagnosticData (
	string Id,
	string Title,
	string Message,
	string FileHint,
	int LineSpanStart,
	int LineSpanLength
);
