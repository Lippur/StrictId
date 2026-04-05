namespace StrictId.AspNetCore.TypeConverters;

/// <summary>
/// <see cref="System.ComponentModel.TypeConverter"/> for the non-generic
/// <see cref="Id"/>. Registered globally via
/// <c>services.AddStrictIdTypeConverters()</c> so legacy binding paths that probe for a
/// <see cref="System.ComponentModel.TypeConverterAttribute"/> find a parser.
/// </summary>
public sealed class IdTypeConverter : StrictIdTypeConverter<Id>;

/// <summary>
/// Generic <see cref="System.ComponentModel.TypeConverter"/> for <see cref="Id{T}"/>.
/// One closed instantiation per entity type is registered via
/// <c>AddStrictIdTypeConverters</c>. Respects the entity's registered prefix list when
/// parsing — a request carrying a different entity's prefix is rejected with a verbose
/// <see cref="FormatException"/>.
/// </summary>
/// <typeparam name="T">The entity type the wrapped <see cref="Id{T}"/> belongs to.</typeparam>
public sealed class IdTypeConverter<T> : StrictIdTypeConverter<Id<T>>;
