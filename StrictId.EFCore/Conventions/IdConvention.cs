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
/// <para>
/// The convention first consults <see cref="StrictIdEfCoreRegistry"/> for a
/// pre-constructed <see cref="ValueConverter"/>. Entries are populated by the StrictId
/// source generator for every <c>[IdPrefix]</c>-decorated type visible at compile time,
/// which keeps the hot model-build path free of <see cref="Type.MakeGenericType(Type[])"/>
/// and its associated trim / AOT warnings. On a miss the convention falls back to
/// <see cref="Activator.CreateInstance(Type)"/> on a closed generic, which is still
/// functionally correct but is annotated as dynamic-code-dependent.
/// </para>
/// </remarks>
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
			var converter = ResolveConverter(clrType, typeof(IdToStringConverter<>), typeArgument);
			propertyBuilder
				.HasConversion(converter)
				?.HasMaxLength(26)
				?.IsUnicode(false);
		}
		else if (definition == typeof(IdNumber<>))
		{
			var converter = ResolveConverter(clrType, typeof(IdNumberToLongConverter<>), typeArgument);
			propertyBuilder.HasConversion(converter);
		}
		else if (definition == typeof(IdString<>))
		{
			var (maxLength, asciiOnly) = ResolveIdStringColumnHints(typeArgument);
			var converter = ResolveConverter(clrType, typeof(IdStringToStringConverter<>), typeArgument);
			var chain = propertyBuilder.HasConversion(converter)?.HasMaxLength(maxLength);
			if (asciiOnly) chain?.IsUnicode(false);
		}
		else if (definition == typeof(Guid<>))
		{
			var converter = ResolveConverter(clrType, typeof(GuidToGuidConverter<>), typeArgument);
			propertyBuilder.HasConversion(converter);
		}
	}

	/// <summary>
	/// Returns a <see cref="ValueConverter"/> for <paramref name="closedIdType"/>. First
	/// consults <see cref="StrictIdEfCoreRegistry"/> for a source-generated instance; on
	/// miss, falls back to closing the open-generic converter via reflection.
	/// </summary>
	// Same pattern as StrictId's JSON factories: ResolveConverter sits behind
	// IPropertyAddedConvention.ProcessPropertyAdded which cannot itself be
	// annotated, so the call site suppressions are required even though the
	// reflection helper is annotated correctly. The registry guard ensures the
	// reflection path is only reached for types the source generator did not
	// visit — AOT + EFCore consumers who decorate every entity with [IdPrefix]
	// will hit the cache every time and never execute the fallback.
	[UnconditionalSuppressMessage("AOT", "IL3050",
		Justification = "The reflection fallback only runs when the StrictId source generator did not emit a registration for this closed id type. Source-gen-visible types hit the StrictIdEfCoreRegistry cache and never reach this code path at runtime.")]
	[UnconditionalSuppressMessage("Trimming", "IL2026",
		Justification = "Same guard as IL3050 — the reflection fallback is gated by a StrictIdEfCoreRegistry lookup populated at module init by the source generator.")]
	private static ValueConverter? ResolveConverter (Type closedIdType, Type openGenericConverter, Type typeArgument)
	{
		if (StrictIdEfCoreRegistry.TryGetValueConverter(closedIdType, out var cached))
			return cached;
		return CreateConverterViaReflection(openGenericConverter, typeArgument);
	}

	[RequiresDynamicCode("StrictId's EF Core convention falls back to Activator.CreateInstance + MakeGenericType when the StrictId source generator did not produce a concrete converter for this closed id type. Decorate the entity with [IdPrefix] (or register manually via StrictIdEfCoreRegistry.RegisterValueConverter) to stay on the AOT-friendly path.")]
	[RequiresUnreferencedCode("StrictId's EF Core convention falls back to reflection on closed generic converter types when no pre-registered converter exists.")]
	private static ValueConverter? CreateConverterViaReflection (Type openGenericConverter, Type typeArgument)
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
		// Default charset is AlphanumericDashUnderscore (ASCII-only).
		return (255, true);
	}
}
