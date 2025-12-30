using System.ComponentModel.DataAnnotations;

namespace SaveState.Core.Configuration;

public class EpicOptions
{
    [Required(ErrorMessage = "AccountId is required")]
    [MinLength(1, ErrorMessage = "AccountId cannot be empty")]
    public string AccountId { get; set; } = string.Empty;

    [Required(ErrorMessage = "AuthToken is required")]
    [MinLength(1, ErrorMessage = "AuthToken cannot be empty")]
    public string AuthToken { get; set; } = string.Empty;
}
