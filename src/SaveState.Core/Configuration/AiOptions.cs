using System.ComponentModel.DataAnnotations;

namespace SaveState.Core.Configuration;

public class AiOptions
{
    public const string Section = "Ai";

    [Required(ErrorMessage = "DefaultModel is required")]
    [MinLength(1, ErrorMessage = "DefaultModel cannot be empty")]
    public string DefaultModel { get; set; } = "gpt-4";

    [Range(5, 1440, ErrorMessage = "CacheTtlMinutes must be between 5 and 1440 minutes")]
    public int CacheTtlMinutes { get; set; } = 30;

    public bool EnableFallback { get; set; } = true;

    [Range(1, 100, ErrorMessage = "MaxConcurrentRequests must be between 1 and 100")]
    public int MaxConcurrentRequests { get; set; } = 10;

    /// <summary>
    /// Session timeout in minutes for conversation contexts. Default: 30.
    /// </summary>
    [Range(1, 1440)]
    public int SessionTimeoutMinutes { get; set; } = 30;
}
