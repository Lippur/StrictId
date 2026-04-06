using FluentAssertions;

namespace StrictId.Test.Attributes;

[TestFixture]
public class IdSeparatorAttributeTests
{
	[Test]
	public void ConstructsWithSeparator ()
	{
		var attr = new IdSeparatorAttribute(IdSeparator.Colon);

		attr.Separator.Should().Be(IdSeparator.Colon);
	}

	[IdSeparator(IdSeparator.Slash)]
	private class SlashEntity;

	private class DerivedSlashEntity : SlashEntity;

	[Test]
	public void IsInheritedFromBaseType ()
	{
		var attr = typeof(DerivedSlashEntity)
			.GetCustomAttributes(typeof(IdSeparatorAttribute), inherit: true)
			.Cast<IdSeparatorAttribute>()
			.Single();

		attr.Separator.Should().Be(IdSeparator.Slash);
	}

	[Test]
	public void AttributeUsageAllowsAssemblyTarget ()
	{
		var usage = typeof(IdSeparatorAttribute)
			.GetCustomAttributes(typeof(AttributeUsageAttribute), inherit: false)
			.Cast<AttributeUsageAttribute>()
			.Single();

		usage.ValidOn.Should().HaveFlag(AttributeTargets.Assembly);
	}
}
