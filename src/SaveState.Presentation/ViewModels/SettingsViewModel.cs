using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Core.Common.Services;
using SaveState.Presentation.Resources;

namespace SaveState.Presentation.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ICultureManager _cultureManager;
    private readonly SaveState.Presentation.Resources.Resources _resources;

    [ObservableProperty]
    private CultureInfo _selectedCulture;

    [ObservableProperty]
    private string _testLocalizedString;

    [ObservableProperty]
    private string _formattedDate;

    [ObservableProperty]
    private string _formattedNumber;

    [ObservableProperty]
    private string _formattedCurrency;

    public SettingsViewModel(
        ICultureManager cultureManager,
        SaveState.Presentation.Resources.Resources resources)
    {
        _cultureManager = cultureManager;
        _resources = resources;
        _selectedCulture = _cultureManager.CurrentCulture;
        UpdateTestString();
        UpdateFormattingExamples();
    }

    public IReadOnlyList<CultureInfo> SupportedCultures => _cultureManager.SupportedCultures;

    public bool IsRightToLeft => _cultureManager.IsRightToLeft(_selectedCulture);

    // Localized properties
    public string Title => _resources.Settings_Title;
    public string GeneralSection => _resources.Settings_General;
    public string LanguageSection => _resources.Settings_Language;
    public string LanguageSelectLabel => _resources.Settings_Language_Select;
    public string TestSection => "Localization Test";
    public string FormattingSection => "Culture Formatting";
    public string DateLabel => _resources.Formatting_Date;
    public string NumberLabel => _resources.Formatting_Number;
    public string CurrencyLabel => _resources.Formatting_Currency;

    [RelayCommand]
    private async Task ChangeCultureAsync(CultureInfo culture)
    {
        if (culture == null || culture == _selectedCulture)
            return;

        var success = await _cultureManager.SetCultureAsync(culture.Name);
        if (success)
        {
            SelectedCulture = culture;
            UpdateTestString();
            UpdateFormattingExamples();
        }
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
