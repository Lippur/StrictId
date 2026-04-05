using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace StrictId.EFCore.ValueConverters;

/// <summary>
/// EF Core value converter that stores <see cref="IdString"/> as its underlying bare
/// suffix. In v3 the prefix is a C# type-system concept; only the bare string value is
/// persisted.
/// </summary>
public class IdStringToStringConverter () : ValueConverter<IdString, string>(
	id => id.Value,
	value => new IdString(value)
);

/// <summary>
/// EF Core value converter that stores <see cref="IdString{T}"/> as its underlying
/// bare suffix. In v3 the prefix is a C# type-system concept carried by
/// <typeparamref name="T"/>; only the bare string value is persisted.
/// </summary>
/// <typeparam name="T">The phantom entity type of the <see cref="IdString{T}"/>.</typeparam>
public class IdStringToStringConverter<T> () : ValueConverter<IdString<T>, string>(
	id => id.Value,
	value => new IdString<T>(value)
);
