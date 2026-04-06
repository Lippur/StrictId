using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StrictId.Generators;

/// <summary>
/// Roslyn incremental source generator that emits <c>[ModuleInitializer]</c> calls
/// populating <c>StrictIdRegistry</c> with prefix and string-option metadata for every
/// type decorated with <c>[IdPrefix]</c>, <c>[IdString]</c>, or both. Types the
/// generator did not see still work via the runtime reflection fallback. Only types
/// that directly declare a StrictId attribute are captured; inherited-only declarations
/// fall through to the reflection path.
/// </summary>
[Generator]
public sealed class StrictIdGenerator : IIncrementalGenerator
{
	private const string IdPrefixAttributeMetadataName = "StrictId.IdPrefixAttribute";
	private const string IdStringAttributeMetadataName = "StrictId.IdStringAttribute";
	private const string EnableSwitchBuildPropertyKey = "build_property.EnableStrictIdSourceGenerator";
	private const string GeneratedFileHint = "StrictIdRegistrations.g.cs";

	private const string EfCoreRegistryMetadataName = "StrictId.EFCore.StrictIdEfCoreRegistry";

	/// <inheritdoc />
	public void Initialize (IncrementalGeneratorInitializationContext context)
	{
		// MSBuild opt-out. Default: generator enabled. Set
		// <EnableStrictIdSourceGenerator>false</EnableStrictIdSourceGenerator> in the
		// consuming project to skip the whole pipeline. We still run the transforms
		// (Roslyn does not let us branch on this earlier) but RegisterSourceOutput
		// emits nothing.
		var enabled = context.AnalyzerConfigOptionsProvider
			.Select(static (provider, _) =>
			{
				if (provider.GlobalOptions.TryGetValue(EnableSwitchBuildPropertyKey, out var value) &&
				    string.Equals(value, "false", StringComparison.OrdinalIgnoreCase))
				{
					return false;
				}
				return true;
			});

		// Emit EF Core value-converter registrations only when StrictId.EFCore is
		// actually referenced by the consuming compilation — otherwise the generated
		// file would fail to resolve StrictIdEfCoreRegistry and the open-generic
		// converter types it needs. The CompilationProvider only re-runs when the
		// reference graph changes, so this is cheap for the incremental cache.
		var efCoreAvailable = context.CompilationProvider
			.Select(static (compilation, _) =>
				compilation.GetTypeByMetadataName(EfCoreRegistryMetadataName) is not null);

		// Assembly-level [IdSeparator] fallback. When a type does not declare its own
		// [IdSeparator], this value is used instead of the hardcoded Underscore default.
		// Re-evaluated when assembly-level attributes change (CompilationProvider
		// granularity), which is the correct invalidation boundary.
		var assemblySeparator = context.CompilationProvider
			.Select(static (compilation, _) => ReadAssemblySeparator(compilation));

		var prefixProvider = context.SyntaxProvider.ForAttributeWithMetadataName(
			IdPrefixAttributeMetadataName,
			predicate: static (node, _) => node is ClassDeclarationSyntax or StructDeclarationSyntax or RecordDeclarationSyntax,
			transform: static (syntaxContext, cancellationToken) => ExtractPrefixDescriptor(syntaxContext, cancellationToken));

		var stringProvider = context.SyntaxProvider.ForAttributeWithMetadataName(
			IdStringAttributeMetadataName,
			predicate: static (node, _) => node is ClassDeclarationSyntax or StructDeclarationSyntax or RecordDeclarationSyntax,
			transform: static (syntaxContext, cancellationToken) => ExtractStringDescriptor(syntaxContext, cancellationToken));

		var combined = prefixProvider.Collect()
			.Combine(stringProvider.Collect())
			.Combine(enabled)
			.Combine(efCoreAvailable)
			.Combine(assemblySeparator);

		context.RegisterSourceOutput(combined, static (sourceContext, payload) =>
		{
			var ((((prefixes, strings), isEnabled), efAvailable), asmSeparator) = payload;
			Emit(sourceContext, prefixes, strings, isEnabled, efAvailable, asmSeparator);
		});
	}

	// ═════ Prefix descriptor extraction ══════════════════════════════════════

