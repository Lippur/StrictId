namespace StrictId.AspNetCore.TypeConverters;

/// <summary>
/// <see cref="System.ComponentModel.TypeConverter"/> for the non-generic
/// <see cref="IdNumber"/>. Registered globally via
/// <c>services.AddStrictIdTypeConverters()</c>.
/// </summary>
public sealed class IdNumberTypeConverter : StrictIdTypeConverter<IdNumber>;

/// <summary>
/// Generic <see cref="System.ComponentModel.TypeConverter"/> for
/// <see cref="IdNumber{T}"/>. Honours the entity's registered prefix during parsing.
/// </summary>
/// <typeparam name="T">The entity type the wrapped <see cref="IdNumber{T}"/> belongs to.</typeparam>
public sealed class IdNumberTypeConverter<T> : StrictIdTypeConverter<IdNumber<T>>;
