using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace StrictId.AspNetCore.Routing;

/// <summary>
/// Route constraint that matches a segment parsable as a non-generic <see cref="IdNumber"/>.
/// Registered under the token <c>idnumber</c> so a route template such as
/// <c>/orders/{id:idnumber}</c> only matches when the segment is a valid bare decimal
/// integer or a prefixed form. Entity-agnostic; per-entity prefix validation happens
/// during parameter binding.
/// </summary>
public sealed class IdNumberRouteConstraint : IRouteConstraint
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
		return IdNumber.IsValid(raw.ToString());
	}
}
