namespace StrictId;

/// <summary>
/// Declares the canonical separator character for StrictIds on the decorated entity
/// type or — when applied at the assembly level — sets the default separator for every
/// entity type in the assembly that does not declare its own <c>[IdSeparator]</c>.
/// </summary>
/// <remarks>
/// <para>
/// Resolution order (first match wins):
/// <list type="number">
///   <item>Type-level <c>[IdSeparator]</c> on the entity type itself.</item>
///   <item>Inherited <c>[IdSeparator]</c> from a base type.</item>
///   <item>Assembly-level <c>[assembly: IdSeparator(...)]</c> on the entity type's declaring assembly.</item>
///   <item>The built-in default: <see cref="IdSeparator.Underscore"/>.</item>
/// </list>
/// </para>
/// <para>
/// Parsing is tolerant of any of the four <see cref="IdSeparator"/> values regardless
/// of which one is declared here, so changing a type's canonical separator does not
/// invalidate historically-emitted IDs. The declared separator is used only on
/// output.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Assembly, AllowMultiple = false, Inherited = true)]
public sealed class IdSeparatorAttribute (IdSeparator separator) : Attribute
{
	/// <summary>The separator declared for the decorated type or assembly.</summary>
	public IdSeparator Separator { get; } = separator;
}