	private static PrefixDescriptor ExtractPrefixDescriptor (
		GeneratorAttributeSyntaxContext syntaxContext,
		CancellationToken cancellationToken)
	{
		var target = (INamedTypeSymbol)syntaxContext.TargetSymbol;
		if (!IsAccessibleFromGeneratedCode(target))
		{
			// Private/protected nested types (typically test fixtures) can't be named
			// from a top-level namespace, so we skip them. They still work correctly at
			// runtime via the reflection fallback.
			return EmptyDescriptor();
		}

		var declarations = ImmutableArray.CreateBuilder<PrefixDeclaration>();

		foreach (var attrData in syntaxContext.Attributes)
		{
			cancellationToken.ThrowIfCancellationRequested();

			if (attrData.ConstructorArguments.Length == 0) continue;

			var prefix = attrData.ConstructorArguments[0].Value as string;
			if (prefix is null) continue;

			// Invalid grammar: silently skip this declaration. STRID003 (in
			// StrictIdAttributeAnalyzer) surfaces the error to the user; the generator
			// just refuses to emit a registration that would produce an unusable
			// runtime state.
			if (PrefixValidator.ValidateGrammar(prefix) is not null) continue;

			var isDefault = false;
			foreach (var named in attrData.NamedArguments)
			{
				if (named.Key == "IsDefault" && named.Value.Value is bool b)
				{
					isDefault = b;
					break;
				}
			}

			declarations.Add(new PrefixDeclaration(prefix, isDefault));
		}

		// Drop duplicates silently (analyzer reports them). Keep the first occurrence
		// to preserve declaration order.
		var seenPrefixes = new HashSet<string>(StringComparer.Ordinal);
		for (var i = 0; i < declarations.Count; i++)
		{
			if (!seenPrefixes.Add(declarations[i].Prefix))
			{
				declarations.RemoveAt(i);
				i--;
			}
		}

		// If no prefix survived grammar validation, the user's [IdPrefix] declaration
		// is entirely broken. Filter the whole type out so no downstream (JSON, EF)
		// registrations attach to it; the analyzer surfaces the grammar error.
		if (declarations.Count == 0) return EmptyDescriptor();

		// If multiple prefixes remain and there isn't exactly one IsDefault, don't
		// emit — the analyzer reports the mismatch and the generator stays silent
		// rather than picking an arbitrary canonical.
		if (declarations.Count > 1)
		{
			var defaults = 0;
			foreach (var d in declarations)
				if (d.IsDefault)
					defaults++;
			if (defaults != 1) return EmptyDescriptor();
		}

		// Locate an [IdSeparator] on the target, if any. The attribute lives on the same
		// symbol but is discovered via GetAttributes() since our provider keyed on
		// [IdPrefix]. No inheritance walk here — the runtime fallback handles inherited
		// separators on types the generator did not see directly.
		var separator = ReadSeparator(target);

		return new PrefixDescriptor(
			FullyQualifiedName: target.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
			Prefixes: new EquatableArray<PrefixDeclaration>(declarations.ToImmutable()),
			SeparatorEnumMember: separator);
	}

	private static PrefixDescriptor EmptyDescriptor () => new(
		FullyQualifiedName: string.Empty,
		Prefixes: EquatableArray<PrefixDeclaration>.Empty,
		SeparatorEnumMember: null);

	/// <summary>
	/// Reads the type-level <c>[IdSeparator]</c> on <paramref name="target"/>. Returns
	/// the enum member name if found, or <see langword="null"/> when no type-level
	/// separator is declared — signalling that the emission phase should apply the
	/// assembly-level fallback.
	/// </summary>
	private static string? ReadSeparator (INamedTypeSymbol target)
	{
		foreach (var attr in target.GetAttributes())
		{
			if (attr.AttributeClass?.ToDisplayString() == "StrictId.IdSeparatorAttribute" &&
			    attr.ConstructorArguments.Length > 0)
			{
				var sepValue = attr.ConstructorArguments[0].Value;
				if (sepValue is int intValue)
				{
					return intValue switch
					{
						0 => "Underscore",
						1 => "Slash",
						2 => "Period",
						3 => "Colon",
						_ => "Underscore",
					};
				}
			}
		}
		return null;
	}

	/// <summary>
	/// Reads the assembly-level <c>[assembly: IdSeparator(...)]</c> from the compilation.
	/// Returns the enum member name if found, or <c>"Underscore"</c> as the built-in default.
	/// </summary>
	private static string ReadAssemblySeparator (Compilation compilation)
	{
		foreach (var attr in compilation.Assembly.GetAttributes())
		{
			if (attr.AttributeClass?.ToDisplayString() == "StrictId.IdSeparatorAttribute" &&
			    attr.ConstructorArguments.Length > 0)
			{
				var sepValue = attr.ConstructorArguments[0].Value;
				if (sepValue is int intValue)
				{
					return intValue switch
					{
						0 => "Underscore",
						1 => "Slash",
						2 => "Period",
						3 => "Colon",
						_ => "Underscore",
					};
				}
			}
		}
		return "Underscore";
	}

