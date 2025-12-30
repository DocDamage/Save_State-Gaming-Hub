using System.ComponentModel.DataAnnotations;

namespace SaveState.Core.Configuration;

public class SteamOptions
{
    [Required(ErrorMessage = "ApiKey is required")]
    [MinLength(1, ErrorMessage = "ApiKey cannot be empty")]
    public string ApiKey { get; set; } = string.Empty;

    [Required(ErrorMessage = "SteamId is required")]
    [MinLength(1, ErrorMessage = "SteamId cannot be empty")]
    [RegularExpression(@"^\d+$", ErrorMessage = "SteamId must be a valid numeric Steam ID")]
    public string SteamId { get; set; } = string.Empty;
}
