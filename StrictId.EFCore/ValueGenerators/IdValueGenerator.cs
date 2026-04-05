using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace StrictId.EFCore.ValueGenerators;

/// <summary>
/// EF Core value generator that produces a fresh <see cref="Id"/> via <see cref="Id.NewId"/>
/// whenever a new entity is added.
/// </summary>
public class IdValueGenerator : ValueGenerator<Id>
{
	/// <inheritdoc />
	public override bool GeneratesTemporaryValues => false;

	/// <inheritdoc />
	public override Id Next (EntityEntry entry)
	{
		return Id.NewId();
	}
}

/// <summary>
/// Factory that creates <see cref="IdValueGenerator"/> instances for EF Core.
/// </summary>
public class IdValueGeneratorFactory : ValueGeneratorFactory
{
	/// <inheritdoc />
	public override ValueGenerator Create (IProperty property, ITypeBase typeBase)
	{
		return new IdValueGenerator();
	}
}
