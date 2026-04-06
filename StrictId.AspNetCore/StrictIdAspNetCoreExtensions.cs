using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using StrictId.AspNetCore.OpenApi;
using StrictId.AspNetCore.ProblemDetails;
using StrictId.AspNetCore.Routing;
using StrictId.AspNetCore.TypeConverters;

namespace StrictId.AspNetCore;

/// <summary>
/// Service-collection extensions for StrictId's ASP.NET Core integration: OpenAPI
/// schemas, route constraints, <see cref="TypeConverter"/> registrations, and
/// <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/> mapping for parse failures.
/// Call <see cref="AddStrictId"/> to enable them all, or call each individually.
/// </summary>
public static class StrictIdAspNetCoreExtensions
{
	/// <summary>
	/// Enables every StrictId ASP.NET Core integration in one call: OpenAPI schemas,
	/// route constraints, type converters, and the <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/>
	/// exception handler.
	/// </summary>
	/// <param name="services">The service collection to configure.</param>
	/// <returns>The same <paramref name="services"/> for chaining.</returns>
	[RequiresDynamicCode("Enumerates source-gen-registered entity types and closes StrictId TypeConverters over them via reflection.")]
	public static IServiceCollection AddStrictId (this IServiceCollection services)
	{
		ArgumentNullException.ThrowIfNull(services);
		services.AddStrictIdOpenApi();
		services.AddStrictIdRouteConstraints();
		services.AddStrictIdTypeConverters();
		services.AddStrictIdProblemDetails();
		return services;
	}

	/// <summary>
	/// Registers StrictId OpenAPI schema and operation transformers so that all StrictId
	/// types appear in the generated document as string schemas with per-type pattern,
	/// example, and description. Attached to all named OpenAPI documents.
	/// </summary>
	/// <param name="services">The service collection to configure.</param>
	/// <returns>The same <paramref name="services"/> for chaining.</returns>
	public static IServiceCollection AddStrictIdOpenApi (this IServiceCollection services)
	{
		ArgumentNullException.ThrowIfNull(services);
		services.ConfigureAll<OpenApiOptions>(options =>
		{
			options.AddSchemaTransformer(StrictIdSchemaTransformer.TransformAsync);
			options.AddOperationTransformer(StrictIdOperationTransformer.TransformAsync);
		});
		return services;
	}

	/// <summary>
	/// Registers StrictId route constraints — <c>id</c>, <c>idnumber</c>,
	/// <c>idstring</c>, and <c>strictguid</c> — in <c>RouteOptions.ConstraintMap</c>
	/// so route templates can pre-filter URL segments before dispatch.
	/// </summary>
	/// <param name="services">The service collection to configure.</param>
	/// <returns>The same <paramref name="services"/> for chaining.</returns>
	public static IServiceCollection AddStrictIdRouteConstraints (this IServiceCollection services)
	{
		ArgumentNullException.ThrowIfNull(services);
		services.Configure<RouteOptions>(options =>
		{
			options.ConstraintMap["id"] = typeof(IdRouteConstraint);
			options.ConstraintMap["idnumber"] = typeof(IdNumberRouteConstraint);
			options.ConstraintMap["idstring"] = typeof(IdStringRouteConstraint);
			options.ConstraintMap["strictguid"] = typeof(GuidRouteConstraint);
		});
		return services;
	}

	/// <summary>
	/// Attaches <see cref="TypeConverterAttribute"/>s to every known StrictId value type
	/// so that <see cref="TypeDescriptor.GetConverter(Type)"/>-based binding paths can
	/// round-trip StrictIds through their string form. Walks <see cref="StrictIdRegistry"/>
	/// to register closed generics for each entity type the source generator discovered.
	/// </summary>
	/// <param name="services">The service collection to configure.</param>
	/// <returns>The same <paramref name="services"/> for chaining.</returns>
	/// <remarks>
	/// For AOT builds, use <see cref="AddStrictIdTypeConverter{TEntity}"/> per entity instead.
	/// </remarks>
	[RequiresDynamicCode("Closes StrictId TypeConverter generics over registered entity types via Type.MakeGenericType.")]
	public static IServiceCollection AddStrictIdTypeConverters (this IServiceCollection services)
	{
		ArgumentNullException.ThrowIfNull(services);

		RegisterNonGenericTypeConverters();

		foreach (var entityType in StrictIdRegistry.EnumerateRegisteredEntityTypes())
			RegisterClosedGenericTypeConverters(entityType);

		return services;
	}

