using System.IdentityModel.Tokens.Jwt;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SaveState.Core.UserManagement.Configuration;
using SaveState.Core.UserManagement.Entities;
using SaveState.Infrastructure.UserManagement;
using SaveState.Tests.Infrastructure;

namespace SaveState.Infrastructure.Tests.UserManagement;

public class JwtTokenServiceTests
{
    [Fact]
    public async Task GenerateAccessTokenAsync_ShouldUseTimeProviderForExpiration()
    {
        var now = new DateTime(2026, 2, 1, 12, 0, 0, DateTimeKind.Utc);
        var timeProvider = new TestTimeProvider(now);
        var sut = CreateSut(timeProvider, accessTokenMinutes: 30, refreshTokenDays: 7);
        var user = CreateUser();

        var token = await sut.GenerateAccessTokenAsync(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        jwt.ValidTo.Should().BeCloseTo(now.AddMinutes(30), TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task GenerateRefreshTokenAsync_ShouldUseTimeProviderForExpiration()
    {
        var now = new DateTime(2026, 2, 1, 12, 0, 0, DateTimeKind.Utc);
        var timeProvider = new TestTimeProvider(now);
        var sut = CreateSut(timeProvider, accessTokenMinutes: 30, refreshTokenDays: 10);
        var user = CreateUser();

        var token = await sut.GenerateRefreshTokenAsync(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        jwt.ValidTo.Should().BeCloseTo(now.AddDays(10), TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task ValidateTokenAsync_WithGeneratedAccessToken_ShouldSucceed()
    {
        var now = new DateTime(2026, 2, 1, 12, 0, 0, DateTimeKind.Utc);
        var timeProvider = new TestTimeProvider(now);
        var sut = CreateSut(timeProvider, accessTokenMinutes: 30, refreshTokenDays: 7);
        var user = CreateUser();
        var token = await sut.GenerateAccessTokenAsync(user);

        var result = await sut.ValidateTokenAsync(token);

        result.IsSuccess.Should().BeTrue();
        result.Value.Identity.Should().NotBeNull();
        result.Value.Identity!.IsAuthenticated.Should().BeTrue();
    }

    private static JwtTokenService CreateSut(TestTimeProvider timeProvider, int accessTokenMinutes, int refreshTokenDays)
    {
        var options = Options.Create(new JwtOptions
        {
            Issuer = "SaveStateReborn",
            Audience = "SaveStateReborn.Client",
            SecretKey = "this-is-a-test-secret-key-with-32+chars",
            AccessTokenExpirationMinutes = accessTokenMinutes,
            RefreshTokenExpirationDays = refreshTokenDays
        });

        return new JwtTokenService(options, NullLogger<JwtTokenService>.Instance, timeProvider);
    }

    private static User CreateUser()
    {
        return User.Create(
            username: "test-user",
            email: "test@example.com",
            passwordHash: "hash",
            passwordSalt: "salt");
    }
}
