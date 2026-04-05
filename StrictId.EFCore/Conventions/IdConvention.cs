using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using StrictId.EFCore.ValueConverters;

namespace StrictId.EFCore.Conventions;

/// <summary>
/// EF Core convention that applies the correct StrictId value converter to any
/// property typed as <see cref="Id{T}"/>, <see cref="IdNumber{T}"/>, or
/// <see cref="IdString{T}"/>. For <see cref="IdString{T}"/> properties, the convention
/// also applies the per-entity maximum suffix length declared by any
/// <see cref="IdStringAttribute"/> on the entity type.
/// </summary>
/// <remarks>
/// The convention closes the open-generic converter types with the property's closed
/// generic argument, which requires <c>Type.MakeGenericType</c> on the runtime
/// reflection path. This is tolerable at model-build time but not AOT-friendly; Phase 8
/// of the v3 rewrite will replace the reflection path with a source-generator that
/// emits direct instantiations per closed generic.
/// </remarks>
[RequiresDynamicCode("StrictId's EF Core convention closes open-generic converters with runtime type arguments.")]
[RequiresUnreferencedCode("StrictId's EF Core convention closes open-generic converters with runtime type arguments.")]
public class IdConvention : IPropertyAddedConvention
{
	/// <inheritdoc />
	public void ProcessPropertyAdded (
		IConventionPropertyBuilder propertyBuilder,
		IConventionContext<IConventionPropertyBuilder> context
	)
	{
		var clrType = propertyBuilder.Metadata.ClrType;
		if (!clrType.IsGenericType) return;

		var definition = clrType.GetGenericTypeDefinition();
		var typeArgument = clrType.GetGenericArguments()[0];

		if (definition == typeof(Id<>))
		{
			var converter = CreateConverter(typeof(IdToStringConverter<>), typeArgument);
			propertyBuilder
				.HasConversion(converter)
				?.HasMaxLength(26)
				?.IsUnicode(false);
		}
		else if (definition == typeof(IdNumber<>))
		{
			var converter = CreateConverter(typeof(IdNumberToLongConverter<>), typeArgument);
			propertyBuilder.HasConversion(converter);
		}
		else if (definition == typeof(IdString<>))
		{
			var (maxLength, asciiOnly) = ResolveIdStringColumnHints(typeArgument);
			var converter = CreateConverter(typeof(IdStringToStringConverter<>), typeArgument);
			var chain = propertyBuilder.HasConversion(converter)?.HasMaxLength(maxLength);
			if (asciiOnly) chain?.IsUnicode(false);
		}
	}

	private static ValueConverter? CreateConverter (Type openGenericConverter, Type typeArgument)
		=> (ValueConverter?)Activator.CreateInstance(openGenericConverter.MakeGenericType(typeArgument));

	/// <summary>
	/// Walks <paramref name="type"/>'s inheritance chain for an <see cref="IdStringAttribute"/>
	/// and returns the column hints derived from it: the maximum suffix length, plus
	/// whether the character set is restricted to ASCII (in which case the column can
	/// be declared non-Unicode to halve its storage cost on SQL Server). Falls back to
	/// the non-generic default (<c>255</c>, any charset) if no attribute is declared.
	/// Mirrors the resolution order used by the core <c>IdString&lt;T&gt;</c> parser so
	/// that the column width matches the suffix length the ID type accepts.
	/// </summary>
	private static (int MaxLength, bool AsciiOnly) ResolveIdStringColumnHints (Type type)
	{
		Type? current = type;
		while (current is not null)
		{
			var attr = current.GetCustomAttribute<IdStringAttribute>(inherit: false);
			if (attr is not null)
			{
				return (attr.MaxLength, attr.CharSet != IdStringCharSet.Any);
			}
			current = current.BaseType;
		}
		return (255, false);
	}
}
