using System.Text;
using System.Text.RegularExpressions;
using StrictId.Internal;

namespace StrictId.AspNetCore.OpenApi;

/// <summary>
/// Builds the <c>pattern</c>, <c>example</c>, and <c>description</c> fields that the
/// <see cref="StrictIdSchemaTransformer"/> writes into an OpenAPI schema for a closed
/// StrictId type. Lives behind a stable internal API so the transformer can stay
/// declarative and unit-testable without reaching into metadata internals directly.
/// </summary>
/// <remarks>
/// <para>
/// The builder is shared across all three StrictId families: <see cref="Id{T}"/>,
/// <see cref="IdNumber{T}"/>, and <see cref="IdString{T}"/>. The family dictates the
/// suffix grammar — Crockford base32 for ULID, decimal digits for numeric, a charset-
/// dependent character class for string — while the prefix portion comes from the
/// shared <see cref="PrefixInfo"/> resolved by
/// <see cref="StrictIdMetadataResolver"/>.
/// </para>
/// <para>
/// Patterns use the declared canonical prefix and separator only. Even though the
/// runtime parser tolerates any <see cref="IdSeparator"/> value on input, the
/// canonical form is what clients should emit; documenting the more permissive
/// grammar would give false precision. The schema description notes that aliases and
/// alternate separators are also accepted.
/// </para>
/// </remarks>
internal static class StrictIdSchemaBuilder
{
	// ULID is stored as exactly 26 characters in Crockford base32. The alphabet is
	// [0-9A-Z] minus I, L, O, U, and the first character is 0-7 because the 128-bit
	// ULID value range tops out at 7ZZZ…. The regex accepts both cases because the
	// parser normalises.
	private const string UlidSuffixPattern = "[0-7][0-9A-HJKMNP-TV-Za-hjkmnp-tv-z]{25}";

	// ulong max is 20 decimal digits (18446744073709551615). Lower-bounded at 1 so
	// leading zeros are still legal but the empty string is not.
	private const string NumericSuffixPattern = @"\d{1,20}";

	/// <summary>
	/// Describes the schema fields to write for a single closed StrictId type.
	/// </summary>
	public readonly struct SchemaFields
	{
		public required string Pattern { get; init; }
		public required string Example { get; init; }
		public required string Description { get; init; }
	}

	/// <summary>
	/// Returns the schema fields to write for <paramref name="clrType"/> if it is one
	/// of the six StrictId shapes (three families × generic/non-generic), or
	/// <see langword="null"/> otherwise. Centralises the type-matching logic so both
	/// OpenAPI transformers (body/response and path/query parameter) share a single
	/// dispatch table.
	/// </summary>
	public static SchemaFields? TryBuildFor (Type clrType)
	{
		// Non-generic families — direct type check.
		if (clrType == typeof(Id)) return BuildForUlid(entityType: null);
		if (clrType == typeof(IdNumber)) return BuildForNumber(entityType: null);
		if (clrType == typeof(IdString)) return BuildForString(entityType: null);

		// Generic families — compare open generic definition, then peel the entity
		// type off the first (and only) type argument.
		if (!clrType.IsGenericType) return null;

		var openDefinition = clrType.GetGenericTypeDefinition();
		var entityType = clrType.GetGenericArguments()[0];

		if (openDefinition == typeof(Id<>)) return BuildForUlid(entityType);
		if (openDefinition == typeof(IdNumber<>)) return BuildForNumber(entityType);
		if (openDefinition == typeof(IdString<>)) return BuildForString(entityType);

		return null;
	}

	/// <summary>
	/// Builds the schema fields for <see cref="Id{T}"/> where <paramref name="entityType"/>
	/// is the closed phantom tag, or for the non-generic <see cref="Id"/> when
	/// <paramref name="entityType"/> is <see langword="null"/>.
	/// </summary>
	public static SchemaFields BuildForUlid (Type? entityType)
	{
		var prefix = entityType is null
			? PrefixInfo.None
			: StrictIdMetadataResolver.ResolvePrefix(entityType);

		var pattern = BuildPattern(prefix, UlidSuffixPattern);
		var example = BuildExample(prefix, Ulid.NewUlid().ToString().ToLowerInvariant());
		var description = BuildDescription(
			entityType,
			prefix,
			"a 26-character Crockford base32 ULID",
			family: "Id");

		return new SchemaFields { Pattern = pattern, Example = example, Description = description };
	}

	/// <summary>
	/// Builds the schema fields for <see cref="IdNumber{T}"/> or the non-generic
	/// <see cref="IdNumber"/> (when <paramref name="entityType"/> is <see langword="null"/>).
	/// </summary>
	public static SchemaFields BuildForNumber (Type? entityType)
	{
		var prefix = entityType is null
			? PrefixInfo.None
			: StrictIdMetadataResolver.ResolvePrefix(entityType);

		var pattern = BuildPattern(prefix, NumericSuffixPattern);
		var example = BuildExample(prefix, "42");
		var description = BuildDescription(
			entityType,
			prefix,
			"a decimal unsigned 64-bit integer",
			family: "IdNumber");

		return new SchemaFields { Pattern = pattern, Example = example, Description = description };
	}

	/// <summary>
	/// Builds the schema fields for <see cref="IdString{T}"/> or the non-generic
	/// <see cref="IdString"/> (when <paramref name="entityType"/> is <see langword="null"/>).
	/// </summary>
	public static SchemaFields BuildForString (Type? entityType)
	{
		var prefix = entityType is null
			? PrefixInfo.None
			: StrictIdMetadataResolver.ResolvePrefix(entityType);
		var options = entityType is null
			? IdStringOptions.Default
			: StrictIdMetadataResolver.ResolveStringOptions(entityType);

		var suffixPattern = BuildStringSuffixPattern(options);
		var pattern = BuildPattern(prefix, suffixPattern);
		var example = BuildExample(prefix, BuildStringExample(options));
		var description = BuildDescription(
			entityType,
			prefix,
			$"an opaque string suffix (max {options.MaxLength} chars, charset {options.CharSet})",
			family: "IdString");

		return new SchemaFields { Pattern = pattern, Example = example, Description = description };
	}

	// ═════ Shared helpers ════════════════════════════════════════════════════

	/// <summary>
	/// Wraps a suffix pattern with an optional canonical-prefix + separator segment.
	/// The prefix segment is emitted only when the type declares a prefix; otherwise
	/// the pattern is the suffix alone, which matches the runtime parser that rejects
	/// any prefix text on a non-prefixed type.
	/// </summary>
	private static string BuildPattern (PrefixInfo prefix, string suffixPattern)
	{
		if (!prefix.HasPrefix)
			return $"^{suffixPattern}$";

		var separator = Regex.Escape(prefix.Separator.ToChar().ToString());
		// Only the canonical prefix is emitted. Aliases are documented in the schema
		// description — keeping the pattern narrow avoids consumers over-validating on
		// the alias list when the canonical form is what servers emit.
		var canonical = Regex.Escape(prefix.Canonical!);
		return $"^{canonical}{separator}{suffixPattern}$";
	}

	/// <summary>
	/// Builds an example value by joining the canonical prefix (if any) with a
	/// family-appropriate suffix sample.
	/// </summary>
	private static string BuildExample (PrefixInfo prefix, string suffixSample)
	{
		if (!prefix.HasPrefix) return suffixSample;
		return $"{prefix.Canonical}{prefix.Separator.ToChar()}{suffixSample}";
	}

	/// <summary>
	/// Builds the human-readable description that explains the schema's structure,
	/// lists the accepted aliases, and notes the separator tolerance of the parser.
	/// </summary>
	private static string BuildDescription (Type? entityType, PrefixInfo prefix, string suffixDescription, string family)
	{
		var sb = new StringBuilder(160);
		var typeDisplay = entityType is null ? family : $"{family}<{entityType.Name}>";
		sb.Append("StrictId ").Append(typeDisplay).Append(". ");

		if (prefix.HasPrefix)
		{
			sb.Append("Format: '").Append(prefix.Canonical).Append(prefix.Separator.ToChar())
				.Append("<suffix>' where <suffix> is ").Append(suffixDescription).Append('.');

			if (prefix.Aliases.Length > 1)
			{
				sb.Append(" Accepted prefix aliases on input: ");
				for (var i = 0; i < prefix.Aliases.Length; i++)
				{
					if (i > 0) sb.Append(", ");
					sb.Append('\'').Append(prefix.Aliases[i]).Append('\'');
				}
				sb.Append('.');
			}

			sb.Append(" The parser also tolerates any IdSeparator character on input (_ / . :).");
		}
		else
		{
			sb.Append("Format: ").Append(suffixDescription).Append('.');
		}

		return sb.ToString();
	}

	// ═════ IdString-specific helpers ═════════════════════════════════════════

	private static string BuildStringSuffixPattern (IdStringOptions options)
	{
		var charClass = options.CharSet switch
		{
			IdStringCharSet.Alphanumeric => "[A-Za-z0-9]",
			IdStringCharSet.AlphanumericDash => "[A-Za-z0-9-]",
			IdStringCharSet.AlphanumericUnderscore => "[A-Za-z0-9_]",
			// Any: non-whitespace, non-separator characters. The parser's
			// IdStringValidator.IsPrintableNonSeparator also rejects control chars
			// and the four IdSeparator glyphs; we encode that directly here.
			IdStringCharSet.Any => @"[^\s_/.:]",
			_ => "[A-Za-z0-9]", // defensive default; enum exhaustiveness enforced in core
		};

		return $"{charClass}{{1,{options.MaxLength}}}";
	}

	private static string BuildStringExample (IdStringOptions options)
	{
		// Sample strings chosen to be valid against every charset variant so the
		// example stays in range for even the strictest rule. MaxLength is almost
		// always > 6 in real use; if it isn't, truncate.
		const string sample = "abc123";
		return options.MaxLength < sample.Length
			? sample[..options.MaxLength]
			: sample;
	}
}
