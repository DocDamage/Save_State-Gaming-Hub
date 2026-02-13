using Microsoft.Extensions.Logging;
using SaveState.Core.Common.Constants;
using SaveState.Core.UserManagement.Entities;
using SaveState.Core.UserManagement.Repositories;
using SaveState.Core.UserManagement.Services;

namespace SaveState.Application.UserManagement;

/// <summary>
/// Authentication service implementation.
/// </summary>
public class AuthenticationService : IAuthenticationService
{
    private readonly IUserRepository _userRepository;
    private readonly IApiKeyRepository _apiKeyRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(
        IUserRepository userRepository,
        IApiKeyRepository apiKeyRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        ILogger<AuthenticationService> logger)
    {
        _userRepository = userRepository;
        _apiKeyRepository = apiKeyRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    public async Task<AuthenticationResult> AuthenticateAsync(string usernameOrEmail, string password, CancellationToken ct = default)
    {
        try
        {
            // Find user by username or email
            var user = await _userRepository.GetByUsernameAsync(usernameOrEmail, ct) ??
                      await _userRepository.GetByEmailAsync(usernameOrEmail, ct);

            if (user == null || !user.IsActive)
            {
                return AuthenticationResult.Failure(ErrorMessages.InvalidCredentials);
            }

            // Verify password
            if (!_passwordHasher.VerifyPassword(password, user.PasswordHash, user.PasswordSalt))
            {
                return AuthenticationResult.Failure(ErrorMessages.InvalidCredentials);
            }

            // Generate tokens
            var accessToken = await _jwtTokenService.GenerateAccessTokenAsync(user, ct);
            var refreshToken = await _jwtTokenService.GenerateRefreshTokenAsync(user, ct);

            // Update last login
            user.UpdateLastLogin();
            await _userRepository.UpdateAsync(user, ct);

            return AuthenticationResult.Success(user, accessToken, refreshToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Authentication failed for user {UsernameOrEmail}", usernameOrEmail);
            return AuthenticationResult.Failure(ErrorMessages.AuthenticationFailed);
        }
    }

    public async Task<Result<User>> ValidateApiKeyAsync(string apiKey, CancellationToken ct = default)
    {
        try
        {
            var user = await _apiKeyRepository.GetUserByApiKeyAsync(apiKey, ct);
            if (user == null)
            {
                return Result.Failure<User>(ErrorMessages.InvalidApiKey, ErrorType.Validation);
            }
            return Result.Success<User>(user);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "API Key validation failed");
            return Result.Failure<User>(ErrorMessages.AuthenticationFailed, ErrorType.Validation);
        }
    }

    public async Task<TokenRefreshResult> RefreshTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        try
        {
            // Validate the refresh token
            var principalResult = await _jwtTokenService.ValidateTokenAsync(refreshToken, ct);
            if (principalResult.IsFailure)
            {
                return TokenRefreshResult.Failure(ErrorMessages.InvalidRefreshToken);
            }
            var principal = principalResult.Value;

            // Check if it's actually a refresh token
            var tokenTypeClaim = principal.FindFirst("token_type");
            if (tokenTypeClaim?.Value != "refresh")
            {
                return TokenRefreshResult.Failure(ErrorMessages.InvalidTokenType);
            }

            // Get user ID from token
            var userIdResult = await _jwtTokenService.GetUserIdFromTokenAsync(refreshToken, ct);
            if (userIdResult.IsFailure)
            {
                return TokenRefreshResult.Failure(ErrorMessages.InvalidRefreshToken);
            }
            var userId = userIdResult.Value;

            // Get user and check if still active
            var user = await _userRepository.GetByIdAsync(userId, ct);
            if (user == null || !user.IsActive)
            {
                return TokenRefreshResult.Failure(ErrorMessages.UserInactive);
            }

            // Generate new access token
            var accessToken = await _jwtTokenService.GenerateAccessTokenAsync(user, ct);
            return TokenRefreshResult.Success(accessToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token refresh failed");
            return TokenRefreshResult.Failure(ErrorMessages.TokenRefreshFailed);
        }
    }
}

