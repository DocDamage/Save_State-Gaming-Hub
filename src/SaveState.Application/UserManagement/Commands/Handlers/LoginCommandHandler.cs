using MediatR;
using Microsoft.Extensions.Logging;
using SaveState.Application.UserManagement.Commands;
using SaveState.Core.Common;
using SaveState.Core.UserManagement.Services;

namespace SaveState.Application.UserManagement.Commands.Handlers;

public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<LoginResponse>>
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        IAuthenticationService authenticationService,
        ILogger<LoginCommandHandler> logger)
    {
        _authenticationService = authenticationService;
        _logger = logger;
    }

    public async Task<Result<LoginResponse>> Handle(LoginCommand request, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Attempting login for user: {UsernameOrEmail}", request.UsernameOrEmail);

            var authResult = await _authenticationService.AuthenticateAsync(
                request.UsernameOrEmail,
                request.Password,
                ct);

            if (!authResult.IsSuccessful || authResult.User == null)
            {
                _logger.LogWarning("Login failed for user: {UsernameOrEmail}. Reason: {Error}",
                    request.UsernameOrEmail, authResult.ErrorMessage);

                return Result.Failure<LoginResponse>(authResult.ErrorMessage ?? "Login failed");
            }

            var user = authResult.User;
            var roles = user.UserRoles.Select(ur => ur.Role.Name).ToArray();

            var response = new LoginResponse
            {
                UserId = user.Id,
                Username = user.Username,
                Email = user.Email,
                AccessToken = authResult.AccessToken!,
                RefreshToken = authResult.RefreshToken!,
                AccessTokenExpiresAt = DateTimeOffset.UtcNow.AddHours(1), // Should come from config
                RefreshTokenExpiresAt = DateTimeOffset.UtcNow.AddDays(7), // Should come from config
                Roles = roles
            };

            _logger.LogInformation("Login successful for user: {Username} ({UserId})",
                user.Username, user.Id);

            return Result.Success<LoginResponse>(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for user: {UsernameOrEmail}", request.UsernameOrEmail);
            return Result.Failure<LoginResponse>("An error occurred during login");
        }
    }
}

