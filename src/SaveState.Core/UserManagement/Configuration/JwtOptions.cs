using System.ComponentModel.DataAnnotations;

namespace SaveState.Core.UserManagement.Configuration;

/// <summary>
/// Configuration options for JWT token generation and validation.
/// </summary>
public class JwtOptions : IValidatableObject
{
    public const string Section = "Jwt";

    /// <summary>
    /// The issuer of the JWT tokens.
    /// </summary>
    [Required]
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// The audience for the JWT tokens.
    /// </summary>
    [Required]
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// The secret key used for signing JWT tokens.
    /// </summary>
    [Required]
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Access token expiration time in minutes.
    /// </summary>
    [Range(1, 1440)] // 1 minute to 24 hours
    public int AccessTokenExpirationMinutes { get; set; } = 60; // 1 hour

    /// <summary>
    /// Refresh token expiration time in days.
    /// </summary>
    [Range(1, 365)]
    public int RefreshTokenExpirationDays { get; set; } = 7; // 7 days

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var results = new List<ValidationResult>();

        if (string.IsNullOrWhiteSpace(SecretKey) || SecretKey.Length < 32)
        {
            results.Add(new ValidationResult("JWT secret key must be at least 32 characters long", new[] { nameof(SecretKey) }));
        }

        if (string.IsNullOrWhiteSpace(Issuer))
        {
            results.Add(new ValidationResult("JWT issuer is required", new[] { nameof(Issuer) }));
        }

        if (string.IsNullOrWhiteSpace(Audience))
        {
            results.Add(new ValidationResult("JWT audience is required", new[] { nameof(Audience) }));
        }

        return results;
    }
}
