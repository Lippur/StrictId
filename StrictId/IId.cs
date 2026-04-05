namespace StrictId;

/// <summary>
/// Common surface shared by <see cref="Id"/> and <see cref="Id{T}"/>. Exposes the underlying
/// <see cref="Ulid"/> value plus formatting, comparison, and conversion helpers.
/// </summary>
public interface IId : IComparable, ISpanFormattable, IUtf8SpanFormattable
{
	/// <summary>The underlying <see cref="Ulid"/> value.</summary>
	Ulid Value { get; }

	/// <summary>
	/// <see langword="true"/> if <see cref="Value"/> is not <see cref="Ulid.Empty"/>.
	/// </summary>
	bool HasValue { get; }

	/// <summary>The 80-bit random component of <see cref="Value"/>.</summary>
	byte[] Random { get; }

	/// <summary>The timestamp component of <see cref="Value"/>.</summary>
	DateTimeOffset Time { get; }

	/// <summary>Converts this identifier to its equivalent <see cref="Guid"/> representation.</summary>
	Guid ToGuid ();

	/// <summary>Returns the canonical 26-character Crockford base32 ULID string.</summary>
	string ToString ();

	/// <summary>Returns a Base64 representation of the underlying 16 bytes.</summary>
	string ToBase64 ();

	/// <summary>Returns the underlying 16-byte representation.</summary>
	byte[] ToByteArray ();
}
