using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace StrictId.EFCore;

/// <summary>
/// Process-wide registry of pre-constructed EF Core <see cref="ValueConverter"/>
/// instances for closed StrictId types. The StrictId source generator emits calls into
/// this registry from a <c>[ModuleInitializer]</c> for every <c>[IdPrefix]</c>-decorated
/// type, so that <see cref="Conventions.IdConvention"/> can reach the concrete typed
/// converter without closing an open generic via <see cref="Type.MakeGenericType(Type[])"/>
/// on model-build.
/// </summary>
/// <remarks>
/// <para>
/// The registry lives in <c>StrictId.EFCore</c> rather than in the core package because
/// <see cref="ValueConverter"/> is defined in EF Core. Consumers who don't reference
/// <c>StrictId.EFCore</c> don't pay for this type; the generator detects whether the
/// consumer's compilation references EF Core and emits registrations only when it does.
/// </para>
/// <para>
/// Manual use is permitted but discouraged — the generator is the intended producer.
/// Calling <see cref="RegisterValueConverter{TId}"/> after the convention has already
/// built a model for the entity type has no effect on that model.
/// </para>
/// </remarks>
public static class StrictIdEfCoreRegistry
{
	private static readonly ConcurrentDictionary<Type, ValueConverter> ConverterRegistry = new();

	/// <summary>
	/// Registers a concrete EF Core <see cref="ValueConverter"/> for the closed StrictId
	/// type <typeparamref name="TId"/>.
	/// </summary>
	/// <typeparam name="TId">The closed StrictId type the converter maps.</typeparam>
	/// <param name="converter">The value converter instance. Must not be <see langword="null"/>.</param>
	public static void RegisterValueConverter<TId> (ValueConverter converter)
		where TId : struct
	{
		ConverterRegistry[typeof(TId)] = converter;
	}

	internal static bool TryGetValueConverter (Type type, out ValueConverter? converter)
	{
		if (ConverterRegistry.TryGetValue(type, out var found))
		{
			converter = found;
			return true;
		}
		converter = null;
		return false;
	}
}