	// ═════ String descriptor extraction ══════════════════════════════════════

	private static StringOptionsDescriptor ExtractStringDescriptor (
		GeneratorAttributeSyntaxContext syntaxContext,
		CancellationToken cancellationToken)
	{
		var target = (INamedTypeSymbol)syntaxContext.TargetSymbol;
		if (!IsAccessibleFromGeneratedCode(target))
		{
			return new StringOptionsDescriptor(
				FullyQualifiedName: string.Empty,
				MaxLength: 0,
				CharSetEnumMember: "Any",
				IgnoreCase: false);
		}

		var attrData = syntaxContext.Attributes[0];

		var maxLength = 255;
		var charSet = "Any";
		var ignoreCase = false;

		foreach (var named in attrData.NamedArguments)
		{
			cancellationToken.ThrowIfCancellationRequested();
			switch (named.Key)
			{
				case "MaxLength" when named.Value.Value is int ml:
					maxLength = ml;
					break;
				case "CharSet" when named.Value.Value is int cs:
					charSet = cs switch
					{
						0 => "Any",
						1 => "Alphanumeric",
						2 => "AlphanumericDash",
						3 => "AlphanumericUnderscore",
						4 => "AlphanumericDashUnderscore",
						_ => "AlphanumericDashUnderscore",
					};
					break;
				case "IgnoreCase" when named.Value.Value is bool ic:
					ignoreCase = ic;
					break;
			}
		}

		return new StringOptionsDescriptor(
			FullyQualifiedName: target.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
			MaxLength: maxLength,
			CharSetEnumMember: charSet,
			IgnoreCase: ignoreCase);
	}

	// ═════ Emission ══════════════════════════════════════════════════════════

	private static void Emit (
		SourceProductionContext context,
		ImmutableArray<PrefixDescriptor> prefixDescriptors,
		ImmutableArray<StringOptionsDescriptor> stringDescriptors,
		bool enabled,
		bool efCoreAvailable,
		string assemblySeparator)
	{
		// STRID003/STRID004 diagnostics are surfaced by StrictIdAttributeAnalyzer rather
		// than the generator; the generator's job is to emit correct code for the
		// valid subset and stay silent about the invalid tail. Types that fail the
		// grammar/cardinality checks surface as empty descriptors here and are
		// filtered out in BuildSource.

		if (!enabled) return;
		if (prefixDescriptors.IsEmpty && stringDescriptors.IsEmpty) return;

		var source = BuildSource(prefixDescriptors, stringDescriptors, efCoreAvailable, assemblySeparator);
		context.AddSource(GeneratedFileHint, source);
	}

