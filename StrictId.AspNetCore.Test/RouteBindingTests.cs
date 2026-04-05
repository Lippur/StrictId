using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace StrictId.AspNetCore.Test;

/// <summary>
/// Verifies that StrictId route binding via <see cref="ISpanParsable{TSelf}"/> works
/// out of the box — no opt-in required — and that the three route constraints
/// registered by <see cref="StrictIdAspNetCoreExtensions.AddStrictIdRouteConstraints"/>
/// correctly pre-filter URL segments before dispatch.
/// </summary>
[TestFixture]
public class RouteBindingTests
{
	[Test]
	public async Task IdOfT_BindsFromPrefixedSegment ()
	{
		var expected = Id<User>.NewId();
		await using var app = await TestHostBuilder.StartAsync(
			configureServices: _ => { },
			configurePipeline: a => a.MapGet("/users/{id}", (Id<User> id) => Results.Ok(id.ToString()))
		);

		var response = await app.CreateClient().GetAsync($"/users/{expected}");
		response.StatusCode.Should().Be(HttpStatusCode.OK);
		(await response.Content.ReadAsStringAsync()).Should().Contain(expected.ToString());
	}

	[Test]
	public async Task IdOfT_BindsFromBareSegment_WhenPrefixMissing ()
	{
		var expected = Id<User>.NewId();
		var bareForm = expected.ToString("B");
		await using var app = await TestHostBuilder.StartAsync(
			configurePipeline: a => a.MapGet("/users/{id}", (Id<User> id) => Results.Ok(id.ToString()))
		);

		var response = await app.CreateClient().GetAsync($"/users/{bareForm}");
		response.StatusCode.Should().Be(HttpStatusCode.OK);
	}

	[Test]
	public async Task IdOfT_UnknownPrefix_Returns400 ()
	{
		// "order" is Order's prefix, not User's — binding should refuse.
		var wrongPrefix = $"order_{Ulid.NewUlid().ToString().ToLowerInvariant()}";
		await using var app = await TestHostBuilder.StartAsync(
			configurePipeline: a => a.MapGet("/users/{id}", (Id<User> id) => Results.Ok(id.ToString()))
		);

		var response = await app.CreateClient().GetAsync($"/users/{wrongPrefix}");
		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
	}

	[Test]
	public async Task IdNumberOfT_BindsFromPrefixedSegment ()
	{
		await using var app = await TestHostBuilder.StartAsync(
			configurePipeline: a => a.MapGet("/invoices/{id}", (IdNumber<Invoice> id) => Results.Ok(id.ToString()))
		);

		var response = await app.CreateClient().GetAsync("/invoices/inv_42");
		response.StatusCode.Should().Be(HttpStatusCode.OK);
		(await response.Content.ReadAsStringAsync()).Should().Contain("inv_42");
	}

	[Test]
	public async Task IdStringOfT_BindsFromPrefixedSegment ()
	{
		await using var app = await TestHostBuilder.StartAsync(
			configurePipeline: a => a.MapGet("/customers/{id}", (IdString<Customer> id) => Results.Ok(id.ToString()))
		);

		var response = await app.CreateClient().GetAsync("/customers/cus_abc123");
		response.StatusCode.Should().Be(HttpStatusCode.OK);
		(await response.Content.ReadAsStringAsync()).Should().Contain("cus_abc123");
	}

	[Test]
	public async Task RouteConstraint_Id_RejectsMalformedSegment ()
	{
		await using var app = await TestHostBuilder.StartAsync(
			configureServices: s => s.AddStrictIdRouteConstraints(),
			configurePipeline: a => a.MapGet("/users/{id:id}", (string id) => Results.Ok(id))
		);

		var response = await app.CreateClient().GetAsync("/users/not-an-id");
		response.StatusCode.Should().Be(HttpStatusCode.NotFound);
	}

	[Test]
	public async Task RouteConstraint_Id_AcceptsValidUlid ()
	{
		var validUlid = Ulid.NewUlid().ToString().ToLowerInvariant();
		await using var app = await TestHostBuilder.StartAsync(
			configureServices: s => s.AddStrictIdRouteConstraints(),
			configurePipeline: a => a.MapGet("/users/{id:id}", (string id) => Results.Ok(id))
		);

		var response = await app.CreateClient().GetAsync($"/users/{validUlid}");
		response.StatusCode.Should().Be(HttpStatusCode.OK);
	}

	[Test]
	public async Task RouteConstraint_IdNumber_AcceptsDigits ()
	{
		await using var app = await TestHostBuilder.StartAsync(
			configureServices: s => s.AddStrictIdRouteConstraints(),
			configurePipeline: a => a.MapGet("/orders/{id:idnumber}", (string id) => Results.Ok(id))
		);

		var response = await app.CreateClient().GetAsync("/orders/42");
		response.StatusCode.Should().Be(HttpStatusCode.OK);
	}

	[Test]
	public async Task RouteConstraint_IdNumber_RejectsLetters ()
	{
		await using var app = await TestHostBuilder.StartAsync(
			configureServices: s => s.AddStrictIdRouteConstraints(),
			configurePipeline: a => a.MapGet("/orders/{id:idnumber}", (string id) => Results.Ok(id))
		);

		var response = await app.CreateClient().GetAsync("/orders/abc");
		response.StatusCode.Should().Be(HttpStatusCode.NotFound);
	}

	[Test]
	public async Task RouteConstraint_IdString_AcceptsAlphanumeric ()
	{
		await using var app = await TestHostBuilder.StartAsync(
			configureServices: s => s.AddStrictIdRouteConstraints(),
			configurePipeline: a => a.MapGet("/customers/{id:idstring}", (string id) => Results.Ok(id))
		);

		var response = await app.CreateClient().GetAsync("/customers/abc123");
		response.StatusCode.Should().Be(HttpStatusCode.OK);
	}

	[Test]
	public async Task RouteConstraint_IdString_RejectsWhitespace ()
	{
		await using var app = await TestHostBuilder.StartAsync(
			configureServices: s => s.AddStrictIdRouteConstraints(),
			configurePipeline: a => a.MapGet("/customers/{id:idstring}", (string id) => Results.Ok(id))
		);

		// A segment that URL-decodes to contain a literal whitespace should fail the
		// IdString non-whitespace rule even though routing's path parser accepts it.
		var response = await app.CreateClient().GetAsync("/customers/a%20b");
		response.StatusCode.Should().Be(HttpStatusCode.NotFound);
	}
}
