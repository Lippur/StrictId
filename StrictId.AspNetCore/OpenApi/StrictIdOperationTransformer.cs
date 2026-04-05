using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace StrictId.AspNetCore.OpenApi;

/// <summary>
/// Operation-level transformer that rewrites the schemas of path, query, header, and
/// form parameters whose CLR type is a StrictId value type. Complements
/// <see cref="StrictIdSchemaTransformer"/>, which only fires for body / response
/// schemas in .NET 10 — path parameters bound via <see cref="ISpanParsable{TSelf}"/>
/// bypass the JSON type-info schema path and have their <c>string</c> schemas built
/// directly from the model binder's expected shape.
/// </summary>
/// <remarks>
/// The transformer walks <see cref="Microsoft.AspNetCore.Mvc.ApiExplorer.ApiDescription.ParameterDescriptions"/>
/// to recover each parameter's CLR type, matches it against the six StrictId shapes,
/// and writes the family-specific pattern, example, and description into the matching
/// parameter's schema via <see cref="StrictIdSchemaBuilder"/>.
/// </remarks>
internal sealed class StrictIdOperationTransformer
{
	/// <summary>
	/// Delegate conforming to the <see cref="OpenApiOptions.AddOperationTransformer(Func{Microsoft.OpenApi.OpenApiOperation,OpenApiOperationTransformerContext,System.Threading.CancellationToken,System.Threading.Tasks.Task})"/>
	/// contract.
	/// </summary>
	public static Task TransformAsync (
		Microsoft.OpenApi.OpenApiOperation operation,
		OpenApiOperationTransformerContext context,
		CancellationToken cancellationToken
	)
	{
		if (operation.Parameters is null) return Task.CompletedTask;

		foreach (var paramDescription in context.Description.ParameterDescriptions)
		{
			var clrType = paramDescription.Type;
			if (clrType is null) continue;

			var fields = StrictIdSchemaBuilder.TryBuildFor(clrType);
			if (fields is null) continue;

			// Find the matching operation parameter by name. ApiDescription uses the
			// same parameter name that ends up in the OpenAPI document so this is a
			// straightforward lookup.
			foreach (var openApiParam in operation.Parameters)
			{
				if (openApiParam.Name != paramDescription.Name) continue;
				if (openApiParam is not OpenApiParameter writable) continue;

				writable.Schema = new OpenApiSchema
				{
					Type = JsonSchemaType.String,
					Pattern = fields.Value.Pattern,
					Example = JsonValue.Create(fields.Value.Example),
					Description = fields.Value.Description,
				};
				break;
			}
		}

		return Task.CompletedTask;
	}
}
