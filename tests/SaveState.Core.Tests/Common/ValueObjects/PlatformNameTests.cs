using FluentAssertions;
using SaveState.Core.GameLibrary.ValueObjects;

namespace SaveState.Core.Tests.Common.ValueObjects;

public class PlatformNameTests
{
    [Fact]
    public void From_WithValidValue_CreatesPlatformName()
    {
        // Arrange
        const string validName = "PC";

        // Act
        var platformName = PlatformName.From(validName);

        // Assert
        platformName.Value.Should().Be(validName);
    }

    [Fact]
    public void From_WithNullValue_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => PlatformName.From(null!));
    }

    [Fact]
    public void From_WithEmptyValue_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentException>(() => PlatformName.From(string.Empty));
    }

    [Fact]
    public void From_WithWhitespaceOnly_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentException>(() => PlatformName.From("   "));
    }

    [Fact]
    public void From_WithTooLongValue_ThrowsArgumentException()
    {
        // Arrange
        var tooLongName = new string('A', 101);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => PlatformName.From(tooLongName));
    }

    [Fact]
    public void From_WithValue_ReplacesValueWithTrimmed()
    {
        // Arrange
        const string untrimmedName = "  PC  ";

        // Act
        var platformName = PlatformName.From(untrimmedName);

        // Assert
        platformName.Value.Should().Be("PC");
    }

    [Fact]
    public void ImplicitOperatorString_ReturnsValue()
    {
        // Arrange
        var platformName = PlatformName.From("Xbox");

        // Act
        string value = platformName;

        // Assert
        value.Should().Be("Xbox");
    }

    [Fact]
    public void ExplicitOperatorPlatformName_CreatesPlatformName()
    {
        // Arrange
        const string value = "PlayStation";

        // Act
        var platformName = (PlatformName)value;

        // Assert
        platformName.Value.Should().Be("PlayStation");
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        // Arrange
        var platformName = PlatformName.From("Nintendo Switch");

        // Act
        var result = platformName.ToString();

        // Assert
        result.Should().Be("Nintendo Switch");
    }

    [Fact]
    public void Equals_WithSameValues_ReturnsTrue()
    {
        // Arrange
        var name1 = PlatformName.From("PC");
        var name2 = PlatformName.From("PC");

        // Act & Assert
        name1.Should().Be(name2);
        name1.GetHashCode().Should().Be(name2.GetHashCode());
    }

    [Fact]
    public void Equals_WithDifferentCase_ReturnsTrue()
    {
        // Arrange
        var name1 = PlatformName.From("PC");
        var name2 = PlatformName.From("pc");

        // Act & Assert
        name1.Should().Be(name2);
        name1.GetHashCode().Should().Be(name2.GetHashCode());
    }

    [Fact]
    public void Equals_WithDifferentValues_ReturnsFalse()
    {
        // Arrange
        var name1 = PlatformName.From("PC");
        var name2 = PlatformName.From("Xbox");

        // Act & Assert
        name1.Should().NotBe(name2);
    }

    [Fact]
    public void Equals_WithNull_ReturnsFalse()
    {
        // Arrange
        var name = PlatformName.From("PC");

        // Act & Assert
        name.Equals(null).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithDifferentType_ReturnsFalse()
    {
        // Arrange
        var name = PlatformName.From("PC");
        var other = "PC";

        // Act & Assert
        name.Equals(other).Should().BeFalse();
    }
}
