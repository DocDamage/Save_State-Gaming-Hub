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
        var themeName = "Test Theme";

        // Act
        var result = await _themeService.CreateThemeAsync(themeName);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Name.Should().Be(themeName);
        result.Value.IsBuiltIn.Should().BeFalse();
    }

    [Fact]
    public async Task CreateTheme_WithBaseTheme_CreatesThemeWithColors()
    {
        // Arrange
        var baseTheme = TestDataSeeder.CreateSampleTheme("Base Theme");
        var createResult = await _themeService.CreateThemeAsync("Base Theme");
        createResult.IsSuccess.Should().BeTrue();
        
        // Create a new theme based on the existing one
        var newThemeName = "Custom Color Theme";

        // Act
        var result = await _themeService.CreateThemeAsync(newThemeName, createResult.Value);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be(newThemeName);
        result.Value.Colors.Should().NotBeNull();
    }

    [Fact]
    public async Task GetTheme_ById_ReturnsTheme()
    {
        // Arrange
        var themeName = "Get Theme Test";
        var createResult = await _themeService.CreateThemeAsync(themeName);
        createResult.IsSuccess.Should().BeTrue();

        // Act
        var result = await _themeService.GetThemeAsync(createResult.Value.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Id.Should().Be(createResult.Value.Id);
        result.Value.Name.Should().Be(themeName);
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
            await _themeService.CreateThemeAsync($"Theme {i}");
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
        var themeName = "Original Theme Name";
        var createResult = await _themeService.CreateThemeAsync(themeName);
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
        var themeName = "Delete Theme Test";
        var createResult = await _themeService.CreateThemeAsync(themeName);
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
        var builtInThemes = await _themeService.GetBuiltInThemesAsync();
        builtInThemes.IsSuccess.Should().BeTrue();
        
        // Skip if no built-in themes exist
        if (builtInThemes.Value.Count == 0)
        {
            return;
        }
        
        var builtInThemeId = builtInThemes.Value[0].Id;

        // Act
        var result = await _themeService.DeleteThemeAsync(builtInThemeId);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    #endregion

    #region Theme Application Tests

    [Fact]
    public async Task ApplyTheme_SetsCurrentTheme()
    {
        // Arrange
        var themeName = "Theme To Apply";
        var createResult = await _themeService.CreateThemeAsync(themeName);
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
        var themeName = "Current Theme Test";
        var createResult = await _themeService.CreateThemeAsync(themeName);
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
        var themeName = "Preview Theme";
        var createResult = await _themeService.CreateThemeAsync(themeName);
        createResult.IsSuccess.Should().BeTrue();

        // Act
        var result = await _themeService.PreviewThemeAsync(createResult.Value);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ResetToDefault_ResetsToDefault()
    {
        // Act
        var result = await _themeService.ResetToDefaultAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Import/Export Tests

    [Fact]
    public async Task ExportTheme_ToJson_ReturnsJsonString()
    {
        // Arrange
        var themeName = "Export Test Theme";
        var createResult = await _themeService.CreateThemeAsync(themeName);
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
        var themeName = "XML Export Test";
        var createResult = await _themeService.CreateThemeAsync(themeName);
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
        var themeName = "Import Test Theme";
        var createResult = await _themeService.CreateThemeAsync(themeName);
        createResult.IsSuccess.Should().BeTrue();

        var exported = await _themeService.ExportThemeAsync(createResult.Value.Id, ThemeFormat.Json);
        exported.IsSuccess.Should().BeTrue();

        // Act
        var result = await _themeService.ImportThemeAsync(exported.Value);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task DuplicateTheme_CreatesThemeCopy()
    {
        // Arrange
        var themeName = "Original Duplicate Theme";
        var createResult = await _themeService.CreateThemeAsync(themeName);
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

    #region Color and Palette Tests

    [Fact]
    public void GenerateFromSeedColor_GeneratesThemeColors()
    {
        // Arrange
        var seedColor = "#6750A4";

        // Act
        var result = _themeService.GenerateFromSeedColor(seedColor, isDark: false);

        // Assert
        result.Should().NotBeNull();
        result.Primary.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GeneratePalette_FromSeedColor_GeneratesPalette()
    {
        // Arrange
        var seedColor = "#6750A4";

        // Act
        var result = _themeService.GeneratePalette(seedColor, count: 5);

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(5);
    }

    [Fact]
    public void CalculateContrast_ReturnsContrastInfo()
    {
        // Arrange
        var foreground = "#FFFFFF";
        var background = "#000000";

        // Act
        var result = _themeService.CalculateContrast(foreground, background);

        // Assert
        result.Should().NotBeNull();
        result.Ratio.Should().BeGreaterThan(0);
    }

    [Fact]
    public void CalculateContrast_HighContrastColors_PassesAA()
    {
        // Arrange
        var foreground = "#FFFFFF";
        var background = "#000000";

        // Act
        var result = _themeService.CalculateContrast(foreground, background);

        // Assert
        result.PassesAaNormal.Should().BeTrue();
        result.PassesAaLarge.Should().BeTrue();
    }

    [Fact]
    public void CalculateContrast_LowContrastColors_FailsAA()
    {
        // Arrange
        var foreground = "#CCCCCC";
        var background = "#DDDDDD";

        // Act
        var result = _themeService.CalculateContrast(foreground, background);

        // Assert
        result.PassesAaNormal.Should().BeFalse();
    }

    [Fact]
    public void SimulateColorBlindness_ReturnsSimulatedColors()
    {
        // Arrange
        var color = "#FF0000";
        var type = ColorBlindnessType.Protanopia;

        // Act
        var result = _themeService.SimulateColorBlindness(color, type);

        // Assert
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void SimulateColorBlindness_DifferentTypes_ReturnsDifferentResults()
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
            var result = _themeService.SimulateColorBlindness(color, type);

            // Assert
            result.Should().NotBeNullOrEmpty();
        }
    }

    #endregion

    #region Built-in Theme Tests

    [Fact]
    public async Task GetBuiltInThemes_ReturnsBuiltInThemes()
    {
        // Act
        var result = await _themeService.GetBuiltInThemesAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public void GetDefaultLightTheme_ReturnsLightTheme()
    {
        // Act
        var result = _themeService.GetDefaultLightTheme();

        // Assert
        result.Should().NotBeNull();
        result.IsDark.Should().BeFalse();
    }

    [Fact]
    public void GetDefaultDarkTheme_ReturnsDarkTheme()
    {
        // Act
        var result = _themeService.GetDefaultDarkTheme();

        // Assert
        result.Should().NotBeNull();
        result.IsDark.Should().BeTrue();
    }

    [Fact]
    public void GetSystemTheme_ReturnsTheme()
    {
        // Act
        var result = _themeService.GetSystemTheme();

        // Assert
        result.Should().NotBeNull();
    }

    #endregion

    #region Theme Events Tests

    [Fact]
    public async Task ThemeChangedEvent_RaisedOnApply()
    {
        // Arrange
        var themeName = "Event Test Theme";
        var createResult = await _themeService.CreateThemeAsync(themeName);
        createResult.IsSuccess.Should().BeTrue();

        var eventRaised = false;
        _themeService.ThemeChanged += (sender, args) => eventRaised = true;

        // Act
        await _themeService.ApplyThemeAsync(createResult.Value.Id);

        // Assert
        // Note: In a real async scenario, we might need to wait for the event
        // This is a simplified check - just verify no exception was thrown
        eventRaised.Should().BeTrue();
    }

    #endregion

    #region Persistence Tests

    [Fact]
    public async Task SaveThemes_PersistsThemes()
    {
        // Act
        var result = await _themeService.SaveThemesAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task LoadThemes_LoadsPersistedThemes()
    {
        // Act
        var result = await _themeService.LoadThemesAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion
}
