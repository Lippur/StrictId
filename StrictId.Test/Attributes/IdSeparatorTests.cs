using FluentAssertions;

namespace StrictId.Test.Attributes;

[TestFixture]
public class IdSeparatorTests
{
	[TestCase(IdSeparator.Underscore, '_')]
	[TestCase(IdSeparator.Slash, '/')]
	[TestCase(IdSeparator.Period, '.')]
	[TestCase(IdSeparator.Colon, ':')]
	public void ToChar_ReturnsExpectedCharacter (IdSeparator separator, char expected)
	{
		separator.ToChar().Should().Be(expected);
	}

	[Test]
	public void ToChar_ThrowsForOutOfRangeValue ()
	{
		var invalid = (IdSeparator)99;

		var act = () => invalid.ToChar();

		act.Should().Throw<ArgumentOutOfRangeException>();
	}

	[TestCase('_', IdSeparator.Underscore)]
	[TestCase('/', IdSeparator.Slash)]
	[TestCase('.', IdSeparator.Period)]
	[TestCase(':', IdSeparator.Colon)]
	public void TryFromChar_RecognisesValidSeparators (char c, IdSeparator expected)
	{
		IdSeparators.TryFromChar(c, out var actual).Should().BeTrue();
		actual.Should().Be(expected);
	}

	[TestCase('-')]
	[TestCase('\\')]
	[TestCase(' ')]
	[TestCase('#')]
	[TestCase('a')]
	[TestCase('0')]
	public void TryFromChar_RejectsNonSeparatorCharacters (char c)
	{
		IdSeparators.TryFromChar(c, out var actual).Should().BeFalse();
		actual.Should().Be(default(IdSeparator));
	}

	[Test]
	public void ToCharAndTryFromChar_RoundTripForEveryEnumValue ()
	{
		foreach (IdSeparator separator in Enum.GetValues<IdSeparator>())
		{
			var c = separator.ToChar();
			IdSeparators.TryFromChar(c, out var roundTripped).Should().BeTrue();
			roundTripped.Should().Be(separator);
		}
	}
}
