using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace StrictId.EFCore.ValueGenerators;

/// <summary>
/// EF Core value generator that produces a fresh <see cref="Guid{T}"/> via
/// <see cref="Guid{T}.NewId()"/> (UUIDv7) whenever a new entity is added. UUIDv7
/// is preferred over UUIDv4 for value generation because its time-sorted structure
/// is friendlier to clustered indexes.
/// </summary>
public class GuidTypedValueGenerator<T> : ValueGenerator<Guid<T>>
{
	/// <inheritdoc />
	public override bool GeneratesTemporaryValues => false;

	/// <inheritdoc />
	public override Guid<T> Next (EntityEntry entry)
	{
		return Guid<T>.NewId();
	}
}

/// <summary>
/// Factory that creates <see cref="GuidTypedValueGenerator{T}"/> instances for EF Core.
/// </summary>
public class GuidTypedValueGeneratorFactory<T> : ValueGeneratorFactory
{
	/// <inheritdoc />
	public override ValueGenerator Create (IProperty property, ITypeBase typeBase)
	{
		return new GuidTypedValueGenerator<T>();
	}
}
