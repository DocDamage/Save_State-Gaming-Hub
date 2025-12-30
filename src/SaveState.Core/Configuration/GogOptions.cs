using System.ComponentModel.DataAnnotations;

namespace SaveState.Core.Configuration;

public class GogOptions
{
    [Required(ErrorMessage = "Username is required")]
    [MinLength(1, ErrorMessage = "Username cannot be empty")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [MinLength(1, ErrorMessage = "Password cannot be empty")]
    public string Password { get; set; } = string.Empty; // In a real app, this would be an OAuth token
}
