using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SaveState.Core.UserManagement.Configuration;
using SaveState.Core.UserManagement.Services;

namespace SaveState.Infrastructure.UserManagement;

/// <summary>
/// Secure password hashing implementation using PBKDF2.
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    private readonly AuthenticationOptions _options;
    private readonly ILogger<PasswordHasher> _logger;
    private const int SaltSize = 16; // 128 bits
    private const int KeySize = 32; // 256 bits
    private const int Iterations = 10000; // PBKDF2 iterations

    public PasswordHasher(IOptions<AuthenticationOptions> options, ILogger<PasswordHasher> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public (string hash, string salt) HashPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password cannot be null or empty", nameof(password));

        using var algorithm = new Rfc2898DeriveBytes(
            password,
            SaltSize,
            Iterations,
            HashAlgorithmName.SHA256);

        var salt = algorithm.Salt;
        var key = algorithm.GetBytes(KeySize);

        var hash = Convert.ToBase64String(key);
        var saltString = Convert.ToBase64String(salt);

        return (hash, saltString);
    }

    public bool VerifyPassword(string password, string hash, string salt)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hash) || string.IsNullOrEmpty(salt))
            return false;

        try
        {
            var saltBytes = Convert.FromBase64String(salt);
            var hashBytes = Convert.FromBase64String(hash);

            using var algorithm = new Rfc2898DeriveBytes(
                password,
                saltBytes,
                Iterations,
                HashAlgorithmName.SHA256);

            var keyToCheck = algorithm.GetBytes(KeySize);
            return keyToCheck.SequenceEqual(hashBytes);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Password verification failed due to an exception");
            return false;
        }
    }

    public PasswordValidationResult ValidatePasswordStrength(string password)
    {
        var errors = new List<string>();

        if (string.IsNullOrEmpty(password))
        {
            errors.Add("Password is required");
            return PasswordValidationResult.Failure(errors);
        }

        if (password.Length < _options.PasswordPolicy.MinimumLength)
        {
            errors.Add($"Password must be at least {_options.PasswordPolicy.MinimumLength} characters long");
        }

        if (_options.PasswordPolicy.RequireUppercase && !Regex.IsMatch(password, @"[A-Z]"))
        {
            errors.Add("Password must contain at least one uppercase letter");
        }

        if (_options.PasswordPolicy.RequireLowercase && !Regex.IsMatch(password, @"[a-z]"))
        {
            errors.Add("Password must contain at least one lowercase letter");
        }

        if (_options.PasswordPolicy.RequireDigit && !Regex.IsMatch(password, @"[0-9]"))
        {
            errors.Add("Password must contain at least one digit");
        }

        if (_options.PasswordPolicy.RequireSpecialCharacter && !Regex.IsMatch(password, @"[^a-zA-Z0-9]"))
        {
            errors.Add("Password must contain at least one special character");
        }

        // Check for common weak patterns
        if (Regex.IsMatch(password, @"(.)\1{2,}"))
        {
            errors.Add("Password cannot contain three or more consecutive identical characters");
        }

        if (Regex.IsMatch(password.ToLower(), @"password|123456|qwerty|admin"))
        {
            errors.Add("Password is too common or easily guessable");
        }

        return errors.Any()
            ? PasswordValidationResult.Failure(errors)
            : PasswordValidationResult.Success;
    }
}
