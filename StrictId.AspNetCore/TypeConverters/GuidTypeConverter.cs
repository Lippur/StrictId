namespace StrictId.AspNetCore.TypeConverters;

/// <summary>
/// Generic <see cref="System.ComponentModel.TypeConverter"/> for <see cref="Guid{T}"/>.
/// One closed instantiation per entity type is registered via
/// <c>AddStrictIdTypeConverters</c>. Respects the entity's registered prefix list when
/// parsing — a request carrying a different entity's prefix is rejected with a verbose
/// <see cref="FormatException"/>.
/// </summary>
/// <typeparam name="T">The entity type the wrapped <see cref="Guid{T}"/> belongs to.</typeparam>
public sealed class GuidTypeConverter<T> : StrictIdTypeConverter<Guid<T>>;
