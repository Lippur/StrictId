using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace StrictId.AspNetCore.Routing;

/// <summary>
/// Route constraint that matches a segment parsable as a non-generic <see cref="Id"/>.
/// Registered under the token <c>id</c> so a route template such as
/// <c>/users/{id:id}</c> only matches when the segment is a valid bare ULID, bare GUID,
/// or a prefixed form. Entity-agnostic; per-entity prefix validation happens during
/// parameter binding via <see cref="Id{T}.TryParse(string?, out Id{T})"/>.
/// </summary>
public sealed class IdRouteConstraint : IRouteConstraint
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
		return Id.IsValid(raw.ToString());
	}
}
