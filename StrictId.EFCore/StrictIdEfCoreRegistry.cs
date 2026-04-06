using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace StrictId.EFCore;

/// <summary>
/// Process-wide registry of pre-constructed EF Core <see cref="ValueConverter"/>
/// instances for closed StrictId types. Populated at module initialisation by the
/// source generator so that <see cref="Conventions.IdConvention"/> can resolve the
/// concrete typed converter without reflection.
/// </summary>
/// <remarks>
/// Registration must happen before the convention builds a model for the entity type.
/// Registrations added after model-build have no effect on that model.
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
