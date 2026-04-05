namespace StrictId;

/// <summary>
/// Declares a prefix string for a StrictId entity type. The attribute is repeatable:
/// multiple prefixes may be registered for a single type, with exactly one marked as
/// <see cref="IsDefault"/> — the canonical one used on output. All registered prefixes
/// are accepted on parse, enabling legacy aliasing and short-form URLs without breaking
/// round-trip.
/// </summary>
/// <remarks>
/// A valid prefix matches <c>^[a-z][a-z0-9_]{0,62}$</c> — a lowercase ASCII letter
/// followed by up to 62 additional lowercase alphanumeric or underscore characters
/// (63 characters maximum). Grammar is validated by the StrictId analyzer at compile
/// time and by the runtime prefix metadata resolver on first access, as a defense in
/// depth.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = true)]
public sealed class IdPrefixAttribute (string prefix) : Attribute
{
	/// <summary>The prefix text, as declared.</summary>
	public string Prefix { get; } = prefix;

	/// <summary>
	/// <see langword="true"/> if this prefix is the canonical one for the type (the one
	/// used on output). When a type declares more than one <see cref="IdPrefixAttribute"/>,
	/// exactly one must be marked as default; when only one is present, it is implicitly
	/// the default regardless of this flag.
	/// </summary>
	public bool IsDefault { get; set; }
}
