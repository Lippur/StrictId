using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace StrictId.AspNetCore.Routing;

/// <summary>
/// Route constraint that matches a segment parsable as a <see cref="Guid"/>. Registered
/// under the token <c>strictguid</c>. Accepts bare and StrictId-prefixed GUID forms.
/// Entity-agnostic; per-entity prefix validation happens during parameter binding via
/// <see cref="Guid{T}.TryParse(string?, out Guid{T})"/>.
/// </summary>
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
