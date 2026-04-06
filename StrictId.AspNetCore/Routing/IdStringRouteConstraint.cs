using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace StrictId.AspNetCore.Routing;

/// <summary>
/// Route constraint that matches a segment parsable as a non-generic <see cref="IdString"/>.
/// Registered under the token <c>idstring</c> so a route template such as
/// <c>/customers/{id:idstring}</c> only matches when the segment is a non-empty,
/// non-whitespace opaque-string identifier. Uses the default validation rules; per-entity
/// tightening from <see cref="IdStringAttribute"/> is applied during parameter binding.
/// </summary>
public sealed class IdStringRouteConstraint : IRouteConstraint
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
		return IdString.IsValid(raw.ToString());
	}
}
