using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace StrictId.AspNetCore.Routing;

/// <summary>
/// Route constraint that matches a segment parsable as a <see cref="Guid"/>. Registered
/// under the token <c>guid</c> so a route template such as <c>/users/{id:guid}</c> only
/// matches when the segment is a valid GUID (bare or prefixed). Note that ASP.NET Core
/// already ships a built-in <c>guid</c> constraint; this one is a superset that also
/// accepts StrictId-prefixed forms and is registered by
/// <see cref="StrictIdAspNetCoreExtensions.AddStrictIdRouteConstraints"/>.
/// </summary>
/// <remarks>
/// <para>
/// The constraint is entity-agnostic: it checks whether the segment is structurally valid
/// as any <see cref="Guid{T}"/>, not whether the prefix matches a specific entity. Per-
/// entity prefix validation happens during parameter binding via
/// <see cref="Guid{T}.TryParse(string?, out Guid{T})"/>.
/// </para>
/// </remarks>
public sealed class GuidRouteConstraint : IRouteConstraint
{
	/// <inheritdoc />
	public bool Match (
		HttpContext? httpContext,
		IRouter? route,
		string routeKey,
		RouteValueDictionary values,
		RouteDirection routeDirection
	)
	{
		if (!values.TryGetValue(routeKey, out var raw) || raw is null) return false;
		return Guid.TryParse(raw.ToString(), out _);
	}
}
