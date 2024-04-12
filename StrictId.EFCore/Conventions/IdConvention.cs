using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using StrictId.EFCore.ValueConverters;

namespace StrictId.EFCore.Conventions;

public class IdConvention : IPropertyAddedConvention
{
	public void ProcessPropertyAdded (
		IConventionPropertyBuilder propertyBuilder,
		IConventionContext<IConventionPropertyBuilder> context
	)
	{
		var propertyType = propertyBuilder.Metadata.ClrType;
		if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Id<>))
		{
			propertyBuilder.HasConversion(
				(ValueConverter?)Activator.CreateInstance(
					typeof(IdToStringConverter<>).MakeGenericType(propertyType.GetGenericArguments().First())
				)
			);
		}
	}
}