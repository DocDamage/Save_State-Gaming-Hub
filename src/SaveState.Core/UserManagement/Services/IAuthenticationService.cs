using SaveState.Core.Common;
using SaveState.Core.UserManagement.Entities;

namespace SaveState.Core.UserManagement.Services;

/// <summary>
/// Service for handling user authentication operations.
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Authenticates a user with username/email and password.
    /// </summary>
    Task<AuthenticationResult> AuthenticateAsync(string usernameOrEmail, string password, CancellationToken ct = default);

    /// <summary>
    /// Validates an API key and returns the associated user.
    /// </summary>
    Task<Result<User>> ValidateApiKeyAsync(string apiKey, CancellationToken ct = default);

    /// <summary>
    /// Refreshes an access token using a refresh token.
    /// </summary>
    Task<TokenRefreshResult> RefreshTokenAsync(string refreshToken, CancellationToken ct = default);
}

/// <summary>
/// Result of authentication operation.
/// </summary>
public class AuthenticationResult
{
    public bool IsSuccessful { get; set; }
    public User? User { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public string? ErrorMessage { get; set; }

    public static AuthenticationResult Success(User user, string accessToken, string refreshToken) =>
        new()
        {
            IsSuccessful = true,
            User = user,
            AccessToken = accessToken,
            RefreshToken = refreshToken
        };

    public static AuthenticationResult Failure(string errorMessage) =>
        new()
        {
            IsSuccessful = false,
            ErrorMessage = errorMessage
        };
}

/// <summary>
/// Result of token refresh operation.
/// </summary>
public class TokenRefreshResult
{
    public bool IsSuccessful { get; set; }
    public string? AccessToken { get; set; }
    public string? ErrorMessage { get; set; }

    public static TokenRefreshResult Success(string accessToken) =>
        new()
        {
            IsSuccessful = true,
            AccessToken = accessToken
        };

    public static TokenRefreshResult Failure(string errorMessage) =>
        new()
        {
            IsSuccessful = false,
            ErrorMessage = errorMessage
        };
}
