namespace StrictId;

/// <summary>
/// Declares the canonical separator character for StrictIds on the decorated entity
/// type. When not specified (and not inherited from a base type), the default is
/// <see cref="IdSeparator.Underscore"/>.
/// </summary>
/// <remarks>
/// Parsing is tolerant of any of the four <see cref="IdSeparator"/> values regardless
/// of which one is declared here, so changing a type's canonical separator does not
/// invalidate historically-emitted IDs. The declared separator is used only on
/// output.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
public sealed class IdSeparatorAttribute (IdSeparator separator) : Attribute
{
	/// <summary>The separator declared for the decorated type.</summary>
	public IdSeparator Separator { get; } = separator;
}
