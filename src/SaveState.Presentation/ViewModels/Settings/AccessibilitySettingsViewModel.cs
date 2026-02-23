using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Presentation.Services;
using SaveState.Presentation.Services.Accessibility;

namespace SaveState.Presentation.ViewModels.Settings;

/// <summary>
/// ViewModel for accessibility settings.
/// </summary>
public partial class AccessibilitySettingsViewModel : ObservableObject
{
    private readonly IAccessibilityService _accessibilityService;
    private readonly AccessibilityAuditor _auditor;
    private readonly IDialogService _dialogService;
    private readonly ILogger<AccessibilitySettingsViewModel> _logger;

    // Screen Reader
    [ObservableProperty]
    private bool _isScreenReaderEnabled;

    [ObservableProperty]
    private List<string> _announcementSpeeds = new() { "Slow", "Normal", "Fast" };

    [ObservableProperty]
    private string _selectedAnnouncementSpeed = "Normal";

    // Visual Aids
    [ObservableProperty]
    private bool _isHighContrastEnabled;

    [ObservableProperty]
    private bool _isFocusVisualEnabled = true;

    [ObservableProperty]
    private bool _isReducedMotionEnabled;

    [ObservableProperty]
    private List<string> _textScaleOptions = new() { "75%", "100%", "125%", "150%", "200%" };

    [ObservableProperty]
    private string _selectedTextScale = "100%";

    [ObservableProperty]
    private List<string> _colorFilterOptions = new() { "None", "Protanopia", "Deuteranopia", "Tritanopia", "Achromatopsia", "High Contrast" };

    [ObservableProperty]
    private string _selectedColorFilter = "None";

    // Keyboard Navigation
    [ObservableProperty]
    private bool _isKeyboardNavigationEnabled = true;

    [ObservableProperty]
    private bool _showShortcutsInTooltips = true;

    // Audit
    [ObservableProperty]
    private double? _lastAuditScore;

    [ObservableProperty]
    private int _lastAuditIssues;

    [ObservableProperty]
    private bool _hasAuditReport;

    public AccessibilitySettingsViewModel(
        IAccessibilityService accessibilityService,
        AccessibilityAuditor auditor,
        IDialogService dialogService,
        ILogger<AccessibilitySettingsViewModel> logger)
    {
        _accessibilityService = accessibilityService;
        _auditor = auditor;
        _dialogService = dialogService;
        _logger = logger;

        // Load current settings
        LoadSettings();
    }

    private void LoadSettings()
    {
        IsHighContrastEnabled = _accessibilityService.IsHighContrastEnabled;
        IsReducedMotionEnabled = _accessibilityService.IsReducedMotionEnabled;
        IsKeyboardNavigationEnabled = _accessibilityService.IsKeyboardNavigationEnabled;
        
        var textScale = _accessibilityService.TextScaleFactor;
        SelectedTextScale = $"{textScale:P0}";
    }

    partial void OnIsScreenReaderEnabledChanged(bool value)
    {
        _ = value 
            ? _accessibilityService.EnableKeyboardNavigationAsync()
            : _accessibilityService.DisableKeyboardNavigationAsync();
    }

    partial void OnIsHighContrastEnabledChanged(bool value)
    {
        _ = value 
            ? _accessibilityService.EnableHighContrastAsync()
            : _accessibilityService.DisableHighContrastAsync();
    }

    partial void OnIsReducedMotionEnabledChanged(bool value)
    {
        _ = value 
            ? _accessibilityService.EnableReducedMotionAsync()
            : _accessibilityService.DisableReducedMotionAsync();
    }

    partial void OnSelectedTextScaleChanged(string value)
    {
        if (double.TryParse(value.TrimEnd('%'), out var scale))
        {
            _ = _accessibilityService.SetTextScaleAsync(scale / 100.0);
        }
    }

    partial void OnSelectedColorFilterChanged(string value)
    {
        if (value == "None")
        {
            _ = _accessibilityService.DisableColorFilterAsync();
        }
        else if (Enum.TryParse<ColorFilterType>(value.Replace(" ", ""), out var filter))
        {
            _ = _accessibilityService.EnableColorFilterAsync(filter);
        }
    }

    partial void OnIsKeyboardNavigationEnabledChanged(bool value)
    {
        _ = value 
            ? _accessibilityService.EnableKeyboardNavigationAsync()
            : _accessibilityService.DisableKeyboardNavigationAsync();
    }

    [RelayCommand]
    private async Task TestScreenReader()
    {
        await _accessibilityService.AnnounceAsync(
            "Screen reader test. If you can hear this message, screen reader support is working correctly.",
            AccessibilityPriority.High);
        
        _logger.LogInformation("Screen reader test announced");
    }

    [RelayCommand]
    private async Task RunAudit()
    {
        try
        {
            // Get the main window as root
            if (Avalonia.Application.Current?.ApplicationLifetime 
                is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                if (desktop.MainWindow != null)
                {
                    var result = await _auditor.AuditAsync(desktop.MainWindow);
                    
                    LastAuditScore = result.ComplianceScore;
                    LastAuditIssues = result.Issues.Count;
                    HasAuditReport = true;

                    // Export report
                    var reportPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "SaveStateReborn",
                        "accessibility-audit.html");
                    
                    await _auditor.ExportResultsAsync(result, reportPath);

                    await _dialogService.ShowMessageDialogAsync(
                        "Audit Complete",
                        $"Accessibility audit complete.\n\nScore: {result.ComplianceScore:F1}%\nIssues: {result.Issues.Count}\n\nReport saved to:\n{reportPath}");

                    _logger.LogInformation(
                        "Accessibility audit complete: {Score:F1}%, {Issues} issues",
                        result.ComplianceScore,
                        result.Issues.Count);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run accessibility audit");
        }
    }

    [RelayCommand]
    private async Task ViewAuditReport()
    {
        var reportPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SaveStateReborn",
            "accessibility-audit.html");

        if (File.Exists(reportPath))
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = reportPath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to open audit report");
            }
        }
    }

    [RelayCommand]
    private void OpenShortcutEditor()
    {
        // Navigate to shortcut editor
        _logger.LogInformation("Opening shortcut editor");
    }

    [RelayCommand]
    private async Task SaveSettings()
    {
        // Settings are saved automatically when changed
        await _accessibilityService.AnnounceAsync(
            "Accessibility settings saved",
            AccessibilityPriority.Normal);
        
        _logger.LogInformation("Accessibility settings saved");
    }
}
