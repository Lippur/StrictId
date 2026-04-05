using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StrictId.EFCore.Conventions;
using StrictId.EFCore.ValueConverters;
using StrictId.EFCore.ValueGenerators;

namespace StrictId.EFCore;

/// <summary>
/// Entity Framework Core integration helpers for <see cref="Id"/> and <see cref="Id{T}"/>.
/// </summary>
public static class EfCoreExtensions
{
	/// <summary>
	/// Configures EF Core to map <see cref="Id"/> and <see cref="Id{T}"/> properties as fixed-length
	/// 26-character strings using the canonical Crockford base32 ULID representation. Also registers
	/// an <see cref="IdConvention"/> that applies the typed converter to any <see cref="Id{T}"/>
	/// property discovered by the model builder.
	/// </summary>
	/// <param name="builder">The model configuration builder.</param>
	/// <returns>The same <paramref name="builder"/>, for chaining.</returns>
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

	/// <summary>
	/// Configures a non-generic <see cref="Id"/> property to be populated by
	/// <see cref="IdValueGenerator"/> on add, generating a new random ULID-backed id.
	/// </summary>
	public static PropertyBuilder<Id> HasIdValueGenerator (this PropertyBuilder<Id> builder)
	{
		builder
			.HasValueGenerator<IdValueGenerator>()
			.HasValueGeneratorFactory<IdValueGeneratorFactory>();

		return builder;
	}

	/// <summary>
	/// Configures a strongly-typed <see cref="Id{T}"/> property to be populated by
	/// <see cref="IdTypedValueGenerator{T}"/> on add, generating a new random ULID-backed id.
	/// </summary>
	public static PropertyBuilder<Id<T>> HasStrictIdValueGenerator<T> (this PropertyBuilder<Id<T>> builder)
	{
		builder
			.HasValueGenerator<IdTypedValueGenerator<T>>()
			.HasValueGeneratorFactory<IdTypedValueGeneratorFactory<T>>();

		return builder;
	}

	/// <summary>
	/// Configures a <see cref="string"/> property to be populated with a new ULID string by
	/// <see cref="IdStringValueGenerator"/> on add. Useful when the column is declared as a plain
	/// string (e.g., on legacy schemas) but should still receive a StrictId-compatible value.
	/// </summary>
	public static PropertyBuilder<string> HasIdStringValueGenerator (this PropertyBuilder<string> builder)
	{
		builder
			.HasValueGenerator<IdStringValueGenerator>()
			.HasValueGeneratorFactory<IdStringValueGeneratorFactory>();

		return builder;
	}
}
