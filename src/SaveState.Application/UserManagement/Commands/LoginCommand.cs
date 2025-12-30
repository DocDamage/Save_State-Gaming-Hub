using MediatR;
using SaveState.Core.Common;

namespace SaveState.Application.UserManagement.Commands;

/// <summary>
/// Command to authenticate a user and generate JWT tokens.
/// </summary>
public class LoginCommand : IRequest<Result<LoginResponse>>
{
    public string UsernameOrEmail { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Response containing authentication tokens.
/// </summary>
public class LoginResponse
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTimeOffset AccessTokenExpiresAt { get; set; }
    public DateTimeOffset RefreshTokenExpiresAt { get; set; }
    public IEnumerable<string> Roles { get; set; } = Array.Empty<string>();
}
