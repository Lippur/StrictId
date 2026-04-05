using System.ComponentModel;
using FluentAssertions;
using StrictId.AspNetCore.TypeConverters;

namespace StrictId.AspNetCore.Test;

/// <summary>
/// Exercises the <see cref="System.ComponentModel.TypeConverter"/> subclasses and the
/// registration extensions that attach them to every known StrictId closed generic.
/// These tests operate directly on <see cref="TypeDescriptor"/> without spinning up a
/// host — the converters are orthogonal to routing and binding.
/// </summary>
/// <remarks>
/// Each closed generic is used only once per fixture so
/// <see cref="TypeDescriptor.AddAttributes(Type, System.Attribute[])"/> mutations do
/// not bleed across tests. The fixtures use dedicated marker types
/// (<c>TypeConverterUser</c> etc.) that are not referenced by any other fixture, so
/// the global <see cref="TypeDescriptor"/> table stays clean for the rest of the
/// suite.
/// </remarks>
[TestFixture]
public class TypeConverterTests
{
	[IdPrefix("tcuser")]
	private sealed class TypeConverterUser;

	[IdPrefix("tcinv")]
	private sealed class TypeConverterInvoice;

	[IdPrefix("tccus")]
	private sealed class TypeConverterCustomer;

	[Test]
	public void IdTypeConverter_RoundTripsNonGenericId ()
	{
		var converter = new IdTypeConverter();
		var original = Id.NewId();

		var serialized = converter.ConvertToString(original);
		serialized.Should().NotBeNull();

		var parsed = (Id)converter.ConvertFromString(serialized!)!;
		parsed.Should().Be(original);
	}

	[Test]
	public void IdNumberTypeConverter_RoundTripsNonGenericIdNumber ()
	{
		var converter = new IdNumberTypeConverter();
		var original = new IdNumber(12345UL);

		var serialized = converter.ConvertToString(original);
		var parsed = (IdNumber)converter.ConvertFromString(serialized!)!;
		parsed.Should().Be(original);
	}

	[Test]
	public void IdStringTypeConverter_RoundTripsNonGenericIdString ()
	{
		var converter = new IdStringTypeConverter();
		var original = new IdString("abc-123");

		var serialized = converter.ConvertToString(original);
		var parsed = (IdString)converter.ConvertFromString(serialized!)!;
		parsed.Should().Be(original);
	}

	[Test]
	public void IdTypeConverterOfT_ReadsPrefixedForm ()
	{
		var converter = new IdTypeConverter<TypeConverterUser>();
		var id = Id<TypeConverterUser>.NewId();

		var text = id.ToString();
		text.Should().StartWith("tcuser_");

		var parsed = (Id<TypeConverterUser>)converter.ConvertFromString(text)!;
		parsed.Should().Be(id);
	}

	[Test]
	public void IdTypeConverterOfT_RejectsWrongPrefix ()
	{
		var converter = new IdTypeConverter<TypeConverterUser>();
		var wrong = $"tccus_{Ulid.NewUlid().ToString().ToLowerInvariant()}";

		var act = () => converter.ConvertFromString(wrong);
		act.Should().Throw<FormatException>();
	}

	[Test]
	public void AddStrictIdTypeConverter_InstallsRegistrationForClosedGeneric ()
	{
		var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
		services.AddStrictIdTypeConverter<TypeConverterInvoice>();

		// Even though AddStrictIdTypeConverter calls into TypeDescriptor statically and
		// doesn't add anything to the service collection itself, the side-effect is the
		// global attribute registration. Verify that GetConverter hands back our type.
		var converter = TypeDescriptor.GetConverter(typeof(IdNumber<TypeConverterInvoice>));
		converter.Should().BeOfType<IdNumberTypeConverter<TypeConverterInvoice>>();
	}

	[Test]
	public void NonGenericConverters_CanConvertFromAndTo_String ()
	{
		var idConv = new IdTypeConverter();
		var numConv = new IdNumberTypeConverter();
		var strConv = new IdStringTypeConverter();

		idConv.CanConvertFrom(typeof(string)).Should().BeTrue();
		idConv.CanConvertTo(typeof(string)).Should().BeTrue();

		numConv.CanConvertFrom(typeof(string)).Should().BeTrue();
		numConv.CanConvertTo(typeof(string)).Should().BeTrue();

		strConv.CanConvertFrom(typeof(string)).Should().BeTrue();
		strConv.CanConvertTo(typeof(string)).Should().BeTrue();
	}

	[Test]
	public void IdStringTypeConverterOfT_HonoursAttributeValidation ()
	{
		var converter = new IdStringTypeConverter<TypeConverterCustomer>();

		// Valid alphanumeric input accepted via the TCCus prefix.
		var valid = converter.ConvertFromString("tccus_abc123");
		valid.Should().BeOfType<IdString<TypeConverterCustomer>>();

		// Whitespace is rejected by IdString validation even though the overall shape
		// is otherwise valid — this confirms the converter delegates to the full
		// IStrictId parse pipeline, not a relaxed shape check.
		var invalidAct = () => converter.ConvertFromString("tccus_abc 123");
		invalidAct.Should().Throw<FormatException>();
	}
}
