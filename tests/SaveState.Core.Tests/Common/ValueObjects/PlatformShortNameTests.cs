using FluentAssertions;
using SaveState.Core.GameLibrary.ValueObjects;

namespace SaveState.Core.Tests.Common.ValueObjects;

public class PlatformShortNameTests
{
    [Fact]
    public void From_WithValidValue_CreatesPlatformShortName()
    {
        // Arrange
        const string validName = "PC";

        // Act
        var shortName = PlatformShortName.From(validName);

        // Assert
        shortName.Value.Should().Be("PC");
    }

    [Fact]
    public void From_WithValidValue_ConvertsToUppercase()
    {
        // Arrange
        const string lowercaseName = "pc";

        // Act
        var shortName = PlatformShortName.From(lowercaseName);

        // Assert
        shortName.Value.Should().Be("PC");
    }

    [Fact]
    public void From_WithValidValue_TrimsWhitespace()
    {
        // Arrange
        const string untrimmedName = "  PC  ";

        // Act
        var shortName = PlatformShortName.From(untrimmedName);

        // Assert
        shortName.Value.Should().Be("PC");
    }

    [Fact]
    public void From_WithNullValue_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => PlatformShortName.From(null!));
    }

    [Fact]
    public void From_WithEmptyValue_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentException>(() => PlatformShortName.From(string.Empty));
    }

    [Fact]
    public void From_WithWhitespaceOnly_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentException>(() => PlatformShortName.From("   "));
    }

    [Fact]
    public void From_WithTooLongValue_ThrowsArgumentException()
    {
        // Arrange
        var tooLongName = new string('A', 21);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => PlatformShortName.From(tooLongName));
    }

    [Theory]
    [InlineData("PC-GAMING")]
    [InlineData("PS5")]
    [InlineData("XBOX_360")]
    [InlineData("NSWITCH")]
    public void From_WithValidCharacters_CreatesPlatformShortName(string validName)
    {
        // Act
        var shortName = PlatformShortName.From(validName);

        // Assert
        shortName.Value.Should().Be(validName.ToUpperInvariant());
    }

    [Theory]
    [InlineData("PC Gaming")]
    [InlineData("PS5@")]
    [InlineData("XBOX#360")]
    [InlineData("NS.WITCH")]
    [InlineData("PLATFORM!")]
    public void From_WithInvalidCharacters_ThrowsArgumentException(string invalidName)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => PlatformShortName.From(invalidName));
    }

    [Fact]
    public void ImplicitOperatorString_ReturnsValue()
    {
        // Arrange
        var shortName = PlatformShortName.From("xbox");

        // Act
        string value = shortName;

        // Assert
        value.Should().Be("XBOX");
    }

    [Fact]
    public void ExplicitOperatorPlatformShortName_CreatesPlatformShortName()
    {
        // Arrange
        const string value = "playstation";

        // Act
        var shortName = (PlatformShortName)value;

        // Assert
        shortName.Value.Should().Be("PLAYSTATION");
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        // Arrange
        var shortName = PlatformShortName.From("nintendo");

        // Act
        var result = shortName.ToString();

        // Assert
        result.Should().Be("NINTENDO");
    }

    [Fact]
    public void Equals_WithSameValues_ReturnsTrue()
    {
        // Arrange
        var name1 = PlatformShortName.From("PC");
        var name2 = PlatformShortName.From("PC");

        // Act & Assert
        name1.Should().Be(name2);
        name1.GetHashCode().Should().Be(name2.GetHashCode());
    }

    [Fact]
    public void Equals_WithDifferentValues_ReturnsFalse()
    {
        // Arrange
        var name1 = PlatformShortName.From("PC");
        var name2 = PlatformShortName.From("XBOX");

        // Act & Assert
        name1.Should().NotBe(name2);
    }

    [Fact]
    public void Equals_WithNull_ReturnsFalse()
    {
        // Arrange
        var name = PlatformShortName.From("PC");

        // Act & Assert
        name.Equals(null).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithDifferentType_ReturnsFalse()
    {
        // Arrange
        var name = PlatformShortName.From("PC");
        var other = "PC";

        // Act & Assert
        name.Equals(other).Should().BeFalse();
    }
}