	/// <summary>
	/// AOT-friendly alternative to <see cref="AddStrictIdTypeConverters"/>: registers
	/// StrictId type converters for a single entity type without reflection.
	/// </summary>
	/// <param name="services">The service collection to configure.</param>
	/// <typeparam name="TEntity">The entity type for which to register converters.</typeparam>
	/// <returns>The same <paramref name="services"/> for chaining.</returns>
	public static IServiceCollection AddStrictIdTypeConverter<TEntity> (this IServiceCollection services)
	{
		ArgumentNullException.ThrowIfNull(services);
		TypeDescriptor.AddAttributes(typeof(Id<TEntity>), new TypeConverterAttribute(typeof(IdTypeConverter<TEntity>)));
		TypeDescriptor.AddAttributes(typeof(IdNumber<TEntity>), new TypeConverterAttribute(typeof(IdNumberTypeConverter<TEntity>)));
		TypeDescriptor.AddAttributes(typeof(IdString<TEntity>), new TypeConverterAttribute(typeof(IdStringTypeConverter<TEntity>)));
		TypeDescriptor.AddAttributes(typeof(Guid<TEntity>), new TypeConverterAttribute(typeof(GuidTypeConverter<TEntity>)));
		return services;
	}

	/// <summary>
	/// Registers the <see cref="StrictIdFormatExceptionHandler"/> as an
	/// <see cref="Microsoft.AspNetCore.Diagnostics.IExceptionHandler"/> so
	/// <see cref="FormatException"/>s from StrictId parsers are mapped to 400 Bad Request
	/// with a <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/> body.
	/// Non-StrictId exceptions fall through to the next handler.
	/// </summary>
	/// <param name="services">The service collection to configure.</param>
	/// <returns>The same <paramref name="services"/> for chaining.</returns>
	public static IServiceCollection AddStrictIdProblemDetails (this IServiceCollection services)
	{
		ArgumentNullException.ThrowIfNull(services);
		services.TryAddSingleton<StrictIdFormatExceptionHandler>();
		services.AddExceptionHandler<StrictIdFormatExceptionHandler>();
		return services;
	}

	// ═════ Private helpers ═══════════════════════════════════════════════════

	/// <summary>
	/// Installs <see cref="TypeConverterAttribute"/>s on the three non-generic StrictId types.
	/// </summary>
	private static void RegisterNonGenericTypeConverters ()
	{
		TypeDescriptor.AddAttributes(typeof(Id), new TypeConverterAttribute(typeof(IdTypeConverter)));
		TypeDescriptor.AddAttributes(typeof(IdNumber), new TypeConverterAttribute(typeof(IdNumberTypeConverter)));
		TypeDescriptor.AddAttributes(typeof(IdString), new TypeConverterAttribute(typeof(IdStringTypeConverter)));
	}

	/// <summary>
	/// Registers closed StrictId type converters for <paramref name="entityType"/>.
	/// </summary>
	[RequiresDynamicCode("Uses Type.MakeGenericType to close StrictId TypeConverters over registered entity types.")]
	private static void RegisterClosedGenericTypeConverters (Type entityType)
	{
		var idClosed = typeof(Id<>).MakeGenericType(entityType);
		var idNumberClosed = typeof(IdNumber<>).MakeGenericType(entityType);
		var idStringClosed = typeof(IdString<>).MakeGenericType(entityType);
		var guidClosed = typeof(Guid<>).MakeGenericType(entityType);

		var idConverterClosed = typeof(IdTypeConverter<>).MakeGenericType(entityType);
		var idNumberConverterClosed = typeof(IdNumberTypeConverter<>).MakeGenericType(entityType);
		var idStringConverterClosed = typeof(IdStringTypeConverter<>).MakeGenericType(entityType);
		var guidConverterClosed = typeof(GuidTypeConverter<>).MakeGenericType(entityType);

		TypeDescriptor.AddAttributes(idClosed, new TypeConverterAttribute(idConverterClosed));
		TypeDescriptor.AddAttributes(idNumberClosed, new TypeConverterAttribute(idNumberConverterClosed));
		TypeDescriptor.AddAttributes(idStringClosed, new TypeConverterAttribute(idStringConverterClosed));
		TypeDescriptor.AddAttributes(guidClosed, new TypeConverterAttribute(guidConverterClosed));
	}
}
