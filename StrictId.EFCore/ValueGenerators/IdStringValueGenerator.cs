using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace StrictId.EFCore.ValueGenerators;

public class IdStringValueGenerator : ValueGenerator<string>
{
	public override bool GeneratesTemporaryValues => false;

	public override string Next (EntityEntry entry)
	{
		return Id.NewId().ToString();
	}
}

public class IdStringValueGeneratorFactory : ValueGeneratorFactory
{
	public override ValueGenerator Create (IProperty property, ITypeBase typeBase)
	{
		return new IdStringValueGenerator();
	}
}