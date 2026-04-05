using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace StrictId.EFCore.ValueConverters;

/// <summary>
/// EF Core value converter that stores <see cref="IdNumber"/> as a 64-bit signed
/// integer (<c>bigint</c>). The underlying storage is an unsigned 64-bit integer;
/// values above <see cref="long.MaxValue"/> cannot be represented in a signed
/// <c>bigint</c> column and will throw <see cref="OverflowException"/> on insert.
/// </summary>
public class IdNumberToLongConverter () : ValueConverter<IdNumber, long>(
	id => checked((long)id.Value),
	value => new IdNumber((ulong)value)
);

/// <summary>
/// EF Core value converter that stores <see cref="IdNumber{T}"/> as a 64-bit signed
/// integer (<c>bigint</c>). In v3 the prefix is a C# type-system concept carried by
/// <typeparamref name="T"/>; only the bare decimal value is persisted.
/// </summary>
/// <typeparam name="T">The phantom entity type of the <see cref="IdNumber{T}"/>.</typeparam>
public class IdNumberToLongConverter<T> () : ValueConverter<IdNumber<T>, long>(
	id => checked((long)id.Value),
	value => new IdNumber<T>((ulong)value)
);
