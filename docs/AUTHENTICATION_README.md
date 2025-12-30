# Authentication & Authorization System

This document describes the comprehensive authentication and authorization system implemented in SaveStateReborn.

## Overview

SaveStateReborn implements a complete enterprise-grade authentication and authorization system with:

- JWT-based authentication with refresh tokens
- Role-Based Access Control (RBAC)
- API key management
- Password security with PBKDF2 hashing
- Rate limiting and security monitoring

## JWT Authentication

### Token Structure

```json
{
  "sub": "user-guid",
  "name": "username",
  "email": "user@example.com",
  "roles": ["User", "Admin"],
  "permissions": ["games:read", "games:write"],
  "iat": 1640995200,
  "exp": 1641081600,
  "iss": "savestate-api",
  "aud": "savestate-client"
}
```

### Refresh Token Flow

1. User logs in with credentials
2. Server returns access token + refresh token
3. Client uses access token for API calls
4. When access token expires, client uses refresh token to get new access token
5. Server validates refresh token and issues new token pair

## API Key Management

### API Key Entity

```json
{
  "id": "guid",
  "name": "My Application",
  "description": "API key for my app",
  "key": "sk_live_**************************", // Replace with your actual Stripe API key
  "createdAt": "2025-12-29T14:30:00Z",
  "expiresAt": "2026-12-29T00:00:00Z"
}
```

### Using API Keys

```bash
curl -H "Authorization: Bearer sk_live_**************************" \
     https://api.savestate.com/games
```

## Configuration

### JWT Settings

```json
{
  "Jwt": {
    "Issuer": "savestate-api",
    "Audience": "savestate-client",
    "SecretKey": "your-256-bit-secret",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
  }
}
```

### Authentication Options

```json
{
  "Authentication": {
    "RequireConfirmedEmail": true,
    "RequireConfirmedPhone": false,
    "PasswordRequiredLength": 8,
    "PasswordRequireDigit": true,
    "PasswordRequireLowercase": true,
    "PasswordRequireUppercase": true,
    "PasswordRequireNonAlphanumeric": true,
    "MaxFailedAccessAttempts": 5,
    "LockoutTimeSpan": "00:15:00"
  }
}
```

## Role-Based Authorization

### Built-in Roles

- **Guest**: Basic read access
- **User**: Standard user permissions
- **Moderator**: Content moderation permissions
- **Admin**: Full administrative access

### Permission System

Permissions are structured as `resource:action` pairs:

- `games:read` - View games
- `games:write` - Create/modify games
- `users:read` - View user profiles
- `users:write` - Modify users
- `admin:*` - All administrative permissions

### Custom Roles

```csharp
// Define custom role with specific permissions
var customRole = new Role
{
    Name = "ContentCreator",
    Permissions = new[]
    {
        Permission.Create("games:write"),
        Permission.Create("media:upload"),
        Permission.Create("achievements:create")
    }
};
```

## Security Features

### Password Hashing

- PBKDF2 with 100,000 iterations
- 256-bit salt per password
- Secure random salt generation

### Rate Limiting

- Sliding window algorithm
- Configurable limits per operation
- Automatic cleanup of expired entries

### Input Validation

- XSS prevention
- SQL injection protection
- Command injection blocking
- Path traversal validation

## Usage Examples

### User Registration

```csharp
var registerCommand = new RegisterUserCommand
{
    Username = "johndoe",
    Email = "john@example.com",
    Password = "SecurePass123!"
};

await mediator.Send(registerCommand);
```

### User Login

```csharp
var loginCommand = new LoginCommand
{
    Username = "johndoe",
    Password = "SecurePass123!"
};

var result = await mediator.Send(loginCommand);
// Returns: { AccessToken, RefreshToken, ExpiresAt }
```

### API Access

```csharp
[Authorize]
[HttpGet("games")]
public async Task<IActionResult> GetGames()
{
    // User is authenticated and authorized
    var games = await gameService.GetUserGames(User.GetUserId());
    return Ok(games);
}
```

### Role-Based Access

```csharp
[Authorize(Roles = "Admin,Moderator")]
[HttpDelete("games/{id}")]
public async Task<IActionResult> DeleteGame(Guid id)
{
    await gameService.DeleteGame(id);
    return NoContent();
}
```

## Security Monitoring

### Failed Login Tracking

- Automatic account lockout after failed attempts
- Configurable lockout duration
- Security event logging

### API Key Security

- Automatic key rotation recommendations
- Expiration enforcement
- Usage monitoring and alerting

## Best Practices

### Password Security

- Use strong passwords (12+ characters recommended)
- Enable two-factor authentication when available
- Change passwords regularly

### API Key Management

- Rotate API keys regularly
- Use separate keys for different environments
- Monitor key usage patterns

### Token Security

- Store tokens securely (HttpOnly cookies for web clients)
- Implement token refresh logic
- Validate token expiration on client side

## Troubleshooting

### Common Issues

1. **Token Expired**: Use refresh token to get new access token
2. **Invalid Credentials**: Check username/password
3. **Insufficient Permissions**: Verify user roles and permissions
4. **Rate Limited**: Wait for rate limit reset or upgrade plan

### Debug Logging

Enable debug logging to troubleshoot authentication issues:

```json
{
  "Logging": {
    "LogLevel": {
      "SaveState.UserManagement": "Debug"
    }
  }
}
```

## API Reference

### Authentication Endpoints

- `POST /api/auth/login` - User login
- `POST /api/auth/register` - User registration
- `POST /api/auth/refresh` - Token refresh
- `POST /api/auth/logout` - User logout

### User Management Endpoints

- `GET /api/users/profile` - Get current user profile
- `PUT /api/users/profile` - Update user profile
- `GET /api/users/{id}` - Get user by ID (admin only)

### API Key Endpoints

- `GET /api/keys` - List user's API keys
- `POST /api/keys` - Create new API key
- `DELETE /api/keys/{id}` - Delete API key

---

**Note**: This authentication system provides enterprise-grade security suitable for production applications. All sensitive operations are logged and monitored for security compliance.