using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace StrictId.EFCore.ValueGenerators;

/// <summary>
/// EF Core value generator that produces a fresh <see cref="Id{T}"/> via
/// <see cref="Id{T}.NewId()"/> whenever a new entity is added.
/// </summary>
public class IdTypedValueGenerator<T> : ValueGenerator<Id<T>>
{
	/// <inheritdoc />
	public override bool GeneratesTemporaryValues => false;

	/// <inheritdoc />
	public override Id<T> Next (EntityEntry entry)
	{
		return Id<T>.NewId();
	}
}

/// <summary>
/// Factory that creates <see cref="IdTypedValueGenerator{T}"/> instances for EF Core.
/// </summary>
public class IdTypedValueGeneratorFactory<T> : ValueGeneratorFactory
{
	/// <inheritdoc />
	public override ValueGenerator Create (IProperty property, ITypeBase typeBase)
	{
		return new IdTypedValueGenerator<T>();
	}
}
