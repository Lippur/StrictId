using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace StrictId.HotChocolate;

public static class GraphQlExtensions
{
	/// <summary>
	/// Add support for strict ID types Id<T> and Id
	/// </summary>
	/// <param name="builder"></param>
	/// <returns></returns>
	public static IRequestExecutorBuilder AddStrictId (this IRequestExecutorBuilder builder) =>
		builder
			.AddType(new IdScalar())
			.TryAddTypeInterceptor<IdTypeInterceptor>(); // Why is this method called "TryAdd" if it just adds without trying...?
}