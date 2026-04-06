using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace StrictId.EFCore.ValueConverters;

/// <summary>
/// EF Core value converter that stores <see cref="IdString"/> as its underlying bare
/// suffix. Prefixes are not stored.
/// </summary>
public class IdStringToStringConverter () : ValueConverter<IdString, string>(
	id => id.Value,
	value => new IdString(value)
);

/// <summary>
/// EF Core value converter that stores <see cref="IdString{T}"/> as its underlying
/// bare suffix. Prefixes are not stored.
/// </summary>
/// <typeparam name="T">The entity type of the <see cref="IdString{T}"/>.</typeparam>
public class IdStringToStringConverter<T> () : ValueConverter<IdString<T>, string>(
	id => id.Value,
	value => new IdString<T>(value)
);
