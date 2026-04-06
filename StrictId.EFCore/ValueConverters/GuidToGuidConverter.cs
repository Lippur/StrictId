using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace StrictId.EFCore.ValueConverters;

/// <summary>
/// EF Core value converter that stores <see cref="Guid{T}"/> as a native
/// <see cref="Guid"/>. The database column stays <c>uniqueidentifier</c> (SQL Server)
/// or <c>uuid</c> (PostgreSQL). No string conversion, no prefix in the database.
/// </summary>
/// <typeparam name="T">The entity type of the <see cref="Guid{T}"/>.</typeparam>
public class GuidToGuidConverter<T> () : ValueConverter<Guid<T>, Guid>(
	id => id.Value,
	value => new Guid<T>(value)
);
