using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace StrictId.AspNetCore.Test;

/// <summary>
/// Builds a minimal, TestServer-backed <see cref="WebApplication"/> for integration
/// testing StrictId's ASP.NET Core integration. Each test fixture builds its own host
/// with exactly the endpoints it needs, keeping the individual tests self-contained
/// and obvious.
/// </summary>
/// <remarks>
/// <para>
/// Uses <see cref="WebApplicationBuilder"/> rather than
/// <see cref="Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory{TEntryPoint}"/>
/// because there is no single shared Program class — each feature exercises a
/// different pipeline. The <see cref="WebHostBuilderExtensions.UseTestServer(IWebHostBuilder)"/>
/// hook swaps out Kestrel so requests travel in-memory and no real port binding is
/// performed.
/// </para>
/// </remarks>
internal static class TestHostBuilder
{
	/// <summary>
	/// Builds and starts a TestServer-backed <see cref="WebApplication"/> configured
	/// by the supplied delegates. Returns the started app; caller must dispose it
	/// (usually via an NUnit <c>TearDown</c> or a <c>using</c>).
	/// </summary>
	/// <param name="configureServices">Extra service-collection configuration — StrictId extensions, options, filters.</param>
	/// <param name="configurePipeline">Endpoint wiring: calls to <c>app.MapGet</c>, middleware, and friends.</param>
	public static async Task<WebApplication> StartAsync (
		Action<IServiceCollection>? configureServices = null,
		Action<WebApplication>? configurePipeline = null
	)
	{
		var builder = WebApplication.CreateBuilder();
		builder.WebHost.UseTestServer();
		configureServices?.Invoke(builder.Services);

		var app = builder.Build();
		configurePipeline?.Invoke(app);

		await app.StartAsync();
		return app;
	}

	/// <summary>
	/// Obtains an <see cref="HttpClient"/> bound to the TestServer inside
	/// <paramref name="app"/>. Each call returns a fresh client sharing the same
	/// underlying test handler.
	/// </summary>
	public static HttpClient CreateClient (this WebApplication app)
		=> app.GetTestServer().CreateClient();
}
