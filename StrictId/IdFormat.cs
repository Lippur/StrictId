namespace StrictId;

/// <summary>
/// Controls how StrictId values are parsed. Pass an instance to the
/// <see cref="IFormatProvider"/> parameter of <c>Parse</c> or <c>TryParse</c>
/// to enforce stricter parsing rules than the default (accept anything structurally valid).
/// </summary>
/// <example>
/// <code>
/// // Succeeds — prefixed input accepted
/// var id = Id&lt;User&gt;.Parse("user_01hv9af3qa4t121hcz873m0bkk", IdFormat.RequirePrefix);
///
/// // Throws FormatException — bare ULID rejected in prefix-required mode
/// var id = Id&lt;User&gt;.Parse("01hv9af3qa4t121hcz873m0bkk", IdFormat.RequirePrefix);
///
/// // TryParse returns false
/// Id&lt;User&gt;.TryParse("01hv9af3qa4t121hcz873m0bkk", IdFormat.RequirePrefix, out _); // false
/// </code>
/// </example>
public sealed class IdFormat : IFormatProvider
{
	/// <summary>
	/// A parse format that requires the input to include a recognized prefix and separator.
	/// Bare values (without a prefix) are rejected even when they are structurally valid.
	/// </summary>
	public static readonly IdFormat RequirePrefix = new();

	private IdFormat () { }

	/// <inheritdoc />
	object? IFormatProvider.GetFormat (Type? formatType)
		=> formatType == typeof(IdFormat) ? this : null;

	/// <summary>
	/// Returns <see langword="true"/> if <paramref name="provider"/> requires a prefix
	/// on parse input.
	/// </summary>
	internal static bool IsPrefixRequired (IFormatProvider? provider) => provider is IdFormat;
}
