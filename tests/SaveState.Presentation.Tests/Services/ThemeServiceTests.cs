using FluentAssertions;
using SaveState.Presentation.Services;
using Xunit;

namespace SaveState.Presentation.Tests.Services;

public class ThemeServiceTests
{
    [Fact]
    public void CurrentTheme_DefaultsToDark()
    {
        // Arrange
        var themeService = new ThemeService();

        // Act & Assert
        themeService.CurrentTheme.Should().Be(ThemeType.Dark);
    }

    [Fact]
    public void AvailableThemes_ReturnsAllThemeTypes()
    {
        // Arrange
        var themeService = new ThemeService();

        // Act
        var availableThemes = themeService.AvailableThemes;

        // Assert
        availableThemes.Should().HaveCount(3);
        availableThemes.Should().Contain(ThemeType.Light);
        availableThemes.Should().Contain(ThemeType.Dark);
        availableThemes.Should().Contain(ThemeType.System);
    }

    [Fact]
    public void SetTheme_ChangesCurrentTheme()
    {
        // Arrange
        var themeService = new ThemeService();
        ThemeType? changedTheme = null;

        themeService.ThemeChanged += (sender, theme) => changedTheme = theme;

        // Act
        themeService.SetTheme(ThemeType.Light);

        // Assert
        themeService.CurrentTheme.Should().Be(ThemeType.Light);
        changedTheme.Should().Be(ThemeType.Light);
    }

    [Fact]
    public void SetTheme_SameTheme_DoesNotRaiseEvent()
    {
        // Arrange
        var themeService = new ThemeService();
        var eventRaised = false;

        themeService.ThemeChanged += (sender, theme) => eventRaised = true;

        // Act
        themeService.SetTheme(ThemeType.Dark); // Same as default

        // Assert
        eventRaised.Should().BeFalse();
    }

    [Fact]
    public void SetTheme_ToLight_RaisesEventWithCorrectTheme()
    {
        // Arrange
        var themeService = new ThemeService();
        ThemeType? receivedTheme = null;

        themeService.ThemeChanged += (sender, theme) => receivedTheme = theme;

        // Act
        themeService.SetTheme(ThemeType.Light);

        // Assert
        receivedTheme.Should().Be(ThemeType.Light);
    }

    [Fact]
    public void SetTheme_ToSystem_RaisesEventWithCorrectTheme()
    {
        // Arrange
        var themeService = new ThemeService();
        ThemeType? receivedTheme = null;

        themeService.ThemeChanged += (sender, theme) => receivedTheme = theme;

        // Act
        themeService.SetTheme(ThemeType.System);

        // Assert
        receivedTheme.Should().Be(ThemeType.System);
    }
}
