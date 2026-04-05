namespace StrictId;

/// <summary>
/// The shared surface of every StrictId value type — the three generic families
/// (<c>Id&lt;T&gt;</c>, <c>IdNumber&lt;T&gt;</c>, <c>IdString&lt;T&gt;</c>) and their
/// non-generic counterparts (<c>Id</c>, <c>IdNumber</c>, <c>IdString</c>).
/// </summary>
/// <remarks>
/// <para>
/// The interface is deliberately minimal. Each concrete StrictId family adds its own
/// family-specific helpers (<c>ToGuid</c>/<c>ToUlid</c> on the ULID family,
/// <c>ToUInt64</c>/<c>ToInt64</c> on the numeric family, and so on). Only the parse,
/// format, equality, and comparison surface is common enough to live on this interface.
/// </para>
/// <para>
/// Parsing comes from <see cref="ISpanParsable{TSelf}"/>; formatting from
/// <see cref="ISpanFormattable"/> (which inherits <see cref="IFormattable"/>) plus
/// <see cref="IUtf8SpanFormattable"/> for UTF-8 byte output. The only StrictId-specific
/// static on the shared surface is <see cref="IsValid"/>, which
/// <see cref="ISpanParsable{TSelf}"/> does not provide.
/// </para>
/// </remarks>
/// <typeparam name="TSelf">The implementing struct type.</typeparam>
public interface IStrictId<TSelf>
	: IEquatable<TSelf>,
		IComparable<TSelf>,
		ISpanParsable<TSelf>,
		ISpanFormattable,
		IUtf8SpanFormattable
	where TSelf : struct, IStrictId<TSelf>
{
	/// <summary>The empty (default) value for <typeparamref name="TSelf"/>.</summary>
	static abstract TSelf Empty { get; }

	/// <summary>
	/// Returns <see langword="true"/> if <paramref name="s"/> can be parsed as a
	/// <typeparamref name="TSelf"/>, otherwise <see langword="false"/>. Never throws
	/// (<see langword="null"/> returns <see langword="false"/>).
	/// </summary>
	/// <param name="s">The input to validate.</param>
	static abstract bool IsValid (string? s);

	/// <summary>
	/// <see langword="true"/> if this value is non-empty. For the ULID and numeric
	/// families this means the underlying storage is not zero; for <c>IdString&lt;T&gt;</c>
	/// it additionally means the backing string is not <see langword="null"/>.
	/// </summary>
	bool HasValue { get; }
}
