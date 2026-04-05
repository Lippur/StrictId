using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace StrictId.AspNetCore.Routing;

/// <summary>
/// Route constraint that matches a segment parsable as a non-generic <see cref="Id"/>.
/// Registered under the token <c>id</c> so a route template such as
/// <c>/users/{id:id}</c> only matches when the segment is a valid bare ULID, bare GUID,
/// or a prefixed form.
/// </summary>
/// <remarks>
/// <para>
/// The constraint runs independently of which closed generic <c>Id&lt;T&gt;</c> the
/// action parameter is typed as — a route constraint sees only the raw route value, not
/// the target parameter's CLR type. That means this constraint accepts any
/// <see cref="Id"/>-compatible string, including one with a prefix that doesn't belong
/// to the target entity. The per-entity validation happens subsequently when
/// <see cref="Id{T}.TryParse(string?, out Id{T})"/> is called during parameter binding,
/// which honours the registered prefix list for the closed generic.
/// </para>
/// <para>
/// Route binding in .NET 7+ is already free via <see cref="ISpanParsable{TSelf}"/>.
/// This constraint is purely a URL-matching filter: it prevents a request with a
/// malformed segment from ever reaching the action, so the dispatcher can fall through
/// to a different route instead of surfacing a 400.
/// </para>
/// </remarks>
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
