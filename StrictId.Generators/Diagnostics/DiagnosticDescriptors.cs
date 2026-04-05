using Microsoft.CodeAnalysis;

namespace StrictId.Generators.Diagnostics;

/// <summary>
/// Diagnostic descriptors emitted by the StrictId source generator during Phase 8.
/// The STRID1xx range is reserved for generator-emitted diagnostics so the STRID0xx
/// range remains available for the analyzers shipped in Phase 9.
/// </summary>
internal static class DiagnosticDescriptors
{
	private const string Category = "StrictId";

	public static readonly DiagnosticDescriptor InvalidPrefixGrammar = new(
		id: "STRID101",
		title: "Invalid [IdPrefix] grammar",
		messageFormat: "Prefix '{0}' on type '{1}' is invalid: {2}. Prefixes must match ^[a-z][a-z0-9_]{{0,62}}$.",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	public static readonly DiagnosticDescriptor DuplicatePrefix = new(
		id: "STRID102",
		title: "Duplicate [IdPrefix] declaration",
		messageFormat: "Prefix '{0}' is declared more than once on type '{1}'. Each prefix must be unique within a type.",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	public static readonly DiagnosticDescriptor NoDefaultPrefix = new(
		id: "STRID103",
		title: "No default [IdPrefix]",
		messageFormat: "Type '{0}' declares {1} [IdPrefix] attributes but none is marked IsDefault = true. Exactly one must be the canonical default when more than one is declared.",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	public static readonly DiagnosticDescriptor MultipleDefaultPrefixes = new(
		id: "STRID104",
		title: "Multiple default [IdPrefix] attributes",
		messageFormat: "Type '{0}' declares {1} [IdPrefix] attributes marked IsDefault = true. Exactly one prefix may be the canonical default.",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);
}