	private static string BuildSource (
		ImmutableArray<PrefixDescriptor> prefixDescriptors,
		ImmutableArray<StringOptionsDescriptor> stringDescriptors,
		bool efCoreAvailable,
		string assemblySeparator)
	{
		var sb = new StringBuilder();
		sb.AppendLine("// <auto-generated/>");
		sb.AppendLine("// Generated by StrictId.Generators. Do not edit.");
		sb.AppendLine("#nullable enable");
		sb.AppendLine();
		sb.AppendLine("namespace StrictId.Generated");
		sb.AppendLine("{");
		sb.AppendLine("    internal static class StrictIdRegistrations");
		sb.AppendLine("    {");
		sb.AppendLine("        [global::System.Runtime.CompilerServices.ModuleInitializer]");
		sb.AppendLine("        [global::System.Diagnostics.CodeAnalysis.SuppressMessage(\"Usage\", \"CA2255\", Justification = \"StrictId relies on module initialisation to pre-populate metadata caches before any id type is read.\")]");
		sb.AppendLine("        internal static void Initialize()");
		sb.AppendLine("        {");

		// Build the set of valid (accessible, well-formed) entity type names so we can
		// emit JSON + EF converter registrations for each one. Types with empty
		// FullyQualifiedName were filtered during extraction (inaccessible nesting,
		// invalid grammar, or cardinality mismatch — the analyzer surfaces the last
		// two as diagnostics). A type may appear in both descriptor lists when it
		// declares both [IdPrefix] and [IdString]; the HashSet dedupes those.
		var validEntityTypes = new List<string>();
		var seenEntityTypes = new HashSet<string>(StringComparer.Ordinal);

		foreach (var descriptor in prefixDescriptors)
		{
			if (descriptor.FullyQualifiedName.Length == 0) continue;
			EmitPrefixRegistration(sb, descriptor, assemblySeparator);
			if (seenEntityTypes.Add(descriptor.FullyQualifiedName))
				validEntityTypes.Add(descriptor.FullyQualifiedName);
		}

		foreach (var descriptor in stringDescriptors)
		{
			if (descriptor.FullyQualifiedName.Length == 0) continue;
			EmitStringRegistration(sb, descriptor);
			// [IdString]-only types still need JSON/EF converters so consumers
			// serialising or persisting IdString<T> stay on the AOT-friendly path.
			if (seenEntityTypes.Add(descriptor.FullyQualifiedName))
				validEntityTypes.Add(descriptor.FullyQualifiedName);
		}

		// JSON converter registrations: emit for every family (Id, IdNumber, IdString)
		// per valid entity type. The generator does not know which families the user
		// actually uses, so it pre-populates all three; each ends up as a single
		// dictionary entry and an object reference. Unused entries cost nothing beyond
		// their registry slot.
		foreach (var fqn in validEntityTypes)
		{
			EmitJsonRegistrations(sb, fqn);
		}

		// EF Core value-converter registrations: conditional on the consumer having a
		// reference to StrictId.EFCore. Otherwise the generated typeof(...) expression
		// would fail to resolve.
		if (efCoreAvailable)
		{
			foreach (var fqn in validEntityTypes)
			{
				EmitEfCoreRegistrations(sb, fqn);
			}
		}

		sb.AppendLine("        }");
		sb.AppendLine("    }");
		sb.AppendLine("}");
		return sb.ToString();
	}

	private static void EmitJsonRegistrations (StringBuilder sb, string fullyQualifiedName)
	{
		sb.Append("            global::StrictId.StrictIdRegistry.RegisterJsonConverter<global::StrictId.Id<");
		sb.Append(fullyQualifiedName);
		sb.Append(">>(new global::StrictId.Json.IdTypedJsonConverter<");
		sb.Append(fullyQualifiedName);
		sb.AppendLine(">());");

		sb.Append("            global::StrictId.StrictIdRegistry.RegisterJsonConverter<global::StrictId.IdNumber<");
		sb.Append(fullyQualifiedName);
		sb.Append(">>(new global::StrictId.Json.IdNumberTypedJsonConverter<");
		sb.Append(fullyQualifiedName);
		sb.AppendLine(">());");

		sb.Append("            global::StrictId.StrictIdRegistry.RegisterJsonConverter<global::StrictId.IdString<");
		sb.Append(fullyQualifiedName);
		sb.Append(">>(new global::StrictId.Json.IdStringTypedJsonConverter<");
		sb.Append(fullyQualifiedName);
		sb.AppendLine(">());");

		sb.Append("            global::StrictId.StrictIdRegistry.RegisterJsonConverter<global::StrictId.Guid<");
		sb.Append(fullyQualifiedName);
		sb.Append(">>(new global::StrictId.Json.GuidTypedJsonConverter<");
		sb.Append(fullyQualifiedName);
		sb.AppendLine(">());");
	}

	private static void EmitEfCoreRegistrations (StringBuilder sb, string fullyQualifiedName)
	{
		sb.Append("            global::StrictId.EFCore.StrictIdEfCoreRegistry.RegisterValueConverter<global::StrictId.Id<");
		sb.Append(fullyQualifiedName);
		sb.Append(">>(new global::StrictId.EFCore.ValueConverters.IdToStringConverter<");
		sb.Append(fullyQualifiedName);
		sb.AppendLine(">());");

		sb.Append("            global::StrictId.EFCore.StrictIdEfCoreRegistry.RegisterValueConverter<global::StrictId.IdNumber<");
		sb.Append(fullyQualifiedName);
		sb.Append(">>(new global::StrictId.EFCore.ValueConverters.IdNumberToLongConverter<");
		sb.Append(fullyQualifiedName);
		sb.AppendLine(">());");

		sb.Append("            global::StrictId.EFCore.StrictIdEfCoreRegistry.RegisterValueConverter<global::StrictId.IdString<");
		sb.Append(fullyQualifiedName);
		sb.Append(">>(new global::StrictId.EFCore.ValueConverters.IdStringToStringConverter<");
		sb.Append(fullyQualifiedName);
		sb.AppendLine(">());");

		sb.Append("            global::StrictId.EFCore.StrictIdEfCoreRegistry.RegisterValueConverter<global::StrictId.Guid<");
		sb.Append(fullyQualifiedName);
		sb.Append(">>(new global::StrictId.EFCore.ValueConverters.GuidToGuidConverter<");
		sb.Append(fullyQualifiedName);
		sb.AppendLine(">());");
	}

