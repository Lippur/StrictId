using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace StrictId.EFCore.ValueConverters;

/// <summary>
/// EF Core value converter that stores <see cref="Id"/> as a fixed-length 26-character
/// lowercase Crockford base32 ULID string — the bare form, without any prefix. In v3
/// the prefix is a C# type-system concept carried by the entity type; it never appears
/// in the database.
/// </summary>
public class IdToStringConverter () : ValueConverter<Id, string>(
	id => id.ToString("B"),
	value => new Id(Ulid.Parse(value))
);

/// <summary>
/// EF Core value converter that stores <see cref="Id{T}"/> as a fixed-length
/// 26-character lowercase Crockford base32 ULID string — the bare form, without any
/// prefix. In v3 the prefix is a C# type-system concept carried by <typeparamref name="T"/>;
/// it never appears in the database.
/// </summary>
/// <typeparam name="T">The phantom entity type of the <see cref="Id{T}"/>.</typeparam>
public class IdToStringConverter<T> () : ValueConverter<Id<T>, string>(
	id => id.ToString("B"),
	value => new Id<T>(Ulid.Parse(value))
);
