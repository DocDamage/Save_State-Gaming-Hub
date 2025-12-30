using System.ComponentModel.DataAnnotations;

namespace SaveState.Core.Configuration;

public class LocalizationOptions
{
    public const string Section = "Localization";

    [Required(ErrorMessage = "DefaultCulture is required")]
    [MinLength(2, ErrorMessage = "DefaultCulture must be at least 2 characters")]
    [RegularExpression(@"^[a-z]{2}(-[A-Z]{2})?$", ErrorMessage = "DefaultCulture must be in format 'xx' or 'xx-XX'")]
    public string DefaultCulture { get; set; } = "en-US";

    [Required(ErrorMessage = "SupportedCultures is required")]
    [MinLength(1, ErrorMessage = "At least one supported culture must be specified")]
    public string[] SupportedCultures { get; set; } = ["en-US", "es-ES", "fr-FR", "de-DE", "ja-JP", "zh-CN", "ar-SA", "he-IL"];

    [Range(1, 365, ErrorMessage = "CacheDurationDays must be between 1 and 365")]
    public int CacheDurationDays { get; set; } = 30;

    public bool EnableCultureFallback { get; set; } = true;

    public bool EnableRightToLeftSupport { get; set; } = true;
}
