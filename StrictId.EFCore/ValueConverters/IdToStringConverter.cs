using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace StrictId.EFCore.ValueConverters;

/// <summary>
/// EF Core value converter that stores <see cref="Id"/> as its canonical 26-character
/// Crockford base32 ULID string. This preserves insertion order in clustered indexes.
/// </summary>
public class IdToStringConverter () : ValueConverter<Id, string>(
	id => id.ToString(),
	value => new Id(value)
);

/// <summary>
/// EF Core value converter that stores <see cref="Id{T}"/> as its canonical 26-character
/// Crockford base32 ULID string. This preserves insertion order in clustered indexes.
/// </summary>
public class IdToStringConverter<T> () : ValueConverter<Id<T>, string>(
	id => id.ToString(),
	value => new Id<T>(value)
);
