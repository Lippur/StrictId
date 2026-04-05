using System.Collections.Concurrent;
using System.Text.Json.Serialization;
using StrictId.Internal;

namespace StrictId;

/// <summary>
/// Process-wide registry of pre-resolved StrictId metadata. The StrictId source
/// generator emits calls into this registry from a <c>[ModuleInitializer]</c> so that
/// <see cref="Id{T}"/>, <see cref="IdNumber{T}"/>, and <see cref="IdString{T}"/> can
/// reconstitute their prefix / string-option metadata without walking user-code
/// attributes via reflection. The reflection path in <c>StrictIdMetadataResolver</c>
/// is preserved as a fallback so the library still works correctly when the generator
/// is disabled via <c>&lt;EnableStrictIdSourceGenerator&gt;false&lt;/EnableStrictIdSourceGenerator&gt;</c>.
/// </summary>
/// <remarks>
/// <para>
/// Manual use is permitted but discouraged — the generator is the intended producer.
/// Calling a register method after the type's <c>StrictIdMetadata&lt;T&gt;</c> static
/// constructor has already run has no effect on that type's cached <c>Prefix</c>
/// field; registration must happen at module initialisation time to be observable.
/// </para>
/// </remarks>
public static class StrictIdRegistry
{
	private static readonly ConcurrentDictionary<Type, PrefixInfo> PrefixRegistry = new();
	private static readonly ConcurrentDictionary<Type, IdStringOptions> StringOptionsRegistry = new();
	private static readonly ConcurrentDictionary<Type, JsonConverter> JsonConverterRegistry = new();

	/// <summary>
	/// Registers the prefix metadata for <typeparamref name="T"/>. The canonical prefix
	/// is written to position 0 of the alias array; any remaining aliases follow in
	/// declaration order. When <paramref name="canonical"/> is <see langword="null"/>
	/// the registration is for a type with only an <c>[IdSeparator]</c> override and no
	/// <c>[IdPrefix]</c> declaration.
	/// </summary>
	/// <typeparam name="T">The entity type whose metadata is being registered.</typeparam>
	/// <param name="canonical">The canonical prefix text, or <see langword="null"/> if the type declares no prefix.</param>
	/// <param name="aliases">
	/// All accepted prefixes for parse, canonical first then aliases in declaration
	/// order. Must be empty when <paramref name="canonical"/> is <see langword="null"/>.
	/// Must start with <paramref name="canonical"/> when it is non-<see langword="null"/>.
	/// </param>
	/// <param name="separator">The separator declared by <c>[IdSeparator]</c>, or <see cref="IdSeparator.Underscore"/> by default.</param>
	public static void RegisterPrefix<T> (string? canonical, string[] aliases, IdSeparator separator)
	{
		PrefixRegistry[typeof(T)] = new PrefixInfo
		{
			Canonical = canonical,
			Aliases = aliases,
			Separator = separator,
		};
	}

	/// <summary>
	/// Registers the <see cref="IdStringAttribute"/> options for <typeparamref name="T"/>.
	/// Used only by <see cref="IdString{T}"/>; other families ignore this call.
	/// </summary>
	/// <typeparam name="T">The entity type whose string options are being registered.</typeparam>
	/// <param name="maxLength">Maximum permitted suffix length. Must match <see cref="IdStringAttribute.MaxLength"/>.</param>
	/// <param name="charSet">The character set constraint. Must match <see cref="IdStringAttribute.CharSet"/>.</param>
	/// <param name="ignoreCase"><see langword="true"/> for case-insensitive comparison.</param>
	public static void RegisterStringOptions<T> (int maxLength, IdStringCharSet charSet, bool ignoreCase)
	{
		StringOptionsRegistry[typeof(T)] = new IdStringOptions
		{
			MaxLength = maxLength,
			CharSet = charSet,
			IgnoreCase = ignoreCase,
		};
	}

	/// <summary>
	/// Registers a concrete <see cref="JsonConverter{T}"/> for the closed StrictId type
	/// <typeparamref name="TId"/> — typically <see cref="Id{T}"/>, <see cref="IdNumber{T}"/>,
	/// or <see cref="IdString{T}"/>. The StrictId typed-JSON factories look this up before
	/// falling back to <see cref="Type.MakeGenericType(Type[])"/>, so a pre-registered
	/// converter keeps hot-path serialisation AOT- and trim-safe. The generator emits one
	/// of these calls per family for every <c>[IdPrefix]</c>-decorated type.
	/// </summary>
	/// <typeparam name="TId">The closed StrictId type the converter serialises.</typeparam>
	/// <param name="converter">The converter instance. Must not be <see langword="null"/>.</param>
	public static void RegisterJsonConverter<TId> (JsonConverter<TId> converter)
		where TId : struct
	{
		JsonConverterRegistry[typeof(TId)] = converter;
	}

	internal static bool TryGetPrefix (Type type, out PrefixInfo info) =>
		PrefixRegistry.TryGetValue(type, out info);

	internal static bool TryGetStringOptions (Type type, out IdStringOptions options) =>
		StringOptionsRegistry.TryGetValue(type, out options);

	internal static bool TryGetJsonConverter (Type type, out JsonConverter? converter)
	{
		if (JsonConverterRegistry.TryGetValue(type, out var found))
		{
			converter = found;
			return true;
		}
		converter = null;
		return false;
	}

	/// <summary>
	/// Enumerates every entity type currently registered in the prefix table. Used by
	/// <c>StrictId.AspNetCore</c> to walk the set of source-gen-visible entities when
	/// wiring legacy <see cref="System.ComponentModel.TypeConverter"/> attributes, since
	/// those attributes have to be attached per closed generic and the registry is the
	/// authoritative list of those closings.
	/// </summary>
	internal static IEnumerable<Type> EnumerateRegisteredEntityTypes () => PrefixRegistry.Keys;
}
