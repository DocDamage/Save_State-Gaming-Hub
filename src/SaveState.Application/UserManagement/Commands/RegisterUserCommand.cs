using MediatR;
using SaveState.Core.Common;

namespace SaveState.Application.UserManagement.Commands;

/// <summary>
/// Command to register a new user.
/// </summary>
public class RegisterUserCommand : IRequest<Result<RegisterUserResponse>>
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}

/// <summary>
/// Response containing the newly registered user information.
/// </summary>
public class RegisterUserResponse
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public bool RequiresEmailVerification { get; set; }
}
