using System.Text.Json.Serialization;

namespace StrictId.Test.Aot;

/// <summary>
/// A DTO that bundles one value from each StrictId family plus their non-generic
/// counterparts. Used to verify that System.Text.Json round-trips every shape
/// StrictId provides when run under AOT compilation.
/// </summary>
public sealed class StrictIdSmokeDto
{
	/// <summary>Closed-generic ULID id.</summary>
	public Id<User> UserId { get; set; }

	/// <summary>Closed-generic numeric id — exercises prefix + digits.</summary>
	public IdNumber<Invoice> InvoiceNumber { get; set; }

	/// <summary>Closed-generic string id with attribute-validated charset.</summary>
	public IdString<Customer> CustomerKey { get; set; }

	/// <summary>Non-generic ULID id.</summary>
	public Id BareId { get; set; }

	/// <summary>Non-generic numeric id.</summary>
	public IdNumber BareNumber { get; set; }

	/// <summary>Non-generic string id.</summary>
	public IdString BareString { get; set; }

	/// <summary>
	/// A dictionary using a StrictId as its key. Exercises the
	/// <c>WriteAsPropertyName</c> / <c>ReadAsPropertyName</c> code paths on the
	/// typed converter, which is where a latent bug would most commonly hide.
	/// </summary>
	public Dictionary<Id<Order>, string> OrderNames { get; set; } = new();
}

/// <summary>
/// System.Text.Json source-generated serialization context for <see cref="StrictIdSmokeDto"/>.
/// The STJ source generator emits a strongly-typed <see cref="JsonSerializerContext"/>
/// that does not rely on runtime reflection for member access. The smoke test uses
/// this context exclusively so any AOT-hostile code path in StrictId's JSON
/// converters surfaces cleanly during the publish step.
/// </summary>
/// <remarks>
/// The context uses default (metadata-based) generation mode rather than serialization
/// mode so <see cref="System.Text.Json.JsonSerializerOptions.Converters"/> registrations
/// from <see cref="System.Text.Json.Serialization.JsonConverterAttribute"/> on the
/// StrictId types are still honoured. In serialization mode STJ sidesteps factory
/// converters, which would bypass <see cref="StrictId.Json.IdTypedJsonConverterFactory"/>
/// and miss the whole point of the test.
/// </remarks>
[JsonSerializable(typeof(StrictIdSmokeDto))]
[JsonSerializable(typeof(Id<User>))]
[JsonSerializable(typeof(IdNumber<Invoice>))]
[JsonSerializable(typeof(IdString<Customer>))]
[JsonSerializable(typeof(Id<Order>))]
[JsonSerializable(typeof(Id))]
[JsonSerializable(typeof(IdNumber))]
[JsonSerializable(typeof(IdString))]
public partial class SmokeTestJsonContext : JsonSerializerContext;
