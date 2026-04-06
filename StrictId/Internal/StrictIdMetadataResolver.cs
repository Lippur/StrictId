namespace StrictId.Internal;

/// <summary>
/// Reflection-based resolver for StrictId prefix, separator, and string-option metadata.
/// Walks a type's inheritance chain: the first type in the chain (starting from the leaf)
/// that declares the attribute wins, and base-type declarations are hidden rather than
/// merged. Used as a fallback when the source generator did not pre-populate
/// <see cref="StrictIdRegistry"/> for a given type.
/// </summary>
internal static class StrictIdMetadataResolver
{
	/// <summary>
	/// Resolves the prefix and separator metadata for the given entity <paramref name="type"/>.
	/// Consults <see cref="StrictIdRegistry"/> first; on miss, walks the type's attributes
	/// via reflection and validates prefix grammar, default cardinality, and uniqueness.
	/// </summary>
	/// <param name="type">The entity type to resolve.</param>
	/// <returns>The resolved <see cref="PrefixInfo"/>. Never <see langword="null"/>.</returns>
	/// <exception cref="InvalidOperationException">
	/// The attribute declarations on <paramref name="type"/> are malformed (invalid grammar,
	/// missing or multiple defaults, or duplicate prefixes).
	/// </exception>
	public static PrefixInfo ResolvePrefix (Type type)
	{
		if (StrictIdRegistry.TryGetPrefix(type, out var registered))
			return registered;

		var (prefixAttrs, separatorAttr) = WalkPrefixAndSeparator(type);
		var separator = separatorAttr?.Separator ?? IdSeparator.Underscore;

		if (prefixAttrs.Length == 0)
		{
			return new PrefixInfo
			{
				Canonical = null,
				Aliases = [],
				Separator = separator,
			};
		}

		foreach (var attr in prefixAttrs)
			ValidateGrammar(type, attr.Prefix);

		var seen = new HashSet<string>(StringComparer.Ordinal);
		foreach (var attr in prefixAttrs)
		{
			if (!seen.Add(attr.Prefix))
				throw DuplicatePrefixException(type, attr.Prefix);
		}

		string canonical;
		if (prefixAttrs.Length == 1)
		{
			canonical = prefixAttrs[0].Prefix;
		}
		else
		{
			var defaults = prefixAttrs.Where(a => a.IsDefault).ToArray();
			if (defaults.Length == 0)
				throw NoDefaultException(type, prefixAttrs);
			if (defaults.Length > 1)
				throw MultipleDefaultsException(type, defaults);
			canonical = defaults[0].Prefix;
		}

		// Canonical first, then remaining aliases in declaration order.
		var aliases = new string[prefixAttrs.Length];
		aliases[0] = canonical;
		var idx = 1;
		foreach (var attr in prefixAttrs)
		{
			if (attr.Prefix != canonical)
				aliases[idx++] = attr.Prefix;
		}

		return new PrefixInfo
		{
			Canonical = canonical,
			Aliases = aliases,
			Separator = separator,
		};
	}

	/// <summary>
	/// Resolves the <see cref="IdStringOptions"/> for the given entity <paramref name="type"/>.
	/// Consults <see cref="StrictIdRegistry"/> first; on miss, walks the inheritance chain
	/// for the first <see cref="IdStringAttribute"/> declaration. Returns
	/// <see cref="IdStringOptions.Default"/> if none is found.
	/// </summary>
	public static IdStringOptions ResolveStringOptions (Type type)
	{
		if (StrictIdRegistry.TryGetStringOptions(type, out var registered))
			return registered;

		Type? current = type;
		while (current is not null)
		{
			var attrs = (IdStringAttribute[])current.GetCustomAttributes(typeof(IdStringAttribute), inherit: false);
			if (attrs.Length > 0)
			{
				var attr = attrs[0];
				return new IdStringOptions
				{
					MaxLength = attr.MaxLength,
					CharSet = attr.CharSet,
					IgnoreCase = attr.IgnoreCase,
				};
			}
			current = current.BaseType;
		}
		return IdStringOptions.Default;
	}

