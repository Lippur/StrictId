// netstandard2.0 — the target required for Roslyn source generators — does not ship
// the well-known IsExternalInit type that C# 9+ requires to compile `init`-only
// setters (which are implicit on every positional record member in Descriptors.cs).
// Shimmed here so the compiler can resolve the name; the shim has no runtime effect.

// ReSharper disable once CheckNamespace
namespace System.Runtime.CompilerServices;

internal static class IsExternalInit;
