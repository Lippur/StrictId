using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace StrictId.Generators.Test;

/// <summary>
/// Harness that feeds a single C# source snippet through the <see cref="StrictIdGenerator"/>
/// and returns the generated files plus any diagnostics, so tests can snapshot the
/// generator's output without depending on the full <c>Microsoft.CodeAnalysis.Testing</c>
/// package family.
/// </summary>
internal static class GeneratorRunner
{
	private static readonly MetadataReference[] CoreReferences = BuildCoreReferences();
	private static readonly MetadataReference[] EfCoreAugmentedReferences = BuildEfCoreAugmentedReferences();

	/// <summary>
	/// Runs the generator against a source snippet using only the StrictId core reference.
	/// The EF Core conditional-emission path stays disabled in this configuration.
	/// </summary>
	public static GeneratorRunResult Run (string source)
		=> RunCore(source, CoreReferences);

	/// <summary>
	/// Runs the generator against a source snippet with both StrictId and
	/// StrictId.EFCore referenced, which activates the EF Core conditional-emission path.
	/// </summary>
	public static GeneratorRunResult RunWithEfCore (string source)
		=> RunCore(source, EfCoreAugmentedReferences);

	private static GeneratorRunResult RunCore (string source, MetadataReference[] references)
	{
		var syntaxTree = CSharpSyntaxTree.ParseText(source);
		var compilation = CSharpCompilation.Create(
			assemblyName: "StrictId.GeneratorTests.Sample",
			syntaxTrees: [syntaxTree],
			references: references,
			options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		var generator = new StrictIdGenerator();
		var driver = CSharpGeneratorDriver.Create(generator);
		var result = driver.RunGenerators(compilation).GetRunResult();

		var primary = result.Results[0];
		var sources = primary.GeneratedSources
			.Select(gs => gs.SourceText.ToString())
			.ToImmutableArray();

		return new GeneratorRunResult(primary.Diagnostics, sources);
	}

	private static MetadataReference[] BuildCoreReferences ()
	{
		// Always include the core BCL, StrictId, and any currently-loaded system
		// assembly so the test compilation has enough surface to resolve
		// IdPrefixAttribute and friends.
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

	private static MetadataReference[] BuildEfCoreAugmentedReferences ()
	{
		var baseRefs = BuildCoreReferences();

		// Locate the StrictId.EFCore assembly next to StrictId. The test project
		// references StrictId.EFCore for this purpose — if it ever doesn't, the file
		// resolution below throws and the tests surface a clear error.
		var strictIdLocation = typeof(IdPrefixAttribute).Assembly.Location;
		var strictIdDir = System.IO.Path.GetDirectoryName(strictIdLocation)!;
		var efCoreLocation = System.IO.Path.Combine(strictIdDir, "StrictId.EFCore.dll");

		if (!System.IO.File.Exists(efCoreLocation))
		{
			throw new FileNotFoundException(
				$"StrictId.EFCore.dll not found next to StrictId.dll at {efCoreLocation}. " +
				"The generator test project must reference StrictId.EFCore so that the " +
				"EF-conditional emission path can be exercised from the harness.");
		}

		var efCoreRef = MetadataReference.CreateFromFile(efCoreLocation);

		// EF Core convention types live in Microsoft.EntityFrameworkCore.dll, which is
		// a transitive dep of StrictId.EFCore. Include the whole directory of DLLs to
		// satisfy the compilation without guessing individual names.
		var extraRefs = System.IO.Directory.GetFiles(strictIdDir, "Microsoft.EntityFrameworkCore*.dll")
			.Select(path => (MetadataReference)MetadataReference.CreateFromFile(path));

		return baseRefs.Concat([efCoreRef]).Concat(extraRefs).ToArray();
	}
}

internal sealed record GeneratorRunResult (
	ImmutableArray<Diagnostic> Diagnostics,
	ImmutableArray<string> GeneratedSources
);
