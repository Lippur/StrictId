using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using MvcProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace StrictId.AspNetCore.Test;

/// <summary>
/// Tests for the <see cref="StrictId.AspNetCore.ProblemDetails.StrictIdFormatExceptionHandler"/>
/// registered via <see cref="StrictIdAspNetCoreExtensions.AddStrictIdProblemDetails"/>.
/// Confirms that <see cref="FormatException"/>s originating in StrictId parsers are
/// mapped to an RFC 7807 <see cref="MvcProblemDetails"/> body with HTTP 400, and that
/// <see cref="FormatException"/>s from unrelated code paths are left alone.
/// </summary>
[TestFixture]
public class ProblemDetailsTests
{
	[Test]
	public async Task StrictIdFormatException_IsMappedToProblemDetails400 ()
	{
		await using var app = await TestHostBuilder.StartAsync(
			configureServices: services =>
			{
				services.AddStrictIdProblemDetails();
				services.AddProblemDetails();
			},
			configurePipeline: a =>
			{
				a.UseExceptionHandler();
				a.MapGet("/throw", () =>
				{
					// Deliberately call Parse with an invalid input to surface the
					// StrictId diagnostic as a FormatException from the action body.
					var _ = Id<User>.Parse("not-a-valid-id");
					return Results.Ok();
				});
			});

		var response = await app.CreateClient().GetAsync("/throw");
		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

		var problem = await response.Content.ReadFromJsonAsync<MvcProblemDetails>();
		problem.Should().NotBeNull();
		problem!.Status.Should().Be(400);
		problem.Title.Should().Be("StrictId parse failed");
		problem.Detail.Should().Contain("not-a-valid-id");
	}

	[Test]
	public async Task NonStrictIdFormatException_FallsThrough ()
	{
		await using var app = await TestHostBuilder.StartAsync(
			configureServices: services =>
			{
				services.AddStrictIdProblemDetails();
				services.AddProblemDetails();
			},
			configurePipeline: a =>
			{
				a.UseExceptionHandler();
				a.MapGet("/other", () =>
				{
					// A FormatException raised inside test code has Source = the test
					// assembly, not "StrictId". The StrictId handler must not claim it.
					throw new FormatException("unrelated failure");
				});
			});

		var response = await app.CreateClient().GetAsync("/other");
		// The default exception handler produces a ProblemDetails at 500 when the
		// exception is unhandled by any custom IExceptionHandler.
		response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
	}

	[Test]
	public async Task StrictIdFormatException_WorksWithoutProblemDetailsService ()
	{
		// Verify the fallback path: when IProblemDetailsService is not registered the
		// handler still emits a structured JSON response directly.
		await using var app = await TestHostBuilder.StartAsync(
			configureServices: services => services.AddStrictIdProblemDetails(),
			configurePipeline: a =>
			{
				a.UseExceptionHandler(_ => { });
				a.MapGet("/throw", () =>
				{
					var _ = Id<User>.Parse("not-valid");
					return Results.Ok();
				});
			});

		var response = await app.CreateClient().GetAsync("/throw");
		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
		response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

		var body = await response.Content.ReadAsStringAsync();
		using var doc = JsonDocument.Parse(body);
		doc.RootElement.GetProperty("title").GetString().Should().Be("StrictId parse failed");
	}
}
