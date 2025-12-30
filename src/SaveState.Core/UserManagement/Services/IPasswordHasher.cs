namespace SaveState.Core.UserManagement.Services;

/// <summary>
/// Service for securely hashing and verifying passwords.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hashes a plain text password and returns the hash and salt.
    /// </summary>
    (string hash, string salt) HashPassword(string password);

    /// <summary>
    /// Verifies that a plain text password matches the provided hash and salt.
    /// </summary>
    bool VerifyPassword(string password, string hash, string salt);

    /// <summary>
    /// Validates password strength according to security policies.
    /// </summary>
    PasswordValidationResult ValidatePasswordStrength(string password);
}

/// <summary>
/// Result of password strength validation.
/// </summary>
public class PasswordValidationResult
{
    public bool IsValid { get; set; }
    public IEnumerable<string> Errors { get; set; } = Array.Empty<string>();

    public static PasswordValidationResult Success => new() { IsValid = true };

    public static PasswordValidationResult Failure(IEnumerable<string> errors) =>
        new() { IsValid = false, Errors = errors };
}
