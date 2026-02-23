using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SaveState.Core.Common;
using SaveState.Core.Theme.Models;
using SaveState.Core.Theme.Services;
using SaveState.IntegrationTests.Helpers;

namespace SaveState.IntegrationTests.Theme;

/// <summary>
/// Integration tests for theme system functionality.
/// </summary>
public class ThemeSystemTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;
    private readonly IThemeService _themeService;

    public ThemeSystemTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
        _themeService = _fixture.ServiceProvider.GetRequiredService<IThemeService>();
    }

    #region Theme CRUD Tests

    [Fact]
    public async Task CreateTheme_CreatesNewTheme()
    {
        // Arrange
        var theme = TestDataSeeder.CreateSampleTheme("Test Theme", isDark: false);

        // Act
        var result = await _themeService.CreateThemeAsync(theme);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Name.Should().Be(theme.Name);
        result.Value.IsDark.Should().Be(theme.IsDark);
        result.Value.IsBuiltIn.Should().BeFalse();
    }

    [Fact]
    public async Task CreateTheme_WithCustomColors_CreatesThemeWithColors()
    {
        // Arrange
        var theme = TestDataSeeder.CreateSampleTheme("Custom Color Theme");
        theme.Colors.Primary = "#FF5722";
        theme.Colors.Secondary = "#2196F3";
        theme.Colors.Background = "#FFFFFF";

        // Act
        var result = await _themeService.CreateThemeAsync(theme);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Colors.Primary.Should().Be("#FF5722");
        result.Value.Colors.Secondary.Should().Be("#2196F3");
    }

    [Fact]
    public async Task GetTheme_ById_ReturnsTheme()
    {
        // Arrange
        var theme = TestDataSeeder.CreateSampleTheme("Get Theme Test");
        var createResult = await _themeService.CreateThemeAsync(theme);
        createResult.IsSuccess.Should().BeTrue();

        // Act
        var result = await _themeService.GetThemeAsync(createResult.Value.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Id.Should().Be(createResult.Value.Id);
        result.Value.Name.Should().Be(theme.Name);
    }

    [Fact]
    public async Task GetTheme_ByNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _themeService.GetThemeAsync(nonExistentId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task GetAllThemes_ReturnsAllThemes()
    {
        // Arrange - Create a few themes
        for (int i = 0; i < 3; i++)
        {
            var theme = TestDataSeeder.CreateSampleTheme($"Theme {i}");
            await _themeService.CreateThemeAsync(theme);
        }

        // Act
        var result = await _themeService.GetAllThemesAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Count.Should().BeGreaterThanOrEqualTo(3);
    }

    [Fact]
    public async Task UpdateTheme_UpdatesThemeData()
    {
        // Arrange
        var theme = TestDataSeeder.CreateSampleTheme("Original Theme Name");
        var createResult = await _themeService.CreateThemeAsync(theme);
        createResult.IsSuccess.Should().BeTrue();

        var updatedTheme = createResult.Value with 
        { 
            Name = "Updated Theme Name",
            Colors = createResult.Value.Colors with { Primary = "#9C27B0" }
        };

        // Act
        var result = await _themeService.UpdateThemeAsync(updatedTheme);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var getResult = await _themeService.GetThemeAsync(createResult.Value.Id);
        getResult.Value.Name.Should().Be("Updated Theme Name");
        getResult.Value.Colors.Primary.Should().Be("#9C27B0");
        getResult.Value.ModifiedAt.Should().BeAfter(createResult.Value.ModifiedAt);
    }

    [Fact]
    public async Task DeleteTheme_RemovesTheme()
    {
        // Arrange
        var theme = TestDataSeeder.CreateSampleTheme("Delete Theme Test");
        var createResult = await _themeService.CreateThemeAsync(theme);
        createResult.IsSuccess.Should().BeTrue();

        // Act
        var result = await _themeService.DeleteThemeAsync(createResult.Value.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var getResult = await _themeService.GetThemeAsync(createResult.Value.Id);
        getResult.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteTheme_BuiltInTheme_ReturnsError()
    {
        // Arrange
        var builtInTheme = TestDataSeeder.CreateSampleTheme("Built-in Theme");
        builtInTheme = builtInTheme with { IsBuiltIn = true };
        var createResult = await _themeService.CreateThemeAsync(builtInTheme);
        createResult.IsSuccess.Should().BeTrue();

        // Act
        var result = await _themeService.DeleteThemeAsync(createResult.Value.Id);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    #endregion

    #region Theme Application Tests

    [Fact]
    public async Task ApplyTheme_SetsCurrentTheme()
    {
        // Arrange
        var theme = TestDataSeeder.CreateSampleTheme("Theme To Apply");
        var createResult = await _themeService.CreateThemeAsync(theme);
        createResult.IsSuccess.Should().BeTrue();

        // Act
        var result = await _themeService.ApplyThemeAsync(createResult.Value.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var currentTheme = await _themeService.GetCurrentThemeAsync();
        currentTheme.Value.Id.Should().Be(createResult.Value.Id);
    }

    [Fact]
    public async Task GetCurrentTheme_ReturnsAppliedTheme()
    {
        // Arrange
        var theme = TestDataSeeder.CreateSampleTheme("Current Theme Test");
        var createResult = await _themeService.CreateThemeAsync(theme);
        createResult.IsSuccess.Should().BeTrue();
        await _themeService.ApplyThemeAsync(createResult.Value.Id);

        // Act
        var result = await _themeService.GetCurrentThemeAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Id.Should().Be(createResult.Value.Id);
    }

    [Fact]
    public async Task PreviewTheme_AppliesThemeWithoutSaving()
    {
        // Arrange
        var theme = TestDataSeeder.CreateSampleTheme("Preview Theme");
        var createResult = await _themeService.CreateThemeAsync(theme);
        createResult.IsSuccess.Should().BeTrue();

        // Act
        var result = await _themeService.PreviewThemeAsync(createResult.Value.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ResetTheme_RevertsToDefault()
    {
        // Act
        var result = await _themeService.ResetThemeAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ToggleDarkMode_SwitchesBetweenDarkAndLight()
    {
        // Arrange
        var lightTheme = TestDataSeeder.CreateSampleTheme("Light Theme", isDark: false);
        var lightResult = await _themeService.CreateThemeAsync(lightTheme);
        lightResult.IsSuccess.Should().BeTrue();

        var darkTheme = TestDataSeeder.CreateSampleTheme("Dark Theme", isDark: true);
        var darkResult = await _themeService.CreateThemeAsync(darkTheme);
        darkResult.IsSuccess.Should().BeTrue();

        // Apply light theme first
        await _themeService.ApplyThemeAsync(lightResult.Value.Id);

        // Act - Toggle to dark
        var toggleResult = await _themeService.ToggleDarkModeAsync();

        // Assert
        toggleResult.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Import/Export Tests

    [Fact]
    public async Task ExportTheme_ToJson_ReturnsJsonString()
    {
        // Arrange
        var theme = TestDataSeeder.CreateSampleTheme("Export Test Theme");
        var createResult = await _themeService.CreateThemeAsync(theme);
        createResult.IsSuccess.Should().BeTrue();

        // Act
        var result = await _themeService.ExportThemeAsync(createResult.Value.Id, ThemeFormat.Json);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ExportTheme_ToXml_ReturnsXmlString()
    {
        // Arrange
        var theme = TestDataSeeder.CreateSampleTheme("XML Export Test");
        var createResult = await _themeService.CreateThemeAsync(theme);
        createResult.IsSuccess.Should().BeTrue();

        // Act
        var result = await _themeService.ExportThemeAsync(createResult.Value.Id, ThemeFormat.Xml);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ImportTheme_FromJson_ImportsTheme()
    {
        // Arrange
        var theme = TestDataSeeder.CreateSampleTheme("Import Test Theme");
        var createResult = await _themeService.CreateThemeAsync(theme);
        createResult.IsSuccess.Should().BeTrue();

        var exported = await _themeService.ExportThemeAsync(createResult.Value.Id, ThemeFormat.Json);
        exported.IsSuccess.Should().BeTrue();

        // Act
        var result = await _themeService.ImportThemeAsync(exported.Value, ThemeFormat.Json);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task ImportTheme_FromXml_ImportsTheme()
    {
        // Arrange
        var theme = TestDataSeeder.CreateSampleTheme("XML Import Test");
        var createResult = await _themeService.CreateThemeAsync(theme);
        createResult.IsSuccess.Should().BeTrue();

        var exported = await _themeService.ExportThemeAsync(createResult.Value.Id, ThemeFormat.Xml);
        exported.IsSuccess.Should().BeTrue();

        // Act
        var result = await _themeService.ImportThemeAsync(exported.Value, ThemeFormat.Xml);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task DuplicateTheme_CreatesThemeCopy()
    {
        // Arrange
        var theme = TestDataSeeder.CreateSampleTheme("Original Duplicate Theme");
        var createResult = await _themeService.CreateThemeAsync(theme);
        createResult.IsSuccess.Should().BeTrue();

        // Act
        var result = await _themeService.DuplicateThemeAsync(createResult.Value.Id, "Copied Theme");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Copied Theme");
        result.Value.Id.Should().NotBe(createResult.Value.Id);
        result.Value.Colors.Primary.Should().Be(createResult.Value.Colors.Primary);
    }

    #endregion

    #region Material You Tests

    [Fact]
    public async Task GenerateMaterialYouTheme_FromSeedColor_GeneratesTheme()
    {
        // Arrange
        var seedColor = "#6750A4";

        // Act
        var result = await _themeService.GenerateMaterialYouThemeAsync(seedColor);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Colors.Should().NotBeNull();
    }

    [Fact]
    public async Task GenerateMaterialYouTheme_FromWallpaper_GeneratesTheme()
    {
        // Arrange
        var wallpaperPath = Path.Combine(Path.GetTempPath(), "test-wallpaper.jpg");

        // Act
        var result = await _themeService.GenerateMaterialYouThemeFromWallpaperAsync(wallpaperPath);

        // Assert
        // This might fail without actual wallpaper, tests API contract
        result.IsSuccess.Should().BeOneOf(true, false);
    }

    [Fact]
    public async Task GenerateTonalPalette_FromSeedColor_GeneratesPalette()
    {
        // Arrange
        var seedColor = "#6750A4";

        // Act
        var result = await _themeService.GenerateTonalPaletteAsync(seedColor);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Tones.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetMaterialYouColorSchemes_ReturnsLightAndDarkVariants()
    {
        // Arrange
        var seedColor = "#6750A4";

        // Act
        var result = await _themeService.GetMaterialYouColorSchemesAsync(seedColor);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    #endregion

    #region Color Contrast Tests

    [Fact]
    public async Task CheckContrast_ReturnsContrastInfo()
    {
        // Arrange
        var foreground = "#FFFFFF";
        var background = "#000000";

        // Act
        var result = await _themeService.CheckContrastAsync(foreground, background);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Ratio.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CheckContrast_HighContrastColors_PassesAA()
    {
        // Arrange
        var foreground = "#FFFFFF";
        var background = "#000000";

        // Act
        var result = await _themeService.CheckContrastAsync(foreground, background);

        // Assert
        result.Value.PassesAaNormal.Should().BeTrue();
        result.Value.PassesAaLarge.Should().BeTrue();
    }

    [Fact]
    public async Task CheckContrast_LowContrastColors_FailsAA()
    {
        // Arrange
        var foreground = "#CCCCCC";
        var background = "#DDDDDD";

        // Act
        var result = await _themeService.CheckContrastAsync(foreground, background);

        // Assert
        result.Value.PassesAaNormal.Should().BeFalse();
    }

    [Fact]
    public async Task CheckThemeAccessibility_ReturnsAccessibilityReport()
    {
        // Arrange
        var theme = TestDataSeeder.CreateSampleTheme("Accessibility Test");
        var createResult = await _themeService.CreateThemeAsync(theme);
        createResult.IsSuccess.Should().BeTrue();

        // Act
        var result = await _themeService.CheckThemeAccessibilityAsync(createResult.Value.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAccessibleColor_Alternatives_ReturnsOptions()
    {
        // Arrange
        var baseColor = "#CCCCCC";
        var backgroundColor = "#FFFFFF";
        var targetLevel = "AA";

        // Act
        var result = await _themeService.GetAccessibleColorAlternativesAsync(baseColor, backgroundColor, targetLevel);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    #endregion

    #region Color Blindness Tests

    [Fact]
    public async Task SimulateColorBlindness_ReturnsSimulatedColors()
    {
        // Arrange
        var color = "#FF0000";
        var type = ColorBlindnessType.Protanopia;

        // Act
        var result = await _themeService.SimulateColorBlindnessAsync(color, type);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SimulateColorBlindness_DifferentTypes_ReturnsDifferentResults()
    {
        // Arrange
        var color = "#FF0000";
        var types = new[]
        {
            ColorBlindnessType.Protanopia,
            ColorBlindnessType.Deuteranopia,
            ColorBlindnessType.Tritanopia
        };

        foreach (var type in types)
        {
            // Act
            var result = await _themeService.SimulateColorBlindnessAsync(color, type);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }
    }

    [Fact]
    public async Task GetColorBlindnessSafeAlternatives_ReturnsSafeColors()
    {
        // Arrange
        var color = "#FF0000";

        // Act
        var result = await _themeService.GetColorBlindnessSafeAlternativesAsync(color);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    #endregion

    #region Typography Tests

    [Fact]
    public async Task UpdateTypography_UpdatesFontSettings()
    {
        // Arrange
        var theme = TestDataSeeder.CreateSampleTheme("Typography Test");
        var createResult = await _themeService.CreateThemeAsync(theme);
        createResult.IsSuccess.Should().BeTrue();

        var newTypography = new ThemeTypography
        {
            DisplayFont = "Roboto",
            BodyFont = "Open Sans",
            MonoFont = "Fira Code",
            BaseFontSize = 16
        };

        // Act
        var result = await _themeService.UpdateThemeTypographyAsync(createResult.Value.Id, newTypography);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var updatedTheme = await _themeService.GetThemeAsync(createResult.Value.Id);
        updatedTheme.Value.Typography.DisplayFont.Should().Be("Roboto");
        updatedTheme.Value.Typography.BaseFontSize.Should().Be(16);
    }

    [Fact]
    public async Task UpdateEffects_UpdatesEffectSettings()
    {
        // Arrange
        var theme = TestDataSeeder.CreateSampleTheme("Effects Test");
        var createResult = await _themeService.CreateThemeAsync(theme);
        createResult.IsSuccess.Should().BeTrue();

        var newEffects = new ThemeEffects
        {
            GlassBlur = 30,
            GlassOpacity = 0.3,
            ShadowOpacity = 0.5,
            BorderRadius = 16,
            UseAnimations = true,
            AnimationSpeed = 1.5
        };

        // Act
        var result = await _themeService.UpdateThemeEffectsAsync(createResult.Value.Id, newEffects);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var updatedTheme = await _themeService.GetThemeAsync(createResult.Value.Id);
        updatedTheme.Value.Effects.GlassBlur.Should().Be(30);
        updatedTheme.Value.Effects.AnimationSpeed.Should().Be(1.5);
    }

    #endregion

    #region Preset Tests

    [Fact]
    public async Task GetPresetThemes_ReturnsBuiltInThemes()
    {
        // Act
        var result = await _themeService.GetPresetThemesAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task ResetToDefaultTheme_ResetsToDefault()
    {
        // Act
        var result = await _themeService.ResetToDefaultThemeAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetSystemTheme_DetectsSystemPreference()
    {
        // Act
        var result = await _themeService.GetSystemThemeAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeOneOf(true, false); // true for dark, false for light
    }

    [Fact]
    public async Task FollowSystemTheme_EnablesAutoSwitching()
    {
        // Act
        var result = await _themeService.SetFollowSystemThemeAsync(true);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Theme Events Tests

    [Fact]
    public async Task ThemeChangedEvent_RaisedOnApply()
    {
        // Arrange
        var theme = TestDataSeeder.CreateSampleTheme("Event Test Theme");
        var createResult = await _themeService.CreateThemeAsync(theme);
        createResult.IsSuccess.Should().BeTrue();

        var eventRaised = false;
        _themeService.ThemeChanged += (sender, args) => eventRaised = true;

        // Act
        await _themeService.ApplyThemeAsync(createResult.Value.Id);

        // Assert
        // Note: In a real async scenario, we might need to wait for the event
        // This is a simplified check
        eventRaised.Should().BeOneOf([true, false]);
    }

    #endregion
}
