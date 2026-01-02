namespace SaveState.Presentation.Resources;

using Microsoft.Extensions.Localization;

/// <summary>
/// Strongly-typed resource accessor for localized strings.
/// </summary>
public partial class Resources
{
    private readonly IStringLocalizer<Resources> _localizer;

    /// <summary>
    /// Initializes a new instance of the Resources class with localized strings.
    /// </summary>
    /// <param name="localizer">The string localizer for resource localization.</param>
    public Resources(IStringLocalizer<Resources> localizer)
    {
        _localizer = localizer;
    }

    // Application Common Strings
    /// <summary>
    /// Gets the application name localized string.
    /// </summary>
    public LocalizedString App_Name => _localizer["App_Name"];

    /// <summary>
    /// Gets the application description localized string.
    /// </summary>
    public LocalizedString App_Description => _localizer["App_Description"];

    // Navigation
    /// <summary>
    /// Gets the home navigation localized string.
    /// </summary>
    public LocalizedString Navigation_Home => _localizer["Navigation_Home"];

    /// <summary>
    /// Gets the games navigation localized string.
    /// </summary>
    public LocalizedString Navigation_Games => _localizer["Navigation_Games"];

    /// <summary>
    /// Gets the library navigation localized string.
    /// </summary>
    public LocalizedString Navigation_Library => _localizer["Navigation_Library"];

    /// <summary>
    /// Gets the settings navigation localized string.
    /// </summary>
    public LocalizedString Navigation_Settings => _localizer["Navigation_Settings"];

    // Buttons
    /// <summary>
    /// Gets the OK button localized string.
    /// </summary>
    public LocalizedString Button_OK => _localizer["Button_OK"];

    /// <summary>
    /// Gets the cancel button localized string.
    /// </summary>
    public LocalizedString Button_Cancel => _localizer["Button_Cancel"];

    /// <summary>
    /// Gets the save button localized string.
    /// </summary>
    public LocalizedString Button_Save => _localizer["Button_Save"];

    /// <summary>
    /// Gets the add button localized string.
    /// </summary>
    public LocalizedString Button_Add => _localizer["Button_Add"];

    /// <summary>
    /// Gets the remove button localized string.
    /// </summary>
    public LocalizedString Button_Remove => _localizer["Button_Remove"];

    /// <summary>
    /// Gets the import button localized string.
    /// </summary>
    public LocalizedString Button_Import => _localizer["Button_Import"];

    /// <summary>
    /// Gets the export button localized string.
    /// </summary>
    public LocalizedString Button_Export => _localizer["Button_Export"];

    /// <summary>
    /// Gets the search button localized string.
    /// </summary>
    public LocalizedString Button_Search => _localizer["Button_Search"];

    // Game Library
    /// <summary>
    /// Gets the game library title localized string.
    /// </summary>
    public LocalizedString GameLibrary_Title => _localizer["GameLibrary_Title"];

    /// <summary>
    /// Gets the no games message localized string.
    /// </summary>
    public LocalizedString GameLibrary_NoGames => _localizer["GameLibrary_NoGames"];

    /// <summary>
    /// Gets the search placeholder localized string.
    /// </summary>
    public LocalizedString GameLibrary_Search_Placeholder => _localizer["GameLibrary_Search_Placeholder"];

    /// <summary>
    /// Gets the platform filter localized string with parameter.
    /// </summary>
    /// <param name="platform">The platform parameter for filtering.</param>
    /// <returns>Localized string for platform filtering.</returns>
    public LocalizedString GameLibrary_Platform_Filter(object platform) =>
        _localizer["GameLibrary_Platform_Filter", platform];

    /// <summary>
    /// Gets the genre filter localized string with parameter.
    /// </summary>
    /// <param name="genre">The genre parameter for filtering.</param>
    /// <returns>Localized string for genre filtering.</returns>
    public LocalizedString GameLibrary_Genre_Filter(object genre) =>
        _localizer["GameLibrary_Genre_Filter", genre];

    // Game Details
    /// <summary>
    /// Gets the game title localized string.
    /// </summary>
    /// <summary>
    /// Gets the game title localized string.
    /// </summary>
    public LocalizedString Game_Title => _localizer["Game_Title"];

    /// <summary>
    /// Gets the game description localized string.
    /// </summary>
    public LocalizedString Game_Description => _localizer["Game_Description"];

    /// <summary>
    /// Gets the game platform localized string.
    /// </summary>
    public LocalizedString Game_Platform => _localizer["Game_Platform"];

    /// <summary>
    /// Gets the game genre localized string.
    /// </summary>
    public LocalizedString Game_Genre => _localizer["Game_Genre"];

