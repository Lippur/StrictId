using FluentAssertions;

namespace StrictId.Test.Attributes;

[TestFixture]
public class IdPrefixAttributeTests
{
	[Test]
	public void ConstructsWithPrefix ()
	{
		var attr = new IdPrefixAttribute("user");

		attr.Prefix.Should().Be("user");
		attr.IsDefault.Should().BeFalse();
	}

	[Test]
	public void SupportsIsDefaultAsNamedArgument ()
	{
		var attr = new IdPrefixAttribute("user") { IsDefault = true };

		attr.IsDefault.Should().BeTrue();
	}

	[IdPrefix("primary", IsDefault = true)]
	[IdPrefix("secondary")]
	private class MultiPrefixed;

	[Test]
	public void AllowsMultiplePrefixesOnASingleType ()
	{
		var attrs = typeof(MultiPrefixed)
			.GetCustomAttributes(typeof(IdPrefixAttribute), inherit: false)
			.Cast<IdPrefixAttribute>()
			.OrderBy(a => a.Prefix, StringComparer.Ordinal)
			.ToArray();

		attrs.Should().HaveCount(2);
		attrs[0].Prefix.Should().Be("primary");
		attrs[0].IsDefault.Should().BeTrue();
		attrs[1].Prefix.Should().Be("secondary");
		attrs[1].IsDefault.Should().BeFalse();
	}

	[IdPrefix("base")]
	private class BaseEntity;

	private class DerivedEntity : BaseEntity;

	[Test]
	public void IsInheritedFromBaseType ()
	{
		var attrs = typeof(DerivedEntity)
			.GetCustomAttributes(typeof(IdPrefixAttribute), inherit: true)
			.Cast<IdPrefixAttribute>()
			.ToArray();

		attrs.Should().HaveCount(1);
		attrs[0].Prefix.Should().Be("base");
	}

	[IdPrefix("widget")]
	private struct WidgetMarker;

	[Test]
	public void CanBeAppliedToStructs ()
	{
		var attr = typeof(WidgetMarker)
			.GetCustomAttributes(typeof(IdPrefixAttribute), inherit: false)
			.Cast<IdPrefixAttribute>()
			.Single();

		attr.Prefix.Should().Be("widget");
	}
}
