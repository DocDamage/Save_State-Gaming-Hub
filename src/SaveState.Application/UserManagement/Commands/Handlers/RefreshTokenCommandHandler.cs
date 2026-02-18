using MediatR;
using Microsoft.Extensions.Logging;
using SaveState.Application.UserManagement.Commands;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.UserManagement.Services;

namespace SaveState.Application.UserManagement.Commands.Handlers;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<RefreshTokenResponse>>
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;
    private readonly ITimeProvider _timeProvider;

    public RefreshTokenCommandHandler(
        IAuthenticationService authenticationService,
        ILogger<RefreshTokenCommandHandler> logger,
        ITimeProvider timeProvider)
    {
        _authenticationService = authenticationService;
        _logger = logger;
        _timeProvider = timeProvider;
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
                return Result.Failure<RefreshTokenResponse>(refreshResult.ErrorMessage ?? "Token refresh failed");
            }

            var response = new RefreshTokenResponse
            {
                AccessToken = refreshResult.AccessToken!,
                AccessTokenExpiresAt = _timeProvider.UtcNow.AddHours(1) // Should come from config
            };

            _logger.LogInformation("Token refresh successful");

            return Result.Success<RefreshTokenResponse>(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return Result.Failure<RefreshTokenResponse>("An error occurred during token refresh");
        }
    }
}

