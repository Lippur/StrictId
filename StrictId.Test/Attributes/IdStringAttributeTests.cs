using FluentAssertions;

namespace StrictId.Test.Attributes;

[TestFixture]
public class IdStringAttributeTests
{
	[Test]
	public void HasExpectedDefaults ()
	{
		var attr = new IdStringAttribute();

		attr.MaxLength.Should().Be(255);
		attr.CharSet.Should().Be(IdStringCharSet.AlphanumericDashUnderscore);
		attr.IgnoreCase.Should().BeFalse();
	}

	[Test]
	public void SupportsNamedArgumentInitialization ()
	{
		var attr = new IdStringAttribute
		{
			MaxLength = 64,
			CharSet = IdStringCharSet.AlphanumericDash,
			IgnoreCase = true,
		};

		attr.MaxLength.Should().Be(64);
		attr.CharSet.Should().Be(IdStringCharSet.AlphanumericDash);
		attr.IgnoreCase.Should().BeTrue();
	}

	[IdString(MaxLength = 32, CharSet = IdStringCharSet.Alphanumeric, IgnoreCase = true)]
	private class Slug;

	[Test]
	public void AppliesAsAttributeWithNamedArguments ()
	{
		var attr = typeof(Slug)
			.GetCustomAttributes(typeof(IdStringAttribute), inherit: false)
			.Cast<IdStringAttribute>()
			.Single();

		attr.MaxLength.Should().Be(32);
		attr.CharSet.Should().Be(IdStringCharSet.Alphanumeric);
		attr.IgnoreCase.Should().BeTrue();
	}
}