	/// <summary>
	/// Walks the inheritance chain of <paramref name="start"/> looking for
	/// <see cref="IdPrefixAttribute"/> and <see cref="IdSeparatorAttribute"/> declarations.
	/// Each attribute is resolved independently: the first type in the chain that declares
	/// it wins, and its declarations fully replace any found on further-up base types.
	/// When no type in the chain declares <see cref="IdSeparatorAttribute"/>, the entity
	/// type's declaring assembly is checked for an assembly-level <c>[assembly: IdSeparator]</c>
	/// before falling back to the built-in default (<see cref="IdSeparator.Underscore"/>).
	/// </summary>
	private static (IdPrefixAttribute[] prefixes, IdSeparatorAttribute? separator) WalkPrefixAndSeparator (Type start)
	{
		IdPrefixAttribute[]? prefixes = null;
		IdSeparatorAttribute? separator = null;

		Type? current = start;
		while (current is not null)
		{
			if (prefixes is null)
			{
				var local = (IdPrefixAttribute[])current.GetCustomAttributes(typeof(IdPrefixAttribute), inherit: false);
				if (local.Length > 0) prefixes = local;
			}

			if (separator is null)
			{
				var localSeps = (IdSeparatorAttribute[])current.GetCustomAttributes(typeof(IdSeparatorAttribute), inherit: false);
				if (localSeps.Length > 0) separator = localSeps[0];
			}

			if (prefixes is not null && separator is not null) break;

			current = current.BaseType;
		}

		// Assembly-level fallback: if no type in the chain declared [IdSeparator],
		// check the entity type's declaring assembly for [assembly: IdSeparator(...)].
		if (separator is null)
		{
			var assemblySeps = (IdSeparatorAttribute[])start.Assembly.GetCustomAttributes(typeof(IdSeparatorAttribute), inherit: false);
			if (assemblySeps.Length > 0) separator = assemblySeps[0];
		}

		return (prefixes ?? [], separator);
	}

	private static void ValidateGrammar (Type type, string prefix)
	{
		if (string.IsNullOrEmpty(prefix))
		{
			throw new InvalidOperationException(
				$"Invalid [IdPrefix] on type '{FormatTypeName(type)}': prefix is empty. " +
				"Prefixes must match ^[a-z][a-z0-9_]{0,62}$.");
		}

		if (prefix.Length > 63)
		{
			throw new InvalidOperationException(
				$"Invalid [IdPrefix] on type '{FormatTypeName(type)}': prefix '{prefix}' is {prefix.Length} characters long. " +
				"Prefixes may be at most 63 characters.");
		}

		var first = prefix[0];
		if (first is < 'a' or > 'z')
		{
			throw new InvalidOperationException(
				$"Invalid [IdPrefix] on type '{FormatTypeName(type)}': prefix '{prefix}' starts with '{first}'. " +
				"Prefixes must start with a lowercase ASCII letter (a-z).");
		}

		for (var i = 1; i < prefix.Length; i++)
		{
			var c = prefix[i];
			if (c is >= 'a' and <= 'z' or >= '0' and <= '9' or '_')
				continue;

			throw new InvalidOperationException(
				$"Invalid [IdPrefix] on type '{FormatTypeName(type)}': prefix '{prefix}' contains '{c}' at position {i}. " +
				"After the first character, only lowercase ASCII letters, digits, and underscore are allowed.");
		}
	}

	private static InvalidOperationException NoDefaultException (Type type, IdPrefixAttribute[] prefixes)
	{
		var quoted = string.Join(", ", prefixes.Select(p => $"'{p.Prefix}'"));
		return new InvalidOperationException(
			$"Invalid [IdPrefix] on type '{FormatTypeName(type)}': type declares {prefixes.Length} [IdPrefix] attributes ({quoted}), " +
			"but none is marked IsDefault = true. When more than one prefix is declared, exactly one must be the canonical default.");
	}

	private static InvalidOperationException MultipleDefaultsException (Type type, IdPrefixAttribute[] defaults)
	{
		var quoted = string.Join(", ", defaults.Select(p => $"'{p.Prefix}'"));
		return new InvalidOperationException(
			$"Invalid [IdPrefix] on type '{FormatTypeName(type)}': type declares {defaults.Length} [IdPrefix] attributes marked IsDefault = true ({quoted}). " +
			"Exactly one prefix may be the canonical default.");
	}

	private static InvalidOperationException DuplicatePrefixException (Type type, string prefix) =>
		new(
			$"Invalid [IdPrefix] on type '{FormatTypeName(type)}': prefix '{prefix}' is declared more than once. " +
			"Prefixes must be unique within a type.");

	private static string FormatTypeName (Type type) => type.FullName ?? type.Name;
}
