using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace StrictId.AspNetCore.Routing;

/// <summary>
/// Route constraint that matches a segment parsable as a non-generic <see cref="IdNumber"/>.
/// Registered under the token <c>idnumber</c> so a route template such as
/// <c>/orders/{id:idnumber}</c> only matches when the segment is a valid bare decimal
/// integer or a prefixed form.
/// </summary>
/// <remarks>
/// Like <see cref="IdRouteConstraint"/>, this filter operates on the raw route value
/// without knowing the target closed generic. It rejects malformed segments up front;
/// the closed generic's per-entity prefix rules are enforced later, during parameter
/// binding.
/// </remarks>
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
