using System.ComponentModel;
using System.Globalization;

namespace StrictId.AspNetCore.TypeConverters;

/// <summary>
/// Base <see cref="TypeConverter"/> for StrictId types. Delegates parse/format to the
/// static members of <see cref="IStrictId{TSelf}"/>.
/// </summary>
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
