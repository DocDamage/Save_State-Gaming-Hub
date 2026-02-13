namespace SaveState.Application.Mugen.Services.WebPortal;

/// <summary>
/// Login request data.
/// </summary>
public class LoginRequest
{
    public string Username { get; set; } = default!;
    public string Password { get; set; } = default!;
    public bool RememberMe { get; set; }
    public string? TwoFactorCode { get; set; }
    public string? CaptchaToken { get; set; }
}

/// <summary>
/// Authentication token data.
/// </summary>
public class AuthToken
{
    public string Token { get; set; } = default!;
    public string RefreshToken { get; set; } = default!;
    public string TokenType { get; set; } = default!;
    public DateTime ExpiresAt { get; set; }
    public DateTime IssuedAt { get; set; }
    public string UserId { get; set; } = default!;
    public IReadOnlyList<string> Scopes { get; set; } = default!;
}

/// <summary>
/// Password reset request.
/// </summary>
public class PasswordResetRequest
{
    public string Email { get; set; } = default!;
    public string? ResetToken { get; set; }
    public string? NewPassword { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

/// <summary>
/// Registration request data.
/// </summary>
public class RegistrationRequest
{
    public string Username { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string? DisplayName { get; set; }
    public bool AcceptTerms { get; set; }
    public string? CaptchaToken { get; set; }
}
