using System.Globalization;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Core.Common.Services;
using SaveState.Core.Ai.Services;
using SaveState.Presentation.Resources;
using SaveState.Presentation.Services;
using SaveState.Presentation.ViewModels.Shell;
using SaveState.Presentation.ViewModels.Settings;

namespace SaveState.Presentation.ViewModels;

/// <summary>
/// View model for application settings and preferences.
/// Manages user preferences, localization, themes, and AI configuration.
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly ICultureManager _cultureManager;
    private readonly SaveState.Presentation.Resources.Resources _resources;
    private readonly IThemeService _themeService;
    private readonly IAiOrchestrator _aiOrchestrator;
    private readonly IUserPreferencesService _preferencesService;
    private readonly IDialogService _dialogService;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private CultureInfo _selectedCulture;

    [ObservableProperty]
    private string? _testLocalizedString;

    [ObservableProperty]
    private string? _formattedDate;

    [ObservableProperty]
    private string? _formattedNumber;

    [ObservableProperty]
    private string? _formattedCurrency;

    [ObservableProperty]
    private ThemeType _selectedTheme;

    [ObservableProperty]
    private string _selectedAiProvider = "OpenAI"; // Added

    [ObservableProperty]
    private string _selectedAiModel = "gpt-4"; // Added

    [ObservableProperty]
    private ObservableCollection<string> _availableAiProviders = new(); // Added

    [ObservableProperty]
    private ObservableCollection<string> _availableAiModels = new(); // Added

    [ObservableProperty]
    private string _openAiApiKey = string.Empty;

    [ObservableProperty]
    private string _groqApiKey = string.Empty;

    public VoiceControlViewModel VoiceSettings { get; }
    public AudioOptimizationViewModel AudioOptimizationViewModel { get; }
    public SystemHealthViewModel SystemHealthViewModel { get; }
    public ConnectedAccountsViewModel ConnectedAccountsViewModel { get; }
    public DataManagementViewModel DataManagementViewModel { get; }
    public AiAdministrationViewModel AiAdministrationViewModel { get; }

    public SettingsViewModel(
        ICultureManager cultureManager,
        SaveState.Presentation.Resources.Resources resources,
        IThemeService themeService,
        IAiOrchestrator aiOrchestrator,
        IUserPreferencesService preferencesService,
        IDialogService dialogService,
        SaveState.Presentation.ViewModels.Shell.VoiceControlViewModel voiceSettings,
        AudioOptimizationViewModel audioOptimizationViewModel,
        SystemHealthViewModel systemHealthViewModel,
        ConnectedAccountsViewModel connectedAccountsViewModel,
        DataManagementViewModel dataManagementViewModel,
        AiAdministrationViewModel aiAdministrationViewModel,
        INavigationService navigationService)
    {
        _cultureManager = cultureManager;
        _resources = resources;
        _themeService = themeService;
        _aiOrchestrator = aiOrchestrator;
        _preferencesService = preferencesService;
        _dialogService = dialogService;
        _navigationService = navigationService;
        VoiceSettings = voiceSettings;
        AudioOptimizationViewModel = audioOptimizationViewModel;
        SystemHealthViewModel = systemHealthViewModel;
        ConnectedAccountsViewModel = connectedAccountsViewModel;
        DataManagementViewModel = dataManagementViewModel;
        AiAdministrationViewModel = aiAdministrationViewModel;

        SelectedCulture = _cultureManager.CurrentCulture;
        SelectedTheme = _themeService.CurrentTheme;

        // Initialize AI options
        _ = LoadAiPreferencesAsync();

        // Subscribe to theme changes
        _themeService.ThemeChanged += (sender, theme) => SelectedTheme = theme;

        UpdateTestString();
        UpdateFormattingExamples();
    }

    public IReadOnlyList<CultureInfo> SupportedCultures => _cultureManager.SupportedCultures;

    public bool IsRightToLeft => _cultureManager.IsRightToLeft(SelectedCulture);

    public IReadOnlyList<ThemeType> AvailableThemes => _themeService.AvailableThemes;

    // Localized properties
    public string Title => _resources.Settings_Title;
    public string GeneralSection => _resources.Settings_General;
    public string LanguageSection => _resources.Settings_Language;
    public string LanguageSelectLabel => _resources.Settings_Language_Select;
    public string ThemeSection => "Appearance";
    public string ThemeSelectLabel => "Theme";
    public string TestSection => "Localization Test";
    public string FormattingSection => "Culture Formatting";
    public string DateLabel => _resources.Formatting_Date;
    public string NumberLabel => _resources.Formatting_Number;
    public string CurrencyLabel => _resources.Formatting_Currency;

    [RelayCommand]
    private async Task ChangeCultureAsync(CultureInfo culture)
    {
        if (culture == null || culture == SelectedCulture)
            return;

        var success = await _cultureManager.SetCultureAsync(culture.Name);
        if (success)
        {
            SelectedCulture = culture;
            UpdateTestString();
            UpdateFormattingExamples();
        }
    }

    [RelayCommand]
    private void ChangeTheme(ThemeType theme)
    {
        if (theme == SelectedTheme)
            return;

        _themeService.SetTheme(theme);
        // SelectedTheme will be updated via the ThemeChanged event
    }

    [RelayCommand]
    private async Task OpenEmulatorSetupAsync()
    {
        await _dialogService.ShowEmulatorSetupWizardAsync();
    }

    [RelayCommand]
    private void OpenSystemHealth()
    {
        // Navigate to System Health view - the view is accessed via the ViewModel property
        // This can be expanded to show the view in a dialog or navigate to it
    }

    [RelayCommand]
    private void OpenConnectedAccounts()
    {
        // Navigate to Connected Accounts view
    }

    [RelayCommand]
    private void OpenDataManagement()
    {
        // Navigate to Data Management view
    }

    [RelayCommand]
    private void OpenAiAdministration()
    {
        // Navigate to AI Administration view
    }

    [RelayCommand]
    private async Task ShowSystemHealthAsync()
    {
        await _navigationService.NavigateToAsync("Settings");
    }

    [RelayCommand]
    private async Task ShowConnectedAccountsAsync()
    {
        await _navigationService.NavigateToAsync("Settings");
    }

    [RelayCommand]
    private async Task ShowLaunchExperienceConfigAsync()
    {
        await _dialogService.ShowLaunchExperienceConfigAsync();
    }

    private async Task LoadAiPreferencesAsync()
    {
        SelectedAiProvider = await _preferencesService.GetPreferredAiProviderAsync();
        SelectedAiModel = await _preferencesService.GetPreferredAiModelAsync();
        OpenAiApiKey = await _preferencesService.GetAiApiKeyAsync("OpenAI");
        GroqApiKey = await _preferencesService.GetAiApiKeyAsync("Groq");
        RefreshAiOptions();
    }

    private void RefreshAiOptions()
    {
        AvailableAiProviders.Clear();
        foreach (var provider in _aiOrchestrator.GetAvailableProviders())
        {
            AvailableAiProviders.Add(provider);
        }

        if (AvailableAiProviders.Count > 0 && string.IsNullOrEmpty(SelectedAiProvider))
        {
            SelectedAiProvider = AvailableAiProviders[0];
        }
    }

    async partial void OnSelectedAiProviderChanged(string value)
    {
        if (string.IsNullOrEmpty(value)) return;

        await _preferencesService.SetPreferredAiProviderAsync(value);

        AvailableAiModels.Clear();
        if (value == "OpenAI")
        {
            AvailableAiModels.Add("gpt-4");
            AvailableAiModels.Add("gpt-3.5-turbo");
        }
        else if (value == "Groq")
        {
            AvailableAiModels.Add("llama3-70b-8192");
            AvailableAiModels.Add("mixtral-8x7b-32768");
        }
        else if (value == "Embedded (Local)")
        {
            AvailableAiModels.Add("phi-3-mini");
            AvailableAiModels.Add("mistral-tiny");
            AvailableAiModels.Add("llama-3-8b");
            AvailableAiModels.Add("savestate-base");
        }

        if (AvailableAiModels.Count > 0)
        {
            var savedModel = await _preferencesService.GetPreferredAiModelAsync();
            if (AvailableAiModels.Contains(savedModel))
            {
                SelectedAiModel = savedModel;
            }
            else
            {
                SelectedAiModel = AvailableAiModels[0];
            }
        }
    }

    async partial void OnSelectedAiModelChanged(string value)
    {
        if (string.IsNullOrEmpty(value)) return;
        await _preferencesService.SetPreferredAiModelAsync(value);
    }

    async partial void OnOpenAiApiKeyChanged(string value)
    {
        await _preferencesService.SetAiApiKeyAsync("OpenAI", value);
    }

    async partial void OnGroqApiKeyChanged(string value)
    {
        await _preferencesService.SetAiApiKeyAsync("Groq", value);
    }

    private void UpdateTestString()
    {
        // Show a test string to demonstrate localization is working
        TestLocalizedString = $"{_resources.App_Name} - {_resources.Settings_Title}";
    }

    private void UpdateFormattingExamples()
    {
        // Sample data for formatting examples
        var sampleDate = new DateTime(2024, 12, 30);
        const double sampleNumber = 1234567.89;
        const decimal sampleCurrency = 1234.56m;

        FormattedDate = _cultureManager.FormatDate(sampleDate);
        FormattedNumber = _cultureManager.FormatNumber(sampleNumber);
        FormattedCurrency = _cultureManager.FormatCurrency(sampleCurrency);
    }
}