    /// <summary>
    /// Gets the game release date localized string.
    /// </summary>
    public LocalizedString Game_ReleaseDate => _localizer["Game_ReleaseDate"];

    /// <summary>
    /// Gets the game developer localized string.
    /// </summary>
    public LocalizedString Game_Developer => _localizer["Game_Developer"];

    /// <summary>
    /// Gets the game publisher localized string.
    /// </summary>
    public LocalizedString Game_Publisher => _localizer["Game_Publisher"];

    // Settings
    /// <summary>
    /// Gets the settings title localized string.
    /// </summary>
    public LocalizedString Settings_Title => _localizer["Settings_Title"];

    /// <summary>
    /// Gets the general settings localized string.
    /// </summary>
    public LocalizedString Settings_General => _localizer["Settings_General"];

    /// <summary>
    /// Gets the appearance settings localized string.
    /// </summary>
    public LocalizedString Settings_Appearance => _localizer["Settings_Appearance"];

    /// <summary>
    /// Gets the language settings localized string.
    /// </summary>
    public LocalizedString Settings_Language => _localizer["Settings_Language"];

    /// <summary>
    /// Gets the language selection localized string.
    /// </summary>
    public LocalizedString Settings_Language_Select => _localizer["Settings_Language_Select"];

    /// <summary>
    /// Gets the theme settings localized string.
    /// </summary>
    public LocalizedString Settings_Theme => _localizer["Settings_Theme"];

    /// <summary>
    /// Gets the light theme localized string.
    /// </summary>
    public LocalizedString Settings_Theme_Light => _localizer["Settings_Theme_Light"];

    /// <summary>
    /// Gets the dark theme localized string.
    /// </summary>
    public LocalizedString Settings_Theme_Dark => _localizer["Settings_Theme_Dark"];

    /// <summary>
    /// Gets the system theme localized string.
    /// </summary>
    public LocalizedString Settings_Theme_System => _localizer["Settings_Theme_System"];

    // Status Messages
    /// <summary>
    /// Gets the loading status localized string.
    /// </summary>
    public LocalizedString Status_Loading => _localizer["Status_Loading"];

    /// <summary>
    /// Gets the saving status localized string.
    /// </summary>
    public LocalizedString Status_Saving => _localizer["Status_Saving"];

    /// <summary>
    /// Gets the error status localized string.
    /// </summary>
    public LocalizedString Status_Error => _localizer["Status_Error"];

    /// <summary>
    /// Gets the success status localized string.
    /// </summary>
    public LocalizedString Status_Success => _localizer["Status_Success"];

    // Error Messages
    /// <summary>
    /// Gets the generic error localized string.
    /// </summary>
    public LocalizedString Error_Generic => _localizer["Error_Generic"];

    /// <summary>
    /// Gets the network error localized string.
    /// </summary>
    public LocalizedString Error_Network => _localizer["Error_Network"];

    /// <summary>
    /// Gets the file not found error localized string.
    /// </summary>
    public LocalizedString Error_FileNotFound => _localizer["Error_FileNotFound"];

    /// <summary>
    /// Gets the invalid input error localized string.
    /// </summary>
    public LocalizedString Error_InvalidInput => _localizer["Error_InvalidInput"];

    // Confirmation Messages
    /// <summary>
    /// Gets the delete confirmation localized string.
    /// </summary>
    public LocalizedString Confirm_Delete => _localizer["Confirm_Delete"];

    /// <summary>
    /// Gets the exit confirmation localized string.
    /// </summary>
    public LocalizedString Confirm_Exit => _localizer["Confirm_Exit"];

    // Onboarding
    /// <summary>
    /// Gets the welcome onboarding localized string.
    /// </summary>
    public LocalizedString Onboarding_Welcome => _localizer["Onboarding_Welcome"];

    /// <summary>
    /// Gets the get started onboarding localized string.
    /// </summary>
    public LocalizedString Onboarding_GetStarted => _localizer["Onboarding_GetStarted"];

    /// <summary>
    /// Gets the skip onboarding localized string.
    /// </summary>
    public LocalizedString Onboarding_Skip => _localizer["Onboarding_Skip"];

    // Formatting Examples
    /// <summary>
    /// Gets the date formatting localized string.
    /// </summary>
    public LocalizedString Formatting_Date => _localizer["Formatting_Date"];

    /// <summary>
    /// Gets the number formatting localized string.
    /// </summary>
    public LocalizedString Formatting_Number => _localizer["Formatting_Number"];

    /// <summary>
    /// Gets the currency formatting localized string.
    /// </summary>
    public LocalizedString Formatting_Currency => _localizer["Formatting_Currency"];

    /// <summary>
    /// Gets the formatting example localized string.
    /// </summary>
    public LocalizedString Formatting_Example => _localizer["Formatting_Example"];
}
