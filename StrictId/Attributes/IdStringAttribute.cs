namespace StrictId;

/// <summary>
/// Configures validation rules for <c>IdString&lt;T&gt;</c> on the decorated entity
/// type. Controls the maximum suffix length, the allowed character set, and case
/// sensitivity.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
public sealed class IdStringAttribute : Attribute
{
	/// <summary>Maximum length of the suffix, in characters. Defaults to <c>255</c>.</summary>
	public int MaxLength { get; set; } = 255;

	/// <summary>
	/// Restricts the characters permitted in the suffix. Defaults to
	/// <see cref="IdStringCharSet.AlphanumericDashUnderscore"/>.
	/// </summary>
	public IdStringCharSet CharSet { get; set; } = IdStringCharSet.AlphanumericDashUnderscore;

	/// <summary>
	/// <see langword="true"/> to treat differently-cased suffixes as equal. Defaults to
	/// <see langword="false"/> to match the behaviour of most real-world opaque string
	/// IDs (for example Stripe's <c>cus_...</c>).
	/// </summary>
	public bool IgnoreCase { get; set; }
}
