using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace StrictId.AspNetCore.Routing;

/// <summary>
/// Route constraint that matches a segment parsable as a non-generic <see cref="IdString"/>.
/// Registered under the token <c>idstring</c> so a route template such as
/// <c>/customers/{id:idstring}</c> only matches when the segment is a non-empty,
/// non-whitespace opaque-string identifier — bare or prefixed.
/// </summary>
/// <remarks>
/// The non-generic <see cref="IdString"/> uses the default validation rules
/// (<c>MaxLength</c> = 255, <see cref="IdStringCharSet.AlphanumericDashUnderscore"/>). Per-entity tightening
/// declared via <see cref="IdStringAttribute"/> is applied subsequently during parameter
/// binding when the closed generic <see cref="IdString{T}"/> is constructed — the
/// constraint deliberately stays loose so it does not shadow the more informative
/// per-type validation error.
/// </remarks>
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
