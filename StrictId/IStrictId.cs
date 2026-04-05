namespace StrictId;

/// <summary>
/// The shared surface of every StrictId value type — the three generic families
/// (<c>Id&lt;T&gt;</c>, <c>IdNumber&lt;T&gt;</c>, <c>IdString&lt;T&gt;</c>) and their
/// non-generic counterparts (<c>Id</c>, <c>IdNumber</c>, <c>IdString</c>).
/// </summary>
/// <remarks>
/// Each concrete StrictId family adds its own family-specific helpers
/// (<c>ToGuid</c>/<c>ToUlid</c> on the ULID family, <c>ToUInt64</c>/<c>ToInt64</c> on
/// the numeric family, and so on). Only the parse, format, equality, and comparison
/// surface is common enough to live on this interface.
/// </remarks>
/// <typeparam name="TSelf">The implementing struct type.</typeparam>
public interface IStrictId<TSelf>
	: IEquatable<TSelf>,
		IComparable<TSelf>,
		ISpanParsable<TSelf>,
		ISpanFormattable
	where TSelf : struct, IStrictId<TSelf>
{
	/// <summary>The empty (default) value for <typeparamref name="TSelf"/>.</summary>
	static abstract TSelf Empty { get; }

	/// <summary>Parses <paramref name="s"/> into a <typeparamref name="TSelf"/>.</summary>
	/// <param name="s">The input to parse.</param>
	/// <exception cref="FormatException">The input could not be parsed.</exception>
	static abstract TSelf Parse (string s);

	/// <summary>Attempts to parse <paramref name="s"/> into a <typeparamref name="TSelf"/>.</summary>
	/// <param name="s">The input to parse, or <see langword="null"/>.</param>
	/// <param name="result">
	/// On success, the parsed value. On failure, the default value of
	/// <typeparamref name="TSelf"/>.
	/// </param>
	/// <returns><see langword="true"/> if parsing succeeded; otherwise <see langword="false"/>.</returns>
	static abstract bool TryParse (string? s, out TSelf result);

	/// <summary>
	/// <see langword="true"/> if this value is non-empty. For the ULID and numeric
	/// families this means the underlying storage is not zero; for <c>IdString&lt;T&gt;</c>
	/// it additionally means the backing string is not <see langword="null"/>.
	/// </summary>
	bool HasValue { get; }
}
