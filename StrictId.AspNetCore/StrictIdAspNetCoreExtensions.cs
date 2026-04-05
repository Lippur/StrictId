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
/// Service-collection extensions that opt an ASP.NET Core app into StrictId's
/// integration surface: OpenAPI schemas, route constraints, legacy
/// <see cref="TypeConverter"/> registrations, and a <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/>
/// mapping for parse failures. Each hook is exposed individually so consumers can
/// cherry-pick — call <see cref="AddStrictId"/> to enable them all at once.
/// </summary>
/// <remarks>
/// Route binding for StrictId types is already free in .NET 7+ via
/// <see cref="ISpanParsable{TSelf}"/>; nothing in this class is required to make
/// <c>[HttpGet("/users/{id}")]</c> work with an <see cref="Id{T}"/> parameter. The
/// extensions here are polish on top of that: better OpenAPI, pre-dispatch filtering,
/// legacy-binding support, and structured error responses.
/// </remarks>
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
	/// Registers the StrictId OpenAPI <see cref="OpenApiOptions.AddSchemaTransformer(Func{Microsoft.OpenApi.OpenApiSchema,OpenApiSchemaTransformerContext,CancellationToken,Task})"/>
	/// transformer so that <see cref="Id{T}"/>, <see cref="IdNumber{T}"/>, and
	/// <see cref="IdString{T}"/> (and their non-generic counterparts) appear in the
	/// generated document as string schemas with per-closed-generic pattern, example,
	/// and description.
	/// </summary>
	/// <param name="services">The service collection to configure.</param>
	/// <returns>The same <paramref name="services"/> for chaining.</returns>
	/// <remarks>
	/// Uses <see cref="OptionsServiceCollectionExtensions.ConfigureAll{TOptions}(IServiceCollection,Action{TOptions})"/>
	/// so the transformer is attached to every named OpenAPI document, not just the
	/// default. Call this after <c>services.AddOpenApi()</c> — or before, since options
	/// configuration runs lazily on first resolve.
	/// </remarks>
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
	/// Registers the three StrictId route constraints — <c>id</c>, <c>idnumber</c>, and
	/// <c>idstring</c> — in <c>RouteOptions.ConstraintMap</c> so route templates can
	/// pre-filter URL segments before dispatch.
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
		});
		return services;
	}

	/// <summary>
	/// Attaches <see cref="TypeConverterAttribute"/>s to every known StrictId value
	/// type so legacy binding paths that use <see cref="TypeDescriptor.GetConverter(Type)"/>
	/// — <c>System.Configuration</c>, some third-party model binders, designers, XAML
	/// — can round-trip StrictIds through their string form.
	/// </summary>
	/// <param name="services">The service collection to configure.</param>
	/// <returns>The same <paramref name="services"/> for chaining.</returns>
	/// <remarks>
	/// <para>
	/// The non-generic <see cref="Id"/>, <see cref="IdNumber"/>, and <see cref="IdString"/>
	/// are registered unconditionally. For the generic families, the extension walks
	/// <see cref="StrictIdRegistry"/> — populated at module init by the StrictId source
	/// generator — and registers the closed <c>IdTypeConverter&lt;T&gt;</c> /
	/// <c>IdNumberTypeConverter&lt;T&gt;</c> / <c>IdStringTypeConverter&lt;T&gt;</c> per
	/// entity. Entities that the generator did not see (for example when
	/// <c>&lt;EnableStrictIdSourceGenerator&gt;false&lt;/EnableStrictIdSourceGenerator&gt;</c>)
	/// still bind through <see cref="ISpanParsable{TSelf}"/> in modern ASP.NET Core; the
	/// legacy converter path just won't fire for them.
	/// </para>
	/// <para>
	/// Marked <see cref="RequiresDynamicCodeAttribute"/> because closing the generic
	/// converter types over the enumerated entity types uses
	/// <c>Type.MakeGenericType</c>. For trim / AOT builds, either call the explicit
	/// <see cref="AddStrictIdTypeConverter{TEntity}"/> per entity or skip this extension
	/// and rely on route / JSON binding exclusively.
	/// </para>
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
	/// AOT-friendly companion to <see cref="AddStrictIdTypeConverters"/>: registers the
	/// three closed-generic StrictId type converters (ULID, numeric, string) for a
	/// single entity type without any reflection closing of open generics. Useful when
	/// trimming or AOT-publishing an app whose entity types are not all visible to the
	/// StrictId source generator.
	/// </summary>
	/// <param name="services">The service collection to configure.</param>
	/// <typeparam name="TEntity">The phantom entity tag for which to register converters.</typeparam>
	/// <returns>The same <paramref name="services"/> for chaining.</returns>
	public static IServiceCollection AddStrictIdTypeConverter<TEntity> (this IServiceCollection services)
	{
		ArgumentNullException.ThrowIfNull(services);
		TypeDescriptor.AddAttributes(typeof(Id<TEntity>), new TypeConverterAttribute(typeof(IdTypeConverter<TEntity>)));
		TypeDescriptor.AddAttributes(typeof(IdNumber<TEntity>), new TypeConverterAttribute(typeof(IdNumberTypeConverter<TEntity>)));
		TypeDescriptor.AddAttributes(typeof(IdString<TEntity>), new TypeConverterAttribute(typeof(IdStringTypeConverter<TEntity>)));
		return services;
	}

	/// <summary>
	/// Registers the <see cref="StrictIdFormatExceptionHandler"/> as an
	/// <see cref="Microsoft.AspNetCore.Diagnostics.IExceptionHandler"/> so unhandled
	/// <see cref="FormatException"/>s originating in StrictId parsers are mapped to a
	/// 400 Bad Request with a <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/> body
	/// carrying the verbose diagnostic from the failing parse.
	/// </summary>
	/// <param name="services">The service collection to configure.</param>
	/// <returns>The same <paramref name="services"/> for chaining.</returns>
	/// <remarks>
	/// The handler is a no-op for exceptions that did not originate in StrictId, so
	/// registering it alongside other <see cref="Microsoft.AspNetCore.Diagnostics.IExceptionHandler"/>
	/// implementations is safe — control falls through to the next handler in order.
	/// Requires the app to call <c>app.UseExceptionHandler()</c> in the pipeline for the
	/// handler to be invoked.
	/// </remarks>
	public static IServiceCollection AddStrictIdProblemDetails (this IServiceCollection services)
	{
		ArgumentNullException.ThrowIfNull(services);
		services.TryAddSingleton<StrictIdFormatExceptionHandler>();
		services.AddExceptionHandler<StrictIdFormatExceptionHandler>();
		return services;
	}

	// ═════ Private helpers ═══════════════════════════════════════════════════

	/// <summary>
	/// Installs <see cref="TypeConverterAttribute"/>s on the three non-generic StrictId
	/// types. Safe to call multiple times: <see cref="TypeDescriptor.AddAttributes(Type,Attribute[])"/>
	/// appends rather than replacing, and the later attribute wins when both are
	/// present, so repeated registrations produce a stable converter.
	/// </summary>
	private static void RegisterNonGenericTypeConverters ()
	{
		TypeDescriptor.AddAttributes(typeof(Id), new TypeConverterAttribute(typeof(IdTypeConverter)));
		TypeDescriptor.AddAttributes(typeof(IdNumber), new TypeConverterAttribute(typeof(IdNumberTypeConverter)));
		TypeDescriptor.AddAttributes(typeof(IdString), new TypeConverterAttribute(typeof(IdStringTypeConverter)));
	}

	/// <summary>
	/// Closes the three generic converter types over <paramref name="entityType"/> and
	/// registers each as the <see cref="TypeConverterAttribute"/> for the matching
	/// closed StrictId generic.
	/// </summary>
	[RequiresDynamicCode("Uses Type.MakeGenericType to close IdTypeConverter<T>, IdNumberTypeConverter<T>, and IdStringTypeConverter<T>.")]
	private static void RegisterClosedGenericTypeConverters (Type entityType)
	{
		var idClosed = typeof(Id<>).MakeGenericType(entityType);
		var idNumberClosed = typeof(IdNumber<>).MakeGenericType(entityType);
		var idStringClosed = typeof(IdString<>).MakeGenericType(entityType);

		var idConverterClosed = typeof(IdTypeConverter<>).MakeGenericType(entityType);
		var idNumberConverterClosed = typeof(IdNumberTypeConverter<>).MakeGenericType(entityType);
		var idStringConverterClosed = typeof(IdStringTypeConverter<>).MakeGenericType(entityType);

		TypeDescriptor.AddAttributes(idClosed, new TypeConverterAttribute(idConverterClosed));
		TypeDescriptor.AddAttributes(idNumberClosed, new TypeConverterAttribute(idNumberConverterClosed));
		TypeDescriptor.AddAttributes(idStringClosed, new TypeConverterAttribute(idStringConverterClosed));
	}
}
