// netstandard2.0 — the target required for Roslyn source generators — lacks several
// types that modern C# language features depend on. Shimmed here so the generator
// project can use `init`-only setters (via IsExternalInit) and collection literals
// (via CollectionsMarshal). These shims have no effect at runtime; they exist only so
// the compiler can resolve the well-known names.

// ReSharper disable once CheckNamespace
namespace System.Runtime.CompilerServices;

internal static class IsExternalInit;
