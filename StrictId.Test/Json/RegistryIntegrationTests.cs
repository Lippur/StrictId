using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using StrictId.Json;

namespace StrictId.Test.Json;

/// <summary>
/// Tests that the typed JSON converter factories (<see cref="IdTypedJsonConverterFactory"/>,
/// <see cref="IdNumberTypedJsonConverterFactory"/>, <see cref="IdStringTypedJsonConverterFactory"/>)
/// consult <see cref="StrictIdRegistry"/> before falling back to reflection. This is the
/// runtime half of the Phase 8 AOT cut-over; the source generator populates the registry
/// at module init, and these tests simulate that by registering a converter instance
/// directly and asserting the factory returns it verbatim.
/// </summary>
[TestFixture]
public class RegistryIntegrationTests
{
	private class RegisteredId;
	private class RegisteredIdNumber;
	private class RegisteredIdString;

	// The typed converters are sealed, so sentinel "subtype" markers aren't an option.
	// Instead, each test registers a concrete converter instance and then asserts the
	// factory returns the SAME reference — proving that the registry is consulted before
	// the reflection fallback (which would always allocate a fresh instance).

	[Test]
	public void IdTypedJsonConverterFactory_ReturnsRegisteredInstance ()
	{
		var registered = new IdTypedJsonConverter<RegisteredId>();
		StrictIdRegistry.RegisterJsonConverter<Id<RegisteredId>>(registered);

		var factory = new IdTypedJsonConverterFactory();
		var produced = factory.CreateConverter(typeof(Id<RegisteredId>), new JsonSerializerOptions());

		produced.Should().BeSameAs(registered);
	}

	[Test]
	public void IdNumberTypedJsonConverterFactory_ReturnsRegisteredInstance ()
	{
		var registered = new IdNumberTypedJsonConverter<RegisteredIdNumber>();
		StrictIdRegistry.RegisterJsonConverter<IdNumber<RegisteredIdNumber>>(registered);

		var factory = new IdNumberTypedJsonConverterFactory();
		var produced = factory.CreateConverter(typeof(IdNumber<RegisteredIdNumber>), new JsonSerializerOptions());

		produced.Should().BeSameAs(registered);
	}

	[Test]
	public void IdStringTypedJsonConverterFactory_ReturnsRegisteredInstance ()
	{
		var registered = new IdStringTypedJsonConverter<RegisteredIdString>();
		StrictIdRegistry.RegisterJsonConverter<IdString<RegisteredIdString>>(registered);

		var factory = new IdStringTypedJsonConverterFactory();
		var produced = factory.CreateConverter(typeof(IdString<RegisteredIdString>), new JsonSerializerOptions());

		produced.Should().BeSameAs(registered);
	}

	[Test]
	public void IdTypedJsonConverterFactory_FallsBackToReflectionOnMiss ()
	{
		// No registration for this type; the factory must still return a working
		// converter via the reflection fallback path.
		var factory = new IdTypedJsonConverterFactory();
		var produced = factory.CreateConverter(typeof(Id<UnknownTypeSentinel>), new JsonSerializerOptions());

		produced.Should().NotBeNull();
		produced.Should().BeOfType<IdTypedJsonConverter<UnknownTypeSentinel>>();
	}

	private class UnknownTypeSentinel;
}
