using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using StrictId.Generators.Diagnostics;

namespace StrictId.Generators;

/// <summary>
/// Roslyn incremental source generator that emits <see cref="System.Runtime.CompilerServices.ModuleInitializerAttribute"/>
/// calls populating <c>StrictId.StrictIdRegistry</c> with the prefix and string-option
/// metadata for every user-code type decorated with <c>[IdPrefix]</c>, <c>[IdString]</c>,
/// or an inherited combination of the two.
/// </summary>
/// <remarks>
/// <para>
/// The goal is to eliminate the runtime reflection walk performed by
/// <c>StrictIdMetadataResolver</c> for every closed generic <c>Id&lt;T&gt;</c>,
/// <c>IdNumber&lt;T&gt;</c>, or <c>IdString&lt;T&gt;</c> visible at compile time. Types
/// that the generator did not see (for example, because they live in an assembly that
/// was not part of the generator's compilation input) still work correctly via the
/// reflection fallback path.
/// </para>
/// <para>
/// The generator scope is intentionally limited to types that directly declare one of
/// the StrictId attributes. Types that inherit an attribute from a base class without
/// redeclaring it are not captured here; they fall through to the reflection path at
/// runtime, which walks the hierarchy correctly. Phase 9 will add analyzers that warn
/// on patterns the generator cannot help with.
/// </para>
/// </remarks>
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
			.Combine(efCoreAvailable);

		context.RegisterSourceOutput(combined, static (sourceContext, payload) =>
		{
			var (((prefixes, strings), isEnabled), efAvailable) = payload;
			Emit(sourceContext, prefixes, strings, isEnabled, efAvailable);
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
			return new PrefixDescriptor(
				FullyQualifiedName: string.Empty,
				EscapedIdentifier: string.Empty,
				Prefixes: EquatableArray<PrefixDeclaration>.Empty,
				SeparatorEnumMember: "Underscore",
				Diagnostics: EquatableArray<DiagnosticData>.Empty);
		}

		var diagnostics = ImmutableArray.CreateBuilder<DiagnosticData>();
		var declarations = ImmutableArray.CreateBuilder<PrefixDeclaration>();

		foreach (var attrData in syntaxContext.Attributes)
		{
			cancellationToken.ThrowIfCancellationRequested();

			if (attrData.ConstructorArguments.Length == 0) continue;

			var prefix = attrData.ConstructorArguments[0].Value as string;
			var isDefault = false;
			foreach (var named in attrData.NamedArguments)
			{
				if (named.Key == "IsDefault" && named.Value.Value is bool b)
				{
					isDefault = b;
					break;
				}
			}

			if (prefix is null) continue;

			if (!TryValidatePrefix(prefix, out var reason))
			{
				diagnostics.Add(BuildDiagnostic(
					DiagnosticDescriptors.InvalidPrefixGrammar,
					attrData,
					target,
					prefix,
					target.ToDisplayString(),
					reason));
				continue;
			}

			declarations.Add(new PrefixDeclaration(prefix, isDefault));
		}

		ValidateCardinality(declarations, target, syntaxContext.Attributes, diagnostics);

		// Locate an [IdSeparator] on the target, if any. The attribute lives on the same
		// symbol but is discovered via GetAttributes() since our provider keyed on
		// [IdPrefix]. No inheritance walk here — the runtime fallback handles inherited
		// separators on types the generator did not see directly.
		var separator = ReadSeparator(target);

		return new PrefixDescriptor(
			FullyQualifiedName: target.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
			EscapedIdentifier: BuildSanitisedIdentifier(target),
			Prefixes: new EquatableArray<PrefixDeclaration>(declarations.ToImmutable()),
			SeparatorEnumMember: separator,
			Diagnostics: new EquatableArray<DiagnosticData>(diagnostics.ToImmutable()));
	}

	private static bool TryValidatePrefix (string prefix, out string reason)
	{
		if (prefix.Length == 0)
		{
			reason = "prefix is empty";
			return false;
		}
		if (prefix.Length > 63)
		{
			reason = $"prefix is {prefix.Length} characters long (max 63)";
			return false;
		}
		var first = prefix[0];
		if (first is < 'a' or > 'z')
		{
			reason = $"first character '{first}' is not a lowercase ASCII letter";
			return false;
		}
		for (var i = 1; i < prefix.Length; i++)
		{
			var c = prefix[i];
			if (c is >= 'a' and <= 'z' or >= '0' and <= '9' or '_') continue;
			reason = $"contains '{c}' at position {i}";
			return false;
		}
		reason = string.Empty;
		return true;
	}

	private static void ValidateCardinality (
		ImmutableArray<PrefixDeclaration>.Builder declarations,
		INamedTypeSymbol target,
		ImmutableArray<AttributeData> attributes,
		ImmutableArray<DiagnosticData>.Builder diagnostics)
	{
		// Duplicate check.
		var seen = new HashSet<string>(StringComparer.Ordinal);
		for (var i = declarations.Count - 1; i >= 0; i--)
		{
			if (!seen.Add(declarations[i].Prefix))
			{
				diagnostics.Add(BuildDiagnostic(
					DiagnosticDescriptors.DuplicatePrefix,
					attributes[0],
					target,
					declarations[i].Prefix,
					target.ToDisplayString()));
				declarations.RemoveAt(i);
			}
		}

		if (declarations.Count <= 1) return;

		var defaults = 0;
		foreach (var d in declarations)
			if (d.IsDefault)
				defaults++;

		if (defaults == 0)
		{
			diagnostics.Add(BuildDiagnostic(
				DiagnosticDescriptors.NoDefaultPrefix,
				attributes[0],
				target,
				target.ToDisplayString(),
				declarations.Count.ToString()));
		}
		else if (defaults > 1)
		{
			diagnostics.Add(BuildDiagnostic(
				DiagnosticDescriptors.MultipleDefaultPrefixes,
				attributes[0],
				target,
				target.ToDisplayString(),
				defaults.ToString()));
		}
	}

	private static string ReadSeparator (INamedTypeSymbol target)
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
				EscapedIdentifier: string.Empty,
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
						_ => "Any",
					};
					break;
				case "IgnoreCase" when named.Value.Value is bool ic:
					ignoreCase = ic;
					break;
			}
		}

		return new StringOptionsDescriptor(
			FullyQualifiedName: target.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
			EscapedIdentifier: BuildSanitisedIdentifier(target),
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
		bool efCoreAvailable)
	{
		// Always report diagnostics even when code emission is disabled — bad attributes
		// should still surface in the IDE so users can fix them before flipping the
		// generator back on.
		foreach (var descriptor in prefixDescriptors)
		{
			foreach (var diag in descriptor.Diagnostics)
			{
				context.ReportDiagnostic(RehydrateDiagnostic(diag));
			}
		}

		if (!enabled) return;
		if (prefixDescriptors.IsEmpty && stringDescriptors.IsEmpty) return;

		var source = BuildSource(prefixDescriptors, stringDescriptors, efCoreAvailable);
		context.AddSource(GeneratedFileHint, source);
	}

	private static string BuildSource (
		ImmutableArray<PrefixDescriptor> prefixDescriptors,
		ImmutableArray<StringOptionsDescriptor> stringDescriptors,
		bool efCoreAvailable)
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

		// Build the set of valid (accessible, diagnostic-free) entity type names so we
		// can emit JSON + EF converter registrations for each one. Types with empty
		// FullyQualifiedName were filtered during extraction (inaccessible); types with
		// diagnostics are malformed and are skipped here to avoid generating broken code.
		var validEntityTypes = ImmutableArray.CreateBuilder<string>();

		foreach (var descriptor in prefixDescriptors)
		{
			if (descriptor.FullyQualifiedName.Length == 0) continue;
			if (!descriptor.Diagnostics.IsEmpty) continue;
			EmitPrefixRegistration(sb, descriptor);
			validEntityTypes.Add(descriptor.FullyQualifiedName);
		}

		foreach (var descriptor in stringDescriptors)
		{
			if (descriptor.FullyQualifiedName.Length == 0) continue;
			EmitStringRegistration(sb, descriptor);
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
	}

	private static void EmitPrefixRegistration (StringBuilder sb, PrefixDescriptor descriptor)
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
		sb.Append(descriptor.SeparatorEnumMember);
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

	private static string BuildSanitisedIdentifier (INamedTypeSymbol symbol)
	{
		var display = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
		var sb = new StringBuilder(display.Length);
		foreach (var c in display)
		{
			if (char.IsLetterOrDigit(c) || c == '_') sb.Append(c);
			else sb.Append('_');
		}
		return sb.ToString();
	}

	// ═════ Diagnostic bridging ═══════════════════════════════════════════════

	private static DiagnosticData BuildDiagnostic (
		DiagnosticDescriptor descriptor,
		AttributeData attr,
		INamedTypeSymbol target,
		params string[] messageArgs)
	{
		_ = attr;    // AttributeData is not equatable across generator runs. Phase 9
		_ = target;  // analyzers will add rich locations; the scaffold reports at
		//             Location.None so the cache remains stable.
		var message = string.Format(descriptor.MessageFormat.ToString(), messageArgs);
		return new DiagnosticData(
			descriptor.Id,
			descriptor.Title.ToString(),
			message,
			FileHint: string.Empty,
			LineSpanStart: 0,
			LineSpanLength: 0);
	}

	private static Diagnostic RehydrateDiagnostic (DiagnosticData data)
	{
		var descriptor = data.Id switch
		{
			"STRID101" => DiagnosticDescriptors.InvalidPrefixGrammar,
			"STRID102" => DiagnosticDescriptors.DuplicatePrefix,
			"STRID103" => DiagnosticDescriptors.NoDefaultPrefix,
			"STRID104" => DiagnosticDescriptors.MultipleDefaultPrefixes,
			_ => throw new InvalidOperationException($"Unknown diagnostic id {data.Id}."),
		};

		return Diagnostic.Create(descriptor, Location.None, data.Message);
	}
}
