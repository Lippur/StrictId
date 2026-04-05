using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StrictId.Generators.Test;

/// <summary>
/// Harness that runs one or more <see cref="DiagnosticAnalyzer"/>s against a C# source
/// snippet and returns the analyzer-emitted diagnostics. Mirrors
/// <see cref="GeneratorRunner"/>'s approach of using raw Roslyn APIs directly rather
/// than the heavier Microsoft.CodeAnalysis.Testing package, so the test project keeps
/// a minimal dependency surface.
/// </summary>
internal static class AnalyzerRunner
{
	private static readonly MetadataReference[] CoreReferences = BuildCoreReferences();

	/// <summary>
	/// Runs <paramref name="analyzer"/> against <paramref name="source"/> and returns
	/// every diagnostic it emitted, plus any compilation diagnostics with severity
	/// error or higher (so tests can assert the compilation itself is sound).
	/// </summary>
	public static async Task<AnalyzerRunResult> RunAsync (DiagnosticAnalyzer analyzer, string source)
	{
		var syntaxTree = CSharpSyntaxTree.ParseText(source);
		var compilation = CSharpCompilation.Create(
			assemblyName: "StrictId.AnalyzerTests.Sample",
			syntaxTrees: [syntaxTree],
			references: CoreReferences,
			options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		var withAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create(analyzer));
		var analyzerDiagnostics = await withAnalyzers.GetAnalyzerDiagnosticsAsync();
		var compilationErrors = compilation.GetDiagnostics()
			.Where(d => d.Severity == DiagnosticSeverity.Error)
			.ToImmutableArray();

		return new AnalyzerRunResult(analyzerDiagnostics, compilationErrors);
	}

	private static MetadataReference[] BuildCoreReferences ()
	{
		var strictIdLocation = typeof(IdPrefixAttribute).Assembly.Location;
		var objectLocation = typeof(object).Assembly.Location;
		var runtimeDir = System.IO.Path.GetDirectoryName(objectLocation)!;

		return
		[
			MetadataReference.CreateFromFile(strictIdLocation),
			MetadataReference.CreateFromFile(objectLocation),
			MetadataReference.CreateFromFile(System.IO.Path.Combine(runtimeDir, "System.Runtime.dll")),
			MetadataReference.CreateFromFile(System.IO.Path.Combine(runtimeDir, "netstandard.dll")),
		];
	}
}

internal sealed record AnalyzerRunResult (
	ImmutableArray<Diagnostic> Diagnostics,
	ImmutableArray<Diagnostic> CompilationErrors
);
