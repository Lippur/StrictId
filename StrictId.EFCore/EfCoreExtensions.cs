using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StrictId.EFCore.Conventions;
using StrictId.EFCore.ValueConverters;
using StrictId.EFCore.ValueGenerators;

namespace StrictId.EFCore;

/// <summary>
/// Entity Framework Core integration helpers for the three StrictId families
/// (<see cref="Id"/>/<see cref="Id{T}"/>, <see cref="IdNumber"/>/<see cref="IdNumber{T}"/>,
/// and <see cref="IdString"/>/<see cref="IdString{T}"/>).
/// </summary>
public static class EfCoreExtensions
{
	/// <summary>
	/// Registers value converters for every StrictId family on the model. In v3 the
	/// prefix is a C# type-system concept and is never stored in the database; each
	/// converter writes only the underlying bare value.
	/// <list type="bullet">
	/// <item><see cref="Id"/>/<see cref="Id{T}"/> → fixed-length 26-character lowercase
	/// Crockford base32 ULID strings.</item>
	/// <item><see cref="IdNumber"/>/<see cref="IdNumber{T}"/> → <c>bigint</c>
	/// (<see cref="long"/>).</item>
	/// <item><see cref="IdString"/>/<see cref="IdString{T}"/> → <c>varchar</c> sized to the
	/// per-entity <see cref="IdStringAttribute.MaxLength"/> (255 by default).</item>
	/// </list>
	/// For generic families, an <see cref="IdConvention"/> is registered that closes each
	/// open-generic converter to the concrete closed generic type at model-build time.
	/// </summary>
	/// <param name="builder">The model configuration builder.</param>
	/// <returns>The same <paramref name="builder"/>, for chaining.</returns>
	[RequiresDynamicCode("StrictId's EF Core convention closes open-generic converters with runtime type arguments.")]
	[RequiresUnreferencedCode("StrictId's EF Core convention closes open-generic converters with runtime type arguments.")]
	public static ModelConfigurationBuilder ConfigureStrictId (this ModelConfigurationBuilder builder)
	{
		builder.Properties<Id>()
			.HaveConversion<IdToStringConverter>()
			.HaveMaxLength(26)
			.AreFixedLength()
			.AreUnicode(false);

		builder.Properties<IdNumber>()
			.HaveConversion<IdNumberToLongConverter>();

		builder.Properties<IdString>()
			.HaveConversion<IdStringToStringConverter>()
			.HaveMaxLength(255);

		builder.Conventions.Add(_ => new IdConvention());

		return builder;
	}

	/// <summary>
	/// Configures a non-generic <see cref="Id"/> property to be populated by
	/// <see cref="IdValueGenerator"/> on add, generating a new ULID-backed id.
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
	/// <see cref="IdTypedValueGenerator{T}"/> on add, generating a new ULID-backed id.
	/// </summary>
	public static PropertyBuilder<Id<T>> HasStrictIdValueGenerator<T> (this PropertyBuilder<Id<T>> builder)
	{
		builder
			.HasValueGenerator<IdTypedValueGenerator<T>>()
			.HasValueGeneratorFactory<IdTypedValueGeneratorFactory<T>>();

		return builder;
	}
}
