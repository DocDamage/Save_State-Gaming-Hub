using MediatR;
using Microsoft.Extensions.Logging;
using SaveState.Application.UserManagement.Commands;
using SaveState.Core.Common;
using SaveState.Core.UserManagement.Services;

namespace SaveState.Application.UserManagement.Commands.Handlers;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<RefreshTokenResponse>>
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    public RefreshTokenCommandHandler(
        IAuthenticationService authenticationService,
        ILogger<RefreshTokenCommandHandler> logger)
    {
        _authenticationService = authenticationService;
        _logger = logger;
    }

    public async Task<Result<RefreshTokenResponse>> Handle(RefreshTokenCommand request, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Attempting token refresh");

            var refreshResult = await _authenticationService.RefreshTokenAsync(request.RefreshToken, ct);

            if (!refreshResult.IsSuccessful)
            {
                _logger.LogWarning("Token refresh failed. Reason: {Error}", refreshResult.ErrorMessage);
                return Result<RefreshTokenResponse>.Failure(refreshResult.ErrorMessage ?? "Token refresh failed");
            }

            var response = new RefreshTokenResponse
            {
                AccessToken = refreshResult.AccessToken!,
                AccessTokenExpiresAt = DateTimeOffset.UtcNow.AddHours(1) // Should come from config
            };

            _logger.LogInformation("Token refresh successful");

            return Result<RefreshTokenResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return Result<RefreshTokenResponse>.Failure("An error occurred during token refresh");
        }
    }
}
