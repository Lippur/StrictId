using StrictId.Test.Aot;

// StrictId AOT smoke test harness.
//
// Each check exercises a single dimension of StrictId's surface. The harness runs
// them sequentially, reports per-check status, and exits with a non-zero code on the
// first failure so CI wrappers (or a human running the native binary) can tell
// success from failure at a glance.
//
// The harness is deliberately minimal — no test framework, no reflection-driven
// discovery, no async entry points. That keeps the AOT publish diff clean and makes
// any StrictId-side reflection the only candidate source of IL2xxx / IL3050 warnings.

var failures = 0;
var checksRun = 0;

Run("Id<T>          round-trip",                      Checks.IdOfT_RoundTripsCanonicalForm);
Run("Id<T>          format specifiers",               Checks.IdOfT_FormatSpecifiers);
Run("IdNumber<T>    round-trip",                      Checks.IdNumberOfT_RoundTripsCanonicalForm);
Run("IdString<T>    round-trip",                      Checks.IdStringOfT_RoundTripsCanonicalForm);
Run("Id<T>          cross-type equality",             Checks.CrossTypeEquality_NeverHolds);
Run("Id<T>          generation burst",                Checks.IdOfT_NewIdBurst_IsUniqueAndTimeOrdered);
Run("JSON           source-gen DTO round-trip",       Checks.StrictIdDto_JsonRoundTrip_UsesSourceGeneratedContext);

Console.WriteLine();
Console.WriteLine($"{checksRun - failures}/{checksRun} checks passed.");
return failures == 0 ? 0 : 1;

void Run (string name, Action check)
{
	checksRun++;
	try
	{
		check();
		Console.WriteLine($"  PASS  {name}");
	}
	catch (Exception ex)
	{
		failures++;
		Console.WriteLine($"  FAIL  {name}");
		Console.WriteLine($"        {ex.GetType().Name}: {ex.Message}");
	}
}
