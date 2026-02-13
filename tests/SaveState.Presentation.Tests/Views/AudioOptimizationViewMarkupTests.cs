using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace SaveState.Tests.Presentation.Views;

public class AudioOptimizationViewMarkupTests
{
    private static XDocument LoadView()
    {
        var baseDir = AppContext.BaseDirectory;
        var repoRoot = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", ".."));
        var viewPath = Path.Combine(
            repoRoot,
            "src",
            "SaveState.Presentation",
            "Views",
            "Settings",
            "AudioOptimizationView.axaml");

        return XDocument.Load(viewPath);
    }

    [Fact]
    public void ExclusiveModeToggle_HasExplicitAutomationId()
    {
        var document = LoadView();
        var toggle = document
            .Descendants()
            .FirstOrDefault(e => e.Name.LocalName == "ToggleSwitch"
                                 && (string?)e.Attribute("Content") == "Exclusive Mode");

        Assert.NotNull(toggle);
        Assert.Equal("ExclusiveModeToggle", (string?)toggle.Attribute("AutomationProperties.AutomationId"));
    }

    [Fact]
    public void ExclusiveModeWarningText_IsMarkedForAutomation()
    {
        var document = LoadView();
        var warning = document
            .Descendants()
            .FirstOrDefault(e => string.Equals(
                (string?)e.Attribute("AutomationProperties.AutomationId"),
                "ExclusiveModeWarningText",
                StringComparison.Ordinal));

        Assert.NotNull(warning);
        Assert.Equal("TextBlock", warning!.Name.LocalName);
    }

    [Fact]
    public void QuickProfileButtons_ExposeAutomationIds()
    {
        var document = LoadView();
        var quickButtons = document
            .Descendants()
            .Where(e => e.Name.LocalName == "Button"
                        && TryGetAutomationId(e, out var automationId)
                        && automationId.Contains("ProfileToggle_", StringComparison.Ordinal))
            .ToList();

        Assert.NotEmpty(quickButtons);
    }

    [Theory]
    [InlineData("SavedProfilesCombo")]
    [InlineData("ApplyProfileButton")]
    [InlineData("DeleteProfileButton")]
    [InlineData("SaveProfileButton")]
    public void ProfileManagementButtons_HaveAutomationIds(string automationId)
    {
        var document = LoadView();
        var element = document
            .Descendants()
            .FirstOrDefault(e => string.Equals(
                (string?)e.Attribute("AutomationProperties.AutomationId"),
                automationId,
                StringComparison.Ordinal));

        Assert.NotNull(element);
    }

    private static bool TryGetAutomationId(XElement element, out string? automationId)
    {
        automationId = (string?)element.Attribute("AutomationProperties.AutomationId");
        return automationId is not null;
    }
}