	private static void EmitPrefixRegistration (StringBuilder sb, PrefixDescriptor descriptor, string assemblySeparator)
	{
		if (descriptor.Prefixes.IsEmpty)
		{
			// No valid prefixes survived validation. Register nothing so the runtime
			// resolver emits PrefixInfo.None via reflection fallback.
			return;
		}

		string canonical;
		if (descriptor.Prefixes.Length == 1)
		{
			canonical = descriptor.Prefixes[0].Prefix;
		}
		else
		{
			canonical = string.Empty;
			foreach (var decl in descriptor.Prefixes)
			{
				if (decl.IsDefault)
				{
					canonical = decl.Prefix;
					break;
				}
			}
			if (canonical.Length == 0) return; // cardinality diagnostic already emitted
		}

		// Type-level separator wins; otherwise fall back to assembly-level, then
		// the built-in default (Underscore, baked into assemblySeparator).
		var effectiveSeparator = descriptor.SeparatorEnumMember ?? assemblySeparator;

		// Aliases: canonical first, then remaining in declaration order.
		sb.Append("            global::StrictId.StrictIdRegistry.RegisterPrefix<");
		sb.Append(descriptor.FullyQualifiedName);
		sb.Append(">(canonical: ");
		AppendStringLiteral(sb, canonical);
		sb.Append(", aliases: new string[] { ");
		AppendStringLiteral(sb, canonical);
		foreach (var decl in descriptor.Prefixes)
		{
			if (decl.Prefix == canonical) continue;
			sb.Append(", ");
			AppendStringLiteral(sb, decl.Prefix);
		}
		sb.Append(" }, separator: global::StrictId.IdSeparator.");
		sb.Append(effectiveSeparator);
		sb.AppendLine(");");
	}

	private static void EmitStringRegistration (StringBuilder sb, StringOptionsDescriptor descriptor)
	{
		sb.Append("            global::StrictId.StrictIdRegistry.RegisterStringOptions<");
		sb.Append(descriptor.FullyQualifiedName);
		sb.Append(">(maxLength: ");
		sb.Append(descriptor.MaxLength);
		sb.Append(", charSet: global::StrictId.IdStringCharSet.");
		sb.Append(descriptor.CharSetEnumMember);
		sb.Append(", ignoreCase: ");
		sb.Append(descriptor.IgnoreCase ? "true" : "false");
		sb.AppendLine(");");
	}

	private static void AppendStringLiteral (StringBuilder sb, string value)
	{
		sb.Append('"');
		foreach (var c in value)
		{
			switch (c)
			{
				case '\\': sb.Append(@"\\"); break;
				case '"': sb.Append("\\\""); break;
				case '\r': sb.Append(@"\r"); break;
				case '\n': sb.Append(@"\n"); break;
				case '\t': sb.Append(@"\t"); break;
				default: sb.Append(c); break;
			}
		}
		sb.Append('"');
	}

	/// <summary>
	/// Returns <see langword="true"/> if the type can be referenced by name from a
	/// top-level namespace inside the same assembly. A type is inaccessible when it
	/// (or any enclosing type) is declared <c>private</c>, <c>protected</c>, or
	/// <c>private protected</c>. Such types cannot be registered because the generated
	/// <c>typeof(...)</c> expression would not compile; they fall back to the runtime
	/// reflection path.
	/// </summary>
	private static bool IsAccessibleFromGeneratedCode (INamedTypeSymbol symbol)
	{
		for (var current = symbol; current is not null; current = current.ContainingType)
		{
			switch (current.DeclaredAccessibility)
			{
				case Accessibility.Public:
				case Accessibility.Internal:
				case Accessibility.ProtectedOrInternal:
					continue;
				default:
					return false;
			}
		}
		return true;
	}

}
