using FluentAssertions;
using SaveState.Core.Mugen.ValueObjects;

namespace SaveState.Core.Tests.Mugen.ValueObjects;

public class ArcadeInfoTests
{
    [Fact]
    public void Constructor_WithDefaultValues_CreatesArcadeInfo()
    {
        // Act
        var arcadeInfo = new ArcadeInfo();

        // Assert
        arcadeInfo.IntroStoryboard.Should().Be(0);
        arcadeInfo.EndingStoryboard.Should().Be(0);
    }

    [Fact]
    public void Constructor_WithCustomValues_CreatesArcadeInfo()
    {
        // Arrange
        const int introStoryboard = 5;
        const int endingStoryboard = 10;

        // Act
        var arcadeInfo = new ArcadeInfo(introStoryboard, endingStoryboard);

        // Assert
        arcadeInfo.IntroStoryboard.Should().Be(introStoryboard);
        arcadeInfo.EndingStoryboard.Should().Be(endingStoryboard);
    }

    [Fact]
    public void Default_Property_ReturnsDefaultInstance()
    {
        // Act
        var defaultArcadeInfo = ArcadeInfo.Default;

        // Assert
        defaultArcadeInfo.IntroStoryboard.Should().Be(0);
        defaultArcadeInfo.EndingStoryboard.Should().Be(0);
    }

    [Fact]
    public void Equals_WithSameValues_ReturnsTrue()
    {
        // Arrange
        var info1 = new ArcadeInfo(1, 2);
        var info2 = new ArcadeInfo(1, 2);

        // Act & Assert
        info1.Should().Be(info2);
        info1.GetHashCode().Should().Be(info2.GetHashCode());
    }

    [Fact]
    public void Equals_WithDifferentValues_ReturnsFalse()
    {
        // Arrange
        var info1 = new ArcadeInfo(1, 2);
        var info2 = new ArcadeInfo(2, 3);

        // Act & Assert
        info1.Should().NotBe(info2);
    }

    [Fact]
    public void Equals_WithNull_ReturnsFalse()
    {
        // Arrange
        var info = new ArcadeInfo(1, 2);

        // Act & Assert
        info.Equals(null).Should().BeFalse();
    }

    [Fact]
    public void WithIntroStoryboard_CreatesNewInstanceWithModifiedValue()
    {
        // Arrange
        var original = new ArcadeInfo(1, 2);

        // Act
        var modified = original with { IntroStoryboard = 5 };

        // Assert
        modified.IntroStoryboard.Should().Be(5);
        modified.EndingStoryboard.Should().Be(2);
        original.IntroStoryboard.Should().Be(1); // Original unchanged
    }

    [Fact]
    public void WithEndingStoryboard_CreatesNewInstanceWithModifiedValue()
    {
        // Arrange
        var original = new ArcadeInfo(1, 2);

        // Act
        var modified = original with { EndingStoryboard = 7 };

        // Assert
        modified.IntroStoryboard.Should().Be(1);
        modified.EndingStoryboard.Should().Be(7);
        original.EndingStoryboard.Should().Be(2); // Original unchanged
    }

    [Fact]
    public void Deconstruct_ExtractsValuesCorrectly()
    {
        // Arrange
        var info = new ArcadeInfo(3, 4);

        // Act
        var (intro, ending) = info;

        // Assert
        intro.Should().Be(3);
        ending.Should().Be(4);
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        // Arrange
        var info = new ArcadeInfo(1, 2);

        // Act
        var result = info.ToString();

        // Assert
        result.Should().Contain("ArcadeInfo");
        result.Should().Contain("1");
        result.Should().Contain("2");
    }
}
