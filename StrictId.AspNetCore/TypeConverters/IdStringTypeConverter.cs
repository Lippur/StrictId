namespace StrictId.AspNetCore.TypeConverters;

/// <summary>
/// <see cref="System.ComponentModel.TypeConverter"/> for the non-generic
/// <see cref="IdString"/>. Registered globally via
/// <c>services.AddStrictIdTypeConverters()</c>.
/// </summary>
public sealed class IdStringTypeConverter : StrictIdTypeConverter<IdString>;

/// <summary>
/// Generic <see cref="System.ComponentModel.TypeConverter"/> for
/// <see cref="IdString{T}"/>. Honours the entity's registered prefix and
/// <see cref="IdStringAttribute"/> validation rules during parsing.
/// </summary>
/// <typeparam name="T">The entity type the wrapped <see cref="IdString{T}"/> belongs to.</typeparam>
public sealed class IdStringTypeConverter<T> : StrictIdTypeConverter<IdString<T>>;
