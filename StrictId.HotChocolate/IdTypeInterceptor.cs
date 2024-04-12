using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors;

namespace StrictId.HotChocolate;

/// <summary>
/// Register all generic ID types as typed ID scalars, with name {Type}Id 
/// </summary>
public class IdTypeInterceptor : TypeInterceptor
{
	public override IEnumerable<TypeReference> RegisterMoreTypes (
		IReadOnlyCollection<ITypeDiscoveryContext> discoveryContexts
	)
	{
		return discoveryContexts.SelectMany(c => c.Dependencies.Select(d => d.Type))
			.OfType<ExtendedTypeReference>()
			.Where(
				r => r.Type.Definition is { IsGenericType: true } &&
				     r.Type.Definition.GetGenericTypeDefinition() == typeof(Id<>)
			)
			.Select(t => new IdTypedScalar(t.Type.TypeArguments[0].Type))
			.DistinctBy(s => s.Name)
			.Where(s => discoveryContexts.All(d => d.Type.Name != s.Name))
			.Select(scalar => TypeReference.Create(scalar));
	}
}