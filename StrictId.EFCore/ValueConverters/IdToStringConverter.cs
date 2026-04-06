using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace StrictId.EFCore.ValueConverters;

/// <summary>
/// EF Core value converter that stores <see cref="Id"/> as a fixed-length 26-character
/// lowercase Crockford base32 ULID string. Only the bare ULID is persisted; prefixes
/// are not stored.
/// </summary>
public class IdToStringConverter () : ValueConverter<Id, string>(
	id => id.ToString("B"),
	value => new Id(Ulid.Parse(value))
);

/// <summary>
/// EF Core value converter that stores <see cref="Id{T}"/> as a fixed-length
/// 26-character lowercase Crockford base32 ULID string. Only the bare ULID is
/// persisted; prefixes are not stored.
/// </summary>
/// <typeparam name="T">The entity type of the <see cref="Id{T}"/>.</typeparam>
public class IdToStringConverter<T> () : ValueConverter<Id<T>, string>(
	id => id.ToString("B"),
	value => new Id<T>(Ulid.Parse(value))
);
