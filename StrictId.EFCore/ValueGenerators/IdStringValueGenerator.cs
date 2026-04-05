using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace StrictId.EFCore.ValueGenerators;

/// <summary>
/// EF Core value generator that produces a fresh ULID string via <see cref="Id.NewId"/>
/// whenever a new entity is added. Useful for plain <see cref="string"/> columns that should
/// receive StrictId-compatible values.
/// </summary>
public class IdStringValueGenerator : ValueGenerator<string>
{
	/// <inheritdoc />
	public override bool GeneratesTemporaryValues => false;

	/// <inheritdoc />
	public override string Next (EntityEntry entry)
	{
		return Id.NewId().ToString();
	}
}

/// <summary>
/// Factory that creates <see cref="IdStringValueGenerator"/> instances for EF Core.
/// </summary>
public class IdStringValueGeneratorFactory : ValueGeneratorFactory
{
	/// <inheritdoc />
	public override ValueGenerator Create (IProperty property, ITypeBase typeBase)
	{
		return new IdStringValueGenerator();
	}
}
