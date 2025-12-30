using MediatR;
using SaveState.Core.Common;

namespace SaveState.Application.UserManagement.Commands;

/// <summary>
/// Command to refresh an access token using a refresh token.
/// </summary>
public class RefreshTokenCommand : IRequest<Result<RefreshTokenResponse>>
{
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>
/// Response containing the new access token.
/// </summary>
public class RefreshTokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTimeOffset AccessTokenExpiresAt { get; set; }
}
