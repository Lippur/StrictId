using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace StrictId.EFCore.ValueConverters;

/// <summary>
/// EF Core value converter that stores <see cref="Id"/> as a <see cref="Guid"/>. The
/// GUID encoding preserves the full 128-bit ULID value but reorders it, so the string
/// form of the column will visually differ from the canonical lowercase ULID; pick this
/// converter only when the target column must be <c>uniqueidentifier</c> (for example,
/// on SQL Server schemas that predate StrictId adoption).
/// </summary>
public class IdToGuidConverter () : ValueConverter<Id, Guid>(
	id => id.Value.ToGuid(),
	value => new Id(value)
);

/// <summary>
/// EF Core value converter that stores <see cref="Id{T}"/> as a <see cref="Guid"/>.
/// See <see cref="IdToGuidConverter"/> for caveats.
/// </summary>
/// <typeparam name="T">The phantom entity type of the <see cref="Id{T}"/>.</typeparam>
public class IdTypedToGuidConverter<T> () : ValueConverter<Id<T>, Guid>(
	id => id.Value.ToGuid(),
	value => new Id<T>(value)
);
