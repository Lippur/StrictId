using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StrictId.EFCore.Conventions;
using StrictId.EFCore.ValueConverters;
using StrictId.EFCore.ValueGenerators;

namespace StrictId.EFCore;

public static class EfCoreExtensions
{
	public static ModelConfigurationBuilder ConfigureStrictId (this ModelConfigurationBuilder builder)
	{
		builder.Properties<Id>()
			.HaveConversion<IdToStringConverter>()
			.HaveMaxLength(26)
			.AreFixedLength();
		
		builder.Properties(typeof(Id<>))
			.HaveConversion(typeof(IdToStringConverter<>))
			.HaveMaxLength(26)
			.AreFixedLength();
		
		builder.Conventions.Add(_ => new IdConvention());

		return builder;
	}

	public static PropertyBuilder<Id> HasIdValueGenerator<T> (this PropertyBuilder<Id> builder)
	{
		builder
			.HasValueGenerator<IdValueGenerator>()
			.HasValueGeneratorFactory<IdValueGeneratorFactory>();

		return builder;
	}
	
	public static PropertyBuilder<Id<T>> HasStrictIdValueGenerator<T> (this PropertyBuilder<Id<T>> builder)
	{
		builder
			.HasValueGenerator<IdTypedValueGenerator<T>>()
			.HasValueGeneratorFactory<IdTypedValueGeneratorFactory<T>>();

		return builder;
	}
}