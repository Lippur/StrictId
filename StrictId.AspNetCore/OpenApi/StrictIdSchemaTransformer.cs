using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace StrictId.AspNetCore.OpenApi;

/// <summary>
/// ASP.NET Core OpenAPI schema transformer that recognises every closed StrictId
/// generic and rewrites its schema into a string type with a per-closed-generic
/// <c>pattern</c>, <c>example</c>, and prefix-aware <c>description</c>. Registered via
/// <see cref="StrictIdAspNetCoreExtensions.AddStrictIdOpenApi"/>.
/// </summary>
/// <remarks>
/// <para>
/// The default OpenAPI schema generation in .NET 10 walks a type's JSON serialization
/// shape, which for the StrictId record structs would expose their underlying
/// <see cref="Ulid"/> / <see cref="ulong"/> / <see cref="string"/> <c>Value</c>
/// property and anything else the JSON converter writes. That is not how StrictId
/// serialises — the type-level <see cref="System.Text.Json.Serialization.JsonConverter"/>
/// writes a single string. This transformer overrides the schema to match the actual
/// wire format.
/// </para>
/// <para>
/// Recognition is duck-typed from the CLR type: the non-generic families are matched
/// directly, and the generic families are matched by comparing the open generic
/// definition. Once a match is found, the transformer delegates to
/// <see cref="StrictIdSchemaBuilder"/> to compute the pattern / example / description
/// from the entity type's <see cref="IdPrefixAttribute"/> and (for <see cref="IdString{T}"/>)
/// <see cref="IdStringAttribute"/> metadata.
/// </para>
/// </remarks>
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
