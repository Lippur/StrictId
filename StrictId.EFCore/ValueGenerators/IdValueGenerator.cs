using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace StrictId.EFCore.ValueGenerators;

public class IdValueGenerator : ValueGenerator<Id>
{
	public override bool GeneratesTemporaryValues => false;

	public override Id Next (EntityEntry entry)
	{
		return Id.NewId();
	}
}

public class IdValueGeneratorFactory : ValueGeneratorFactory
{
	public override ValueGenerator Create (IProperty property, ITypeBase typeBase)
	{
		return new IdValueGenerator();
	}
}