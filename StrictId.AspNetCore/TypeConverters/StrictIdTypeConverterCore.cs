using System.ComponentModel;
using System.Globalization;

namespace StrictId.AspNetCore.TypeConverters;

/// <summary>
/// Shared logic for all StrictId <see cref="TypeConverter"/> subclasses. Centralises
/// the <c>string ⇄ IStrictId</c> conversion by delegating parse/format to the static
/// members of <see cref="IStrictId{TSelf}"/> so each family's concrete converter is a
/// one-line pass-through to the appropriate closed generic.
/// </summary>
/// <remarks>
/// <para>
/// StrictId values are already parseable via <see cref="ISpanParsable{TSelf}"/> on
/// .NET 7+, which is the path used by ASP.NET Core model binding in modern apps. This
/// <see cref="TypeConverter"/> exists strictly for legacy surfaces that predate
/// <see cref="IParsable{TSelf}"/>: XAML, WPF/WinForms designers, <see cref="TypeDescriptor"/>-
/// driven serializers, the obsolete <c>System.Configuration</c> binding path, and
/// third-party libraries that probe for a converter via <see cref="TypeConverterAttribute"/>
/// before trying anything else.
/// </para>
/// <para>
/// Converters are stateless singletons — instantiation is free and the cached
/// <see cref="TypeConverterAttribute"/> registration hands out the same instance to
/// every call site.
/// </para>
/// </remarks>
/// <typeparam name="T">The concrete StrictId value type this converter handles.</typeparam>
public abstract class StrictIdTypeConverter<T> : TypeConverter
	where T : struct, IStrictId<T>
{
	/// <inheritdoc />
	public override bool CanConvertFrom (ITypeDescriptorContext? context, Type sourceType)
		=> sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

	/// <inheritdoc />
	public override bool CanConvertTo (ITypeDescriptorContext? context, Type? destinationType)
		=> destinationType == typeof(string) || base.CanConvertTo(context, destinationType);

	/// <inheritdoc />
	public override object? ConvertFrom (ITypeDescriptorContext? context, CultureInfo? culture, object value)
	{
		if (value is string s)
		{
			if (T.TryParse(s, provider: null, out var parsed))
				return parsed;

			// Re-raise via Parse so the caller gets the full diagnostic message built
			// by the family-specific parser — TryParse discards it and a converter that
			// threw a bare InvalidOperationException would bury useful context.
			return T.Parse(s, provider: null);
		}

		return base.ConvertFrom(context, culture, value);
	}

	/// <inheritdoc />
	public override object? ConvertTo (
		ITypeDescriptorContext? context,
		CultureInfo? culture,
		object? value,
		Type destinationType
	)
	{
		if (destinationType == typeof(string) && value is T id)
			return id.ToString();

		return base.ConvertTo(context, culture, value, destinationType);
	}
}
