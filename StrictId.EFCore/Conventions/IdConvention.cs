using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using StrictId.EFCore.ValueConverters;

namespace StrictId.EFCore.Conventions;

/// <summary>
/// EF Core convention that applies <see cref="IdToStringConverter{T}"/> to any
/// <see cref="Id{T}"/> property added to the model.
/// </summary>
public class IdConvention : IPropertyAddedConvention
{
	/// <inheritdoc />
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
