using System.ComponentModel.DataAnnotations;

namespace SaveState.Core.UserManagement.Configuration;

/// <summary>
/// Configuration options for authentication behavior.
/// </summary>
public class AuthenticationOptions : IValidatableObject
{
    public const string Section = "Authentication";

    /// <summary>
    /// Whether authentication is required globally.
    /// </summary>
    public bool RequireAuthentication { get; set; } = true;

    /// <summary>
    /// Password policy settings.
    /// </summary>
    public PasswordPolicyOptions PasswordPolicy { get; set; } = new();

    /// <summary>
    /// Account lockout settings.
    /// </summary>
    public LockoutOptions Lockout { get; set; } = new();

    /// <summary>
    /// API key settings.
    /// </summary>
    public ApiKeyOptions ApiKeys { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var results = new List<ValidationResult>();

        if (PasswordPolicy != null)
        {
            var passwordResults = PasswordPolicy.Validate(new ValidationContext(PasswordPolicy));
            results.AddRange(passwordResults);
        }

        if (Lockout != null)
        {
            var lockoutResults = Lockout.Validate(new ValidationContext(Lockout));
            results.AddRange(lockoutResults);
        }

        if (ApiKeys != null)
        {
            var apiKeyResults = ApiKeys.Validate(new ValidationContext(ApiKeys));
            results.AddRange(apiKeyResults);
        }

        return results;
    }
}

/// <summary>
/// Password policy configuration.
/// </summary>
public class PasswordPolicyOptions : IValidatableObject
{
    /// <summary>
    /// Minimum password length.
    /// </summary>
    [Range(8, 128)]
    public int MinimumLength { get; set; } = 8;

    /// <summary>
    /// Whether passwords must contain uppercase letters.
    /// </summary>
    public bool RequireUppercase { get; set; } = true;

    /// <summary>
    /// Whether passwords must contain lowercase letters.
    /// </summary>
    public bool RequireLowercase { get; set; } = true;

    /// <summary>
    /// Whether passwords must contain digits.
    /// </summary>
    public bool RequireDigit { get; set; } = true;

    /// <summary>
    /// Whether passwords must contain special characters.
    /// </summary>
    public bool RequireSpecialCharacter { get; set; } = true;

    /// <summary>
    /// Number of recent passwords to prevent reuse.
    /// </summary>
    [Range(0, 10)]
    public int PreventReuseCount { get; set; } = 3;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var results = new List<ValidationResult>();

        if (MinimumLength < 8)
        {
            results.Add(new ValidationResult("Minimum password length should be at least 8 characters for security", new[] { nameof(MinimumLength) }));
        }

        return results;
    }
}

/// <summary>
/// Account lockout configuration.
/// </summary>
public class LockoutOptions : IValidatableObject
{
    /// <summary>
    /// Whether account lockout is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Maximum number of failed login attempts before lockout.
    /// </summary>
    [Range(3, 10)]
    public int MaxFailedAttempts { get; set; } = 5;

    /// <summary>
    /// Lockout duration in minutes.
    /// </summary>
    [Range(5, 1440)] // 5 minutes to 24 hours
    public int LockoutDurationMinutes { get; set; } = 30;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var results = new List<ValidationResult>();

        if (Enabled && MaxFailedAttempts < 3)
        {
            results.Add(new ValidationResult("Maximum failed attempts should be at least 3 when lockout is enabled", new[] { nameof(MaxFailedAttempts) }));
        }

        return results;
    }
}

/// <summary>
/// API key configuration.
/// </summary>
public class ApiKeyOptions : IValidatableObject
{
    /// <summary>
    /// Whether API keys are enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Default expiration for API keys in days (null = no expiration).
    /// </summary>
    [Range(1, 3650)] // 1 day to 10 years
    public int? DefaultExpirationDays { get; set; } = 365;

    /// <summary>
    /// Maximum number of API keys per user.
    /// </summary>
    [Range(1, 100)]
    public int MaxKeysPerUser { get; set; } = 10;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var results = new List<ValidationResult>();

        if (Enabled && MaxKeysPerUser < 1)
        {
            results.Add(new ValidationResult("Maximum keys per user must be at least 1 when API keys are enabled", new[] { nameof(MaxKeysPerUser) }));
        }

        return results;
    }
}
