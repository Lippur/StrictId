using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace StrictId.EFCore.ValueGenerators;

public class IdTypedValueGenerator<T> : ValueGenerator<Id<T>>
{
	public override bool GeneratesTemporaryValues => false;

	public override Id<T> Next (EntityEntry entry)
	{
		return Id<T>.NewId();
	}
}

public class IdTypedValueGeneratorFactory<T> : ValueGeneratorFactory
{
	public override ValueGenerator Create (IProperty property, ITypeBase typeBase)
	{
		return new IdTypedValueGenerator<T>();
	}
}