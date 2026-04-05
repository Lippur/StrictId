using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace StrictId.EFCore.ValueConverters;

/// <summary>
/// EF Core value converter that stores <see cref="Id"/> as a <see cref="Guid"/>. Note that this
/// makes the database representation visually differ from the canonical ULID string form.
/// </summary>
public class IdToGuidConverter () : ValueConverter<Id, Guid>(
	id => id.ToGuid(),
	value => new Id(value)
);

/// <summary>
/// EF Core value converter that stores <see cref="Id{T}"/> as a <see cref="Guid"/>. Note that this
/// makes the database representation visually differ from the canonical ULID string form.
/// </summary>
public class IdTypedToGuidConverter<T> () : ValueConverter<Id<T>, Guid>(
	id => id.ToGuid(),
	value => new Id<T>(value)
);
