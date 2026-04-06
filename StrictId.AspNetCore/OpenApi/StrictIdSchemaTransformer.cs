using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace StrictId.AspNetCore.OpenApi;

/// <summary>
/// ASP.NET Core OpenAPI schema transformer that rewrites StrictId type schemas into
/// string types with per-type <c>pattern</c>, <c>example</c>, and <c>description</c>.
/// Overrides the default schema generation, which would expose the underlying
/// <c>Value</c> property instead of the actual JSON wire format (a single string).
/// </summary>
internal sealed class StrictIdSchemaTransformer
{
	/// <summary>
	/// Delegate conforming to the <see cref="OpenApiOptions.AddSchemaTransformer(Func{OpenApiSchema,OpenApiSchemaTransformerContext,System.Threading.CancellationToken,System.Threading.Tasks.Task})"/>
	/// contract. Wires a fresh instance into the ASP.NET Core OpenAPI pipeline.
	/// </summary>
	public static Task TransformAsync (
		OpenApiSchema schema,
		OpenApiSchemaTransformerContext context,
		CancellationToken cancellationToken
	)
	{
		var fields = StrictIdSchemaBuilder.TryBuildFor(context.JsonTypeInfo.Type);
		if (fields is null) return Task.CompletedTask;

		// Reset any previously-generated structure and write the string shape. Clearing
		// Properties in particular is important: the default reflection walk may have
		// populated a property schema from the record struct's Value accessor, which
		// would bleed through in the rendered document otherwise.
		schema.Type = JsonSchemaType.String;
		schema.Format = null;
		schema.Properties = null;
		schema.Required = null;
		schema.AdditionalProperties = null;
		schema.Pattern = fields.Value.Pattern;
		schema.Example = JsonValue.Create(fields.Value.Example);
		schema.Description = fields.Value.Description;

		return Task.CompletedTask;
	}
}
