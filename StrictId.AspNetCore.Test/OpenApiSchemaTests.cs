using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;

namespace StrictId.AspNetCore.Test;

/// <summary>
/// Tests that exercise <see cref="StrictId.AspNetCore.OpenApi.StrictIdSchemaTransformer"/>
/// end-to-end: stand up a minimal API, register <c>AddOpenApi</c> +
/// <c>AddStrictIdOpenApi</c>, fetch the generated document, and verify that each
/// StrictId parameter renders as a string schema with the expected pattern, example,
/// and description.
/// </summary>
/// <remarks>
/// The assertions inspect the rendered JSON document rather than poking at the schema
/// object graph directly so the test exercises the full serialisation pipeline. That
/// catches bugs where the transformer sets a property correctly but the downstream
/// OpenAPI writer drops it — for example, an example value assigned to a property
/// that doesn't round-trip through <c>System.Text.Json.Nodes.JsonNode</c>.
/// </remarks>
[TestFixture]
public class OpenApiSchemaTests
{
	private const string OpenApiDocumentPath = "/openapi/v1.json";

	[Test]
	public async Task SchemaTransformer_EmitsStringSchemaForIdOfT ()
	{
		var doc = await FetchOpenApiDocumentAsync(a =>
			a.MapGet("/users/{id}", (Id<User> id) => Results.Ok(id.ToString())));

		var schema = FindParameterSchema(doc, path: "/users/{id}", paramName: "id");
		schema.GetProperty("type").GetString().Should().Be("string");
		schema.GetProperty("pattern").GetString().Should().Contain("user_");
		schema.GetProperty("example").GetString().Should().StartWith("user_");
		schema.GetProperty("description").GetString().Should().Contain("Id<User>");
	}

	[Test]
	public async Task SchemaTransformer_EmitsStringSchemaForIdNumberOfT ()
	{
		var doc = await FetchOpenApiDocumentAsync(a =>
			a.MapGet("/invoices/{id}", (IdNumber<Invoice> id) => Results.Ok(id.ToString())));

		var schema = FindParameterSchema(doc, "/invoices/{id}", "id");
		schema.GetProperty("type").GetString().Should().Be("string");
		schema.GetProperty("pattern").GetString().Should().Contain(@"\d{1,20}");
		schema.GetProperty("example").GetString().Should().StartWith("inv_");
	}

	[Test]
	public async Task SchemaTransformer_EmitsStringSchemaForIdStringOfT ()
	{
		var doc = await FetchOpenApiDocumentAsync(a =>
			a.MapGet("/customers/{id}", (IdString<Customer> id) => Results.Ok(id.ToString())));

		var schema = FindParameterSchema(doc, "/customers/{id}", "id");
		schema.GetProperty("type").GetString().Should().Be("string");
		schema.GetProperty("pattern").GetString().Should().Contain("cus_");
		schema.GetProperty("pattern").GetString().Should().Contain("[A-Za-z0-9]{1,32}");
		schema.GetProperty("example").GetString().Should().StartWith("cus_");
	}

	[Test]
	public async Task SchemaTransformer_EmitsBareSchemaForEntitiesWithoutPrefix ()
	{
		var doc = await FetchOpenApiDocumentAsync(a =>
			a.MapGet("/anon/{id}", (Id<Anonymous> id) => Results.Ok(id.ToString())));

		var schema = FindParameterSchema(doc, "/anon/{id}", "id");
		schema.GetProperty("type").GetString().Should().Be("string");
		var pattern = schema.GetProperty("pattern").GetString()!;
		pattern.Should().NotContain("_");
		pattern.Should().Contain("[0-7]");
	}

	// ═════ Helpers ═══════════════════════════════════════════════════════════

	private static async Task<JsonDocument> FetchOpenApiDocumentAsync (Action<WebApplication> configureEndpoints)
	{
		var app = await TestHostBuilder.StartAsync(
			configureServices: services =>
			{
				services.AddOpenApi();
				services.AddStrictIdOpenApi();
			},
			configurePipeline: a =>
			{
				configureEndpoints(a);
				a.MapOpenApi();
			});

		try
		{
			var response = await app.CreateClient().GetAsync(OpenApiDocumentPath);
			response.StatusCode.Should().Be(HttpStatusCode.OK);
			var json = await response.Content.ReadAsStringAsync();
			return JsonDocument.Parse(json);
		}
		finally
		{
			await app.DisposeAsync();
		}
	}

	/// <summary>
	/// Locates the schema node for a path parameter, following <c>$ref</c> into
	/// <c>components.schemas</c> when the framework has hoisted the inline schema.
	/// </summary>
	private static JsonElement FindParameterSchema (JsonDocument doc, string path, string paramName)
	{
		var parameters = doc.RootElement
			.GetProperty("paths")
			.GetProperty(path)
			.GetProperty("get")
			.GetProperty("parameters");

		foreach (var param in parameters.EnumerateArray())
		{
			if (param.GetProperty("name").GetString() != paramName) continue;

			var schema = param.GetProperty("schema");
			if (schema.TryGetProperty("$ref", out var refNode))
			{
				var refPath = refNode.GetString()!; // "#/components/schemas/Name"
				var name = refPath.Split('/').Last();
				return doc.RootElement.GetProperty("components").GetProperty("schemas").GetProperty(name);
			}

			return schema;
		}

		throw new InvalidOperationException($"Parameter '{paramName}' not found in '{path}'.");
	}
}
