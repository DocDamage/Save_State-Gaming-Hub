using System.ComponentModel.DataAnnotations;

namespace SaveState.Core.Configuration;

public class IgdbOptions
{
    [Required(ErrorMessage = "ClientId is required")]
    [MinLength(1, ErrorMessage = "ClientId cannot be empty")]
    public string ClientId { get; set; } = string.Empty;

    [Required(ErrorMessage = "ClientSecret is required")]
    [MinLength(1, ErrorMessage = "ClientSecret cannot be empty")]
    public string ClientSecret { get; set; } = string.Empty;
}
