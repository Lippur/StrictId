namespace StrictId;

public interface IId : IComparable, ISpanFormattable, IUtf8SpanFormattable
{
	Ulid Value { get; }
	bool HasValue { get; }
	byte[] Random { get; }
	DateTimeOffset Time { get; }
	Guid ToGuid ();
	string ToString ();
	string ToBase64 ();
	byte[] ToByteArray ();
}