namespace SaveState.Presentation.Resources;

using Microsoft.Extensions.Localization;

/// <summary>
/// Strongly-typed resource accessor for localized strings.
/// </summary>
public partial class Resources
{
    private readonly IStringLocalizer<Resources> _localizer;

    public Resources(IStringLocalizer<Resources> localizer)
    {
        _localizer = localizer;
    }

    // Application Common Strings
    public LocalizedString App_Name => _localizer["App_Name"];
    public LocalizedString App_Description => _localizer["App_Description"];

    // Navigation
    public LocalizedString Navigation_Home => _localizer["Navigation_Home"];
    public LocalizedString Navigation_Games => _localizer["Navigation_Games"];
    public LocalizedString Navigation_Library => _localizer["Navigation_Library"];
    public LocalizedString Navigation_Settings => _localizer["Navigation_Settings"];

    // Buttons
    public LocalizedString Button_OK => _localizer["Button_OK"];
    public LocalizedString Button_Cancel => _localizer["Button_Cancel"];
    public LocalizedString Button_Save => _localizer["Button_Save"];
    public LocalizedString Button_Add => _localizer["Button_Add"];
    public LocalizedString Button_Remove => _localizer["Button_Remove"];
    public LocalizedString Button_Import => _localizer["Button_Import"];
    public LocalizedString Button_Export => _localizer["Button_Export"];
    public LocalizedString Button_Search => _localizer["Button_Search"];

    // Game Library
    public LocalizedString GameLibrary_Title => _localizer["GameLibrary_Title"];
    public LocalizedString GameLibrary_NoGames => _localizer["GameLibrary_NoGames"];
    public LocalizedString GameLibrary_Search_Placeholder => _localizer["GameLibrary_Search_Placeholder"];

    public LocalizedString GameLibrary_Platform_Filter(object platform) =>
        _localizer["GameLibrary_Platform_Filter", platform];

    public LocalizedString GameLibrary_Genre_Filter(object genre) =>
        _localizer["GameLibrary_Genre_Filter", genre];

    // Game Details
    public LocalizedString Game_Title => _localizer["Game_Title"];
    public LocalizedString Game_Description => _localizer["Game_Description"];
    public LocalizedString Game_Platform => _localizer["Game_Platform"];
    public LocalizedString Game_Genre => _localizer["Game_Genre"];
    public LocalizedString Game_ReleaseDate => _localizer["Game_ReleaseDate"];
    public LocalizedString Game_Developer => _localizer["Game_Developer"];
    public LocalizedString Game_Publisher => _localizer["Game_Publisher"];

    // Settings
    public LocalizedString Settings_Title => _localizer["Settings_Title"];
    public LocalizedString Settings_General => _localizer["Settings_General"];
    public LocalizedString Settings_Appearance => _localizer["Settings_Appearance"];
    public LocalizedString Settings_Language => _localizer["Settings_Language"];
    public LocalizedString Settings_Language_Select => _localizer["Settings_Language_Select"];
    public LocalizedString Settings_Theme => _localizer["Settings_Theme"];
    public LocalizedString Settings_Theme_Light => _localizer["Settings_Theme_Light"];
    public LocalizedString Settings_Theme_Dark => _localizer["Settings_Theme_Dark"];
    public LocalizedString Settings_Theme_System => _localizer["Settings_Theme_System"];

    // Status Messages
    public LocalizedString Status_Loading => _localizer["Status_Loading"];
    public LocalizedString Status_Saving => _localizer["Status_Saving"];
    public LocalizedString Status_Error => _localizer["Status_Error"];
    public LocalizedString Status_Success => _localizer["Status_Success"];

    // Error Messages
    public LocalizedString Error_Generic => _localizer["Error_Generic"];
    public LocalizedString Error_Network => _localizer["Error_Network"];
    public LocalizedString Error_FileNotFound => _localizer["Error_FileNotFound"];
    public LocalizedString Error_InvalidInput => _localizer["Error_InvalidInput"];

    // Confirmation Messages
    public LocalizedString Confirm_Delete => _localizer["Confirm_Delete"];
    public LocalizedString Confirm_Exit => _localizer["Confirm_Exit"];

    // Onboarding
    public LocalizedString Onboarding_Welcome => _localizer["Onboarding_Welcome"];
    public LocalizedString Onboarding_GetStarted => _localizer["Onboarding_GetStarted"];
    public LocalizedString Onboarding_Skip => _localizer["Onboarding_Skip"];

    // Formatting Examples
    public LocalizedString Formatting_Date => _localizer["Formatting_Date"];
    public LocalizedString Formatting_Number => _localizer["Formatting_Number"];
    public LocalizedString Formatting_Currency => _localizer["Formatting_Currency"];
    public LocalizedString Formatting_Example => _localizer["Formatting_Example"];
}
