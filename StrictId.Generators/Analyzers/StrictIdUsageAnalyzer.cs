using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace StrictId.Generators.Analyzers;

/// <summary>
/// Roslyn analyzer that reports usage-pattern mistakes involving StrictId types.
/// Groups four rules that all hinge on recognising closed <c>Id&lt;T&gt;</c>,
/// <c>IdNumber&lt;T&gt;</c>, and <c>IdString&lt;T&gt;</c> types: STRID001 cross-type
/// <c>.Value</c> comparison, STRID002 <c>default(Id&lt;T&gt;)</c> where
/// <c>NewId()</c> is likely intended, STRID005 mixing an entity's declared attribute
/// family with the wrong StrictId wrapper, and STRID006 closing a StrictId generic
/// with a generic type parameter.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class StrictIdUsageAnalyzer : DiagnosticAnalyzer
{
	private const string Category = "StrictId";

	// Metadata names for the four StrictId open generics and the IdString attribute.
	// Metadata format for generics uses a backtick-arity suffix.
	private const string IdOpenGenericMetadataName = "StrictId.Id`1";
	private const string IdNumberOpenGenericMetadataName = "StrictId.IdNumber`1";
	private const string IdStringOpenGenericMetadataName = "StrictId.IdString`1";
	private const string GuidOpenGenericMetadataName = "StrictId.Guid`1";
	private const string IdStringAttributeMetadataName = "StrictId.IdStringAttribute";

	/// <summary>STRID001 — comparing <c>.Value</c> across two different closed StrictId generic types.</summary>
	public static readonly DiagnosticDescriptor CrossTypeValueComparison = new(
		id: "STRID001",
		title: "Cross-type StrictId .Value comparison",
		messageFormat: "Comparing .Value across different StrictId types ('{0}' and '{1}'). Even if the underlying values are equal, the identifiers belong to different entities and should not be compared.",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		description: "StrictId's strong type safety prevents direct comparison of Id<A> with Id<B>. Comparing their .Value properties bypasses that safety.");

	/// <summary>STRID002 — assigning <c>default(Id&lt;T&gt;)</c> to an <c>Id</c> property, likely a mistake.</summary>
	public static readonly DiagnosticDescriptor DefaultIdAssignment = new(
		id: "STRID002",
		title: "default(Id<T>) assigned to an 'Id' property",
		messageFormat: "Assigning default({0}) to an 'Id' property produces an empty identifier; call {0}.NewId() instead",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Info,
		isEnabledByDefault: true,
		description: "A default(Id<T>) value is the zero ULID, which collides across entities and cannot be meaningfully compared. Use NewId() to generate a fresh identifier.");

	/// <summary>STRID005 — using <c>Id&lt;T&gt;</c>/<c>IdNumber&lt;T&gt;</c> when <c>T</c> declares <c>[IdString]</c>.</summary>
	public static readonly DiagnosticDescriptor WrongIdFamily = new(
		id: "STRID005",
		title: "Wrong StrictId family for entity attribute configuration",
		messageFormat: "'{0}' declares [IdString] but is being used via {1}<{0}>. Use IdString<{0}> to honour the attribute.",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		description: "An entity decorated with [IdString] should be used with IdString<T>. Mixing families causes the [IdString] configuration to be silently ignored.");

	/// <summary>STRID006 — closing a StrictId generic with a generic type parameter.</summary>
	public static readonly DiagnosticDescriptor OpenGenericIdParameter = new(
		id: "STRID006",
		title: "StrictId closed with a generic type parameter",
		messageFormat: "'{0}<{1}>' is parameterised by the type parameter '{1}'. Strong type safety requires a concrete entity type, not an open type parameter.",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		description: "StrictId's type tag must refer to a specific entity type so the compiler can prevent cross-entity mix-ups. A generic type parameter defeats that guarantee.");

	/// <inheritdoc />
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
		ImmutableArray.Create(CrossTypeValueComparison, DefaultIdAssignment, WrongIdFamily, OpenGenericIdParameter);

	/// <inheritdoc />
	public override void Initialize (AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();

		context.RegisterCompilationStartAction(compilationContext =>
		{
			var compilation = compilationContext.Compilation;
			var idType = compilation.GetTypeByMetadataName(IdOpenGenericMetadataName);
			var idNumberType = compilation.GetTypeByMetadataName(IdNumberOpenGenericMetadataName);
			var idStringType = compilation.GetTypeByMetadataName(IdStringOpenGenericMetadataName);
			var guidType = compilation.GetTypeByMetadataName(GuidOpenGenericMetadataName);
			var idStringAttrType = compilation.GetTypeByMetadataName(IdStringAttributeMetadataName);

			// If StrictId isn't referenced at all the symbols won't resolve and there
			// is nothing to check. Bail out silently — this lets the analyzer be
			// harmless in projects that happen to have it on the analyzer list.
			if (idType is null || idNumberType is null || idStringType is null) return;

			var cache = new StrictIdSymbolCache(idType, idNumberType, idStringType, guidType, idStringAttrType);

			compilationContext.RegisterOperationAction(
				ctx => AnalyzeBinaryOperation(ctx, cache),
				OperationKind.Binary);

			compilationContext.RegisterOperationAction(
				ctx => AnalyzeSimpleAssignment(ctx, cache),
				OperationKind.SimpleAssignment);

			compilationContext.RegisterSyntaxNodeAction(
				ctx => AnalyzeGenericName(ctx, cache),
				SyntaxKind.GenericName);
		});
	}

	// ═════ STRID001 — cross-type .Value comparison ═══════════════════════════

	private static void AnalyzeBinaryOperation (OperationAnalysisContext context, StrictIdSymbolCache cache)
	{
		var op = (IBinaryOperation)context.Operation;
		if (op.OperatorKind is not (BinaryOperatorKind.Equals or BinaryOperatorKind.NotEquals)) return;

		var leftType = ExtractValuePropertyContainingType(op.LeftOperand, cache);
		if (leftType is null) return;

		var rightType = ExtractValuePropertyContainingType(op.RightOperand, cache);
		if (rightType is null) return;

		// Same closed generic type on both sides → no diagnostic. The analyzer only
		// fires when the containing types differ.
		if (SymbolEqualityComparer.Default.Equals(leftType, rightType)) return;

		context.ReportDiagnostic(Diagnostic.Create(
			CrossTypeValueComparison,
			op.Syntax.GetLocation(),
			leftType.ToDisplayString(),
			rightType.ToDisplayString()));
	}

	/// <summary>
	/// If <paramref name="operation"/> is a direct <c>.Value</c> property access on a
	/// closed StrictId generic type, returns that containing type; otherwise
	/// <see langword="null"/>. Transparently steps through implicit conversions so the
	/// comparison works regardless of operator-resolved widening.
	/// </summary>
	private static INamedTypeSymbol? ExtractValuePropertyContainingType (IOperation operation, StrictIdSymbolCache cache)
	{
		// Roslyn wraps property accesses in IConversionOperation when the binary
		// operator coerces types; unwrap to find the underlying IPropertyReferenceOperation.
		while (operation is IConversionOperation conversion)
			operation = conversion.Operand;

		if (operation is not IPropertyReferenceOperation propRef) return null;
		if (propRef.Property.Name != "Value") return null;

		var containing = propRef.Property.ContainingType;
		if (containing is null) return null;
		if (!cache.IsStrictIdGeneric(containing)) return null;

		return containing;
	}

	// ═════ STRID002 — default(Id<T>) → NewId() suggestion ════════════════════

	private static void AnalyzeSimpleAssignment (OperationAnalysisContext context, StrictIdSymbolCache cache)
	{
		var assignment = (ISimpleAssignmentOperation)context.Operation;

		// The value must be a literal default expression (`default`, `default(Id<T>)`).
		// Roslyn sometimes wraps the default in an implicit IConversionOperation when
		// the expression has no explicit type — unwrap so we see the underlying default.
		var value = assignment.Value;
		while (value is IConversionOperation conv) value = conv.Operand;
		if (value is not IDefaultValueOperation) return;

		// The target must be a property reference named "Id" whose type is a closed
		// Id<T> or Guid<T>. IdNumber and IdString don't have NewId().
		if (assignment.Target is not IPropertyReferenceOperation propRef) return;
		if (propRef.Property.Name != "Id") return;

		var propType = propRef.Property.Type as INamedTypeSymbol;
		if (propType is null || (!cache.IsIdGeneric(propType) && !cache.IsGuidGeneric(propType))) return;

		// Only fire inside an object initializer or a constructor body — this keeps
		// the heuristic narrow enough to avoid flagging legitimate `id = default`
		// resets in general-purpose code.
		if (!IsInsideObjectInitializerOrConstructor(assignment, context.ContainingSymbol)) return;

		context.ReportDiagnostic(Diagnostic.Create(
			DefaultIdAssignment,
			assignment.Syntax.GetLocation(),
			propType.ToDisplayString()));
	}

	private static bool IsInsideObjectInitializerOrConstructor (IOperation operation, ISymbol containingSymbol)
	{
		// Constructor body: the immediately containing member is a constructor.
		if (containingSymbol is IMethodSymbol { MethodKind: MethodKind.Constructor }) return true;

		// Object initializer: walk up the operation tree looking for an
		// IObjectOrCollectionInitializerOperation ancestor.
		for (var current = operation.Parent; current is not null; current = current.Parent)
		{
			if (current is IObjectOrCollectionInitializerOperation) return true;
		}

		return false;
	}

	// ═════ STRID005 + STRID006 — generic-name syntax analysis ════════════════

	private static void AnalyzeGenericName (SyntaxNodeAnalysisContext context, StrictIdSymbolCache cache)
	{
		var genericName = (GenericNameSyntax)context.Node;

		// Quick syntactic filter: short-circuit when the simple identifier isn't one
		// of the three StrictId generic names. This keeps the analyzer cheap; the
		// semantic resolve only runs on plausible candidates.
		var identifier = genericName.Identifier.ValueText;
		if (identifier is not ("Id" or "IdNumber" or "IdString" or "Guid")) return;

		var symbolInfo = context.SemanticModel.GetSymbolInfo(genericName, context.CancellationToken);
		var symbol = symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault();
		if (symbol is not INamedTypeSymbol closedGeneric) return;
		if (!cache.IsStrictIdGeneric(closedGeneric)) return;

		if (closedGeneric.TypeArguments.Length != 1) return;
		var typeArgument = closedGeneric.TypeArguments[0];

		// STRID006 — generic type parameter. Fires first because the other rule is
		// moot when the argument isn't a concrete type. StrictId's own library
		// assemblies declare intentional wrapper generics (for example
		// IdTypedJsonConverter<T> : JsonConverter<Id<T>>, and the Id<T>/IdNumber<T>/
		// IdString<T> types themselves whose operators and members reference their
		// own T through the closed generic). Scope the rule to the assembly being
		// compiled so it only fires on user code, not on any StrictId source tree.
		if (typeArgument is ITypeParameterSymbol typeParam)
		{
			if (!IsStrictIdLibraryAssembly(context.Compilation.Assembly))
			{
				context.ReportDiagnostic(Diagnostic.Create(
					OpenGenericIdParameter,
					genericName.GetLocation(),
					closedGeneric.OriginalDefinition.Name,
					typeParam.Name));
			}
			return;
		}

		// STRID005 — entity has [IdString] but is being used via Id<T>/IdNumber<T>.
		if (cache.IdStringAttribute is null) return;
		if (SymbolEqualityComparer.Default.Equals(closedGeneric.OriginalDefinition, cache.IdString))
			return; // IdString<T> is correct when T has [IdString]
		if (!HasIdStringAttribute(typeArgument, cache.IdStringAttribute)) return;

		string familyName;
		if (SymbolEqualityComparer.Default.Equals(closedGeneric.OriginalDefinition, cache.Id))
			familyName = "Id";
		else if (SymbolEqualityComparer.Default.Equals(closedGeneric.OriginalDefinition, cache.IdNumber))
			familyName = "IdNumber";
		else
			familyName = "Guid";

		context.ReportDiagnostic(Diagnostic.Create(
			WrongIdFamily,
			genericName.GetLocation(),
			typeArgument.ToDisplayString(),
			familyName));
	}

	/// <summary>
	/// Returns <see langword="true"/> if <paramref name="assembly"/> is one of the
	/// StrictId library assemblies. Used to exempt library-internal wrapper generics
	/// from STRID006: types like <c>IdTypedJsonConverter&lt;T&gt;</c> legitimately
	/// propagate a generic parameter through <c>Id&lt;T&gt;</c> by design.
	/// </summary>
	private static bool IsStrictIdLibraryAssembly (IAssemblySymbol? assembly)
	{
		if (assembly is null) return false;
		var name = assembly.Name;
		return name is "StrictId" or "StrictId.EFCore" or "StrictId.AspNetCore";
	}

	private static bool HasIdStringAttribute (ITypeSymbol type, INamedTypeSymbol attributeSymbol)
	{
		// Walk the inheritance chain so an attribute declared on a base type still
		// triggers the rule on a derived type used as the type argument.
		var current = type as INamedTypeSymbol;
		while (current is not null)
		{
			foreach (var attr in current.GetAttributes())
			{
				if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass, attributeSymbol))
					return true;
			}
			current = current.BaseType;
		}
		return false;
	}

	// ═════ Symbol cache ══════════════════════════════════════════════════════

	private sealed class StrictIdSymbolCache (
		INamedTypeSymbol id,
		INamedTypeSymbol idNumber,
		INamedTypeSymbol idString,
		INamedTypeSymbol? guid,
		INamedTypeSymbol? idStringAttribute)
	{
		public INamedTypeSymbol Id { get; } = id;
		public INamedTypeSymbol IdNumber { get; } = idNumber;
		public INamedTypeSymbol IdString { get; } = idString;
		public INamedTypeSymbol? Guid { get; } = guid;
		public INamedTypeSymbol? IdStringAttribute { get; } = idStringAttribute;

		public bool IsStrictIdGeneric (INamedTypeSymbol type)
		{
			var def = type.OriginalDefinition;
			return SymbolEqualityComparer.Default.Equals(def, Id)
				|| SymbolEqualityComparer.Default.Equals(def, IdNumber)
				|| SymbolEqualityComparer.Default.Equals(def, IdString)
				|| (Guid is not null && SymbolEqualityComparer.Default.Equals(def, Guid));
		}

		public bool IsIdGeneric (INamedTypeSymbol type)
			=> SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, Id);

		public bool IsGuidGeneric (INamedTypeSymbol type)
			=> Guid is not null && SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, Guid);
	}
}
