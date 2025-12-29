using FluentAssertions;
using SaveState.Core.Common.ValueObjects;
using Xunit;

namespace SaveState.Core.Tests.Common.ValueObjects;

public class GameTitleTests
{
    [Fact]
    public void From_ValidTitle_CreatesGameTitle()
    {
        var title = "Test Game";
        var gameTitle = GameTitle.From(title);

        gameTitle.Value.Should().Be(title);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void From_InvalidTitle_ThrowsArgumentException(string? invalidTitle)
    {
        Action act = () => GameTitle.From(invalidTitle!);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*title*");
    }

    [Fact]
    public void From_SameTitle_ReturnsEqualObjects()
    {
        var title = "Test Game";
        var title1 = GameTitle.From(title);
        var title2 = GameTitle.From(title);

        title1.Should().Be(title2);
        title1.GetHashCode().Should().Be(title2.GetHashCode());
    }

    [Fact]
    public void From_DifferentTitles_ReturnsUnequalObjects()
    {
        var title1 = GameTitle.From("Game 1");
        var title2 = GameTitle.From("Game 2");

        title1.Should().NotBe(title2);
    }

    [Fact]
    public void From_SameTitlesDifferentCase_ReturnsEqualObjects()
    {
        var title1 = GameTitle.From("Test Game");
        var title2 = GameTitle.From("TEST GAME");

        title1.Should().Be(title2);
        title1.GetHashCode().Should().Be(title2.GetHashCode());
    }

    [Fact]
    public void ImplicitOperatorString_ReturnsValue()
    {
        var title = "Test Game";
        var gameTitle = GameTitle.From(title);

        string result = gameTitle;
        result.Should().Be(title);
    }

    [Fact]
    public void ExplicitOperatorGameTitle_ValidString_CreatesGameTitle()
    {
        var title = "Test Game";
        var gameTitle = (GameTitle)title;

        gameTitle.Value.Should().Be(title);
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        var title = "Test Game";
        var gameTitle = GameTitle.From(title);

        gameTitle.ToString().Should().Be(title);
    }
}
