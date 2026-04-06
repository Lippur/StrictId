using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StrictId.Generators.Analyzers;

/// <summary>
/// Roslyn analyzer that validates <c>[IdPrefix]</c> and <c>[IdSeparator]</c> attribute
/// applications. Surfaces STRID003 for malformed prefixes (invalid grammar, duplicates,
/// missing or multiple defaults) and STRID004 for out-of-range separator enum values.
/// Fires at symbol-declaration time so diagnostics appear immediately in the IDE and
/// block problematic builds.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class StrictIdAttributeAnalyzer : DiagnosticAnalyzer
{
	private const string Category = "StrictId";
	private const string IdPrefixAttributeMetadataName = "StrictId.IdPrefixAttribute";
	private const string IdSeparatorAttributeMetadataName = "StrictId.IdSeparatorAttribute";

	/// <summary>STRID003 — the <c>[IdPrefix]</c> declarations on the type violate the grammar or cardinality rules.</summary>
	public static readonly DiagnosticDescriptor InvalidIdPrefix = new(
		id: "STRID003",
		title: "Invalid [IdPrefix]",
		messageFormat: "[IdPrefix] on '{0}' is invalid: {1}",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		description: "Each prefix must match ^[a-z][a-z0-9_]{0,62}$, must be unique within a type, and when more than one is declared exactly one must be marked IsDefault.");

	/// <summary>STRID004 — the <c>[IdSeparator]</c> argument is outside the closed enum.</summary>
	public static readonly DiagnosticDescriptor InvalidIdSeparator = new(
		id: "STRID004",
		title: "Invalid [IdSeparator]",
		messageFormat: "[IdSeparator] on '{0}' uses value {1}, which is not a defined IdSeparator member",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		description: "IdSeparator is a closed enum. The only valid members are Underscore, Slash, Period, and Colon.",
		customTags: WellKnownDiagnosticTags.CompilationEnd);

	/// <inheritdoc />
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
		ImmutableArray.Create(InvalidIdPrefix, InvalidIdSeparator);

	/// <inheritdoc />
	public override void Initialize (AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
		context.RegisterCompilationAction(AnalyzeAssemblySeparator);
	}

	private static void AnalyzeNamedType (SymbolAnalysisContext context)
	{
		var type = (INamedTypeSymbol)context.Symbol;
		AnalyzeIdPrefix(type, context);
		AnalyzeIdSeparator(type, context);
	}

	// ═════ STRID003 ══════════════════════════════════════════════════════════

	private static void AnalyzeIdPrefix (INamedTypeSymbol type, SymbolAnalysisContext context)
	{
		var prefixAttrs = GetAttributes(type, IdPrefixAttributeMetadataName);
		if (prefixAttrs.Count == 0) return;

		// Per-declaration grammar check. Each violation reports at its own attribute
		// location so IDE squiggles point to the exact bad prefix.
		var validDeclarations = new List<(string prefix, bool isDefault, AttributeData attr)>(prefixAttrs.Count);
		var seenPrefixes = new HashSet<string>(StringComparer.Ordinal);

		foreach (var attr in prefixAttrs)
		{
			if (attr.ConstructorArguments.Length == 0) continue;
			if (attr.ConstructorArguments[0].Value is not string prefix)
			{
				ReportPrefix(context, attr, type, "prefix argument is not a string");
				continue;
			}

			var grammarError = PrefixValidator.ValidateGrammar(prefix);
			if (grammarError is not null)
			{
				ReportPrefix(context, attr, type, $"prefix '{prefix}' — {grammarError}");
				continue;
			}

			if (!seenPrefixes.Add(prefix))
			{
				ReportPrefix(context, attr, type, $"prefix '{prefix}' is declared more than once");
				continue;
			}

			var isDefault = false;
			foreach (var named in attr.NamedArguments)
			{
				if (named.Key == "IsDefault" && named.Value.Value is bool b)
				{
					isDefault = b;
					break;
				}
			}

			validDeclarations.Add((prefix, isDefault, attr));
		}

		// Cardinality rules only apply when more than one prefix survives the
		// per-declaration checks.
		if (validDeclarations.Count <= 1) return;

		var defaults = validDeclarations.Count(d => d.isDefault);
		if (defaults == 0)
		{
			// Report on the type declaration itself (the first attribute is as good
			// a proxy as any) — the issue is the set of attributes, not a single one.
			ReportPrefix(
				context,
				validDeclarations[0].attr,
				type,
				$"{validDeclarations.Count} [IdPrefix] attributes are declared but none is marked IsDefault = true. Exactly one must be the canonical default");
		}
		else if (defaults > 1)
		{
			// Flag each redundant default individually so users see every offender.
			foreach (var (prefix, isDefault, attr) in validDeclarations)
			{
				if (!isDefault) continue;
				ReportPrefix(
					context,
					attr,
					type,
					$"prefix '{prefix}' is one of {defaults} attributes marked IsDefault = true. Exactly one may be the canonical default");
			}
		}
	}

	private static void ReportPrefix (
		SymbolAnalysisContext context,
		AttributeData attr,
		INamedTypeSymbol type,
		string reason)
	{
		var location = GetAttributeLocation(attr);
		context.ReportDiagnostic(Diagnostic.Create(
			InvalidIdPrefix,
			location,
			type.ToDisplayString(),
			reason));
	}

	// ═════ STRID004 ══════════════════════════════════════════════════════════

	private static void AnalyzeIdSeparator (INamedTypeSymbol type, SymbolAnalysisContext context)
	{
		var separatorAttrs = GetAttributes(type, IdSeparatorAttributeMetadataName);
		if (separatorAttrs.Count == 0) return;

		foreach (var attr in separatorAttrs)
		{
			if (attr.ConstructorArguments.Length == 0) continue;
			var value = attr.ConstructorArguments[0].Value;
			if (value is not int intValue) continue;

			// IdSeparator is a closed enum with four members at ordinals 0–3
			// (Underscore, Slash, Period, Colon). Any other int (e.g., from a cast
			// like (IdSeparator)99) is an out-of-range value.
			if (intValue is >= 0 and <= 3) continue;

			var location = GetAttributeLocation(attr);
			context.ReportDiagnostic(Diagnostic.Create(
				InvalidIdSeparator,
				location,
				type.ToDisplayString(),
				intValue));
		}
	}

	// ═════ Assembly-level STRID004 ═══════════════════════════════════════════

	private static void AnalyzeAssemblySeparator (CompilationAnalysisContext context)
	{
		foreach (var attr in context.Compilation.Assembly.GetAttributes())
		{
			if (attr.AttributeClass?.ToDisplayString() != IdSeparatorAttributeMetadataName) continue;
			if (attr.ConstructorArguments.Length == 0) continue;
			var value = attr.ConstructorArguments[0].Value;
			if (value is not int intValue) continue;

			if (intValue is >= 0 and <= 3) continue;

			var location = GetAttributeLocation(attr);
			context.ReportDiagnostic(Diagnostic.Create(
				InvalidIdSeparator,
				location,
				context.Compilation.AssemblyName ?? "<assembly>",
				intValue));
		}
	}

	// ═════ Helpers ═══════════════════════════════════════════════════════════

	private static List<AttributeData> GetAttributes (INamedTypeSymbol type, string attributeMetadataName)
	{
		var result = new List<AttributeData>();
		foreach (var attr in type.GetAttributes())
		{
			if (attr.AttributeClass?.ToDisplayString() == attributeMetadataName)
				result.Add(attr);
		}
		return result;
	}

	private static Location GetAttributeLocation (AttributeData attr)
	{
		var syntaxRef = attr.ApplicationSyntaxReference;
		return syntaxRef is not null
			? Location.Create(syntaxRef.SyntaxTree, syntaxRef.Span)
			: Location.None;
	}
}
