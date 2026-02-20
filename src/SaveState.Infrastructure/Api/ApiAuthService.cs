using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SaveState.Core.Api.Entities;
using SaveState.Core.Api.Services;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Infrastructure.Persistence;

namespace SaveState.Infrastructure.Api;

/// <summary>
/// Implementation of API authentication service.
/// </summary>
public partial class ApiAuthService : IApiAuthService
{
    private readonly SaveStateDbContext _dbContext;
    private readonly ILogger<ApiAuthService> _logger;
    private readonly ITimeProvider _timeProvider;

    public ApiAuthService(SaveStateDbContext dbContext, ILogger<ApiAuthService> logger, ITimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task<Result<string>> GenerateApiKeyAsync(string appName, string[] scopes, CancellationToken ct = default)
    {
        try
        {
            LogGeneratingApiKey(_logger, appName);

            // Generate secure API key
            var keyString = GenerateSecureApiKey();

            // Set expiration to 1 year from now by default
            var expiresAt = _timeProvider.UtcNow.AddYears(1);

            // Create ApiKey entity
            var apiKey = ApiKey.Create(keyString, appName, scopes, expiresAt);

            // Save to database
            _dbContext.ExternalApiKeys.Add(apiKey);
            await _dbContext.SaveChangesAsync(ct);

            LogApiKeyGenerated(_logger, appName, apiKey.Id);
            return Result.Success(keyString);
        }
        catch (Exception ex)
        {
            LogGenerateApiKeyFailed(_logger, appName, ex);
            return Result.Failure<string>($"Failed to generate API key: {ex.Message}");
        }
    }

    public async Task<Result<ApiKeyInfo>> ValidateApiKeyAsync(string apiKey, CancellationToken ct = default)
    {
        try
        {
            // Query for the API key
            var key = await _dbContext.ExternalApiKeys
                .FirstOrDefaultAsync(k => k.Key == apiKey && k.IsActive, ct);

            if (key == null)
            {
                LogApiKeyNotFound(_logger);
                return Result.Failure<ApiKeyInfo>("Invalid or inactive API key");
            }

            // Check expiration
            if (key.IsExpired())
            {
                LogApiKeyExpired(_logger, key.AppName);
                return Result.Failure<ApiKeyInfo>("API key has expired");
            }

            // Update last used timestamp
            key.UpdateLastUsed();
            await _dbContext.SaveChangesAsync(ct);

            // Map to DTO
            var info = new ApiKeyInfo(
                Key: key.Key,
                AppName: key.AppName,
                Scopes: key.Scopes,
                CreatedAt: key.CreatedAt,
                ExpiresAt: key.ExpiresAt,
                LastUsedAt: key.LastUsedAt,
                IsActive: key.IsActive);

            LogApiKeyValidated(_logger, key.AppName);
            return Result.Success(info);
        }
        catch (Exception ex)
        {
            LogValidateApiKeyFailed(_logger, ex);
            return Result.Failure<ApiKeyInfo>("Invalid API key");
        }
    }

    public async Task<Result> RevokeApiKeyAsync(string apiKey, CancellationToken ct = default)
    {
        try
        {
            LogRevokingApiKey(_logger);

            // Find and revoke the API key
            var key = await _dbContext.ExternalApiKeys
                .FirstOrDefaultAsync(k => k.Key == apiKey, ct);

            if (key == null)
            {
                LogApiKeyNotFound(_logger);
                return Result.Failure("API key not found");
            }

            key.Revoke();
            await _dbContext.SaveChangesAsync(ct);

            LogApiKeyRevoked(_logger, key.AppName);
            return Result.Success();
        }
        catch (Exception ex)
        {
            LogRevokeApiKeyFailed(_logger, ex);
            return Result.Failure($"Failed to revoke API key: {ex.Message}");
        }
    }

    public async Task<Result<List<ApiKeyInfo>>> GetActiveApiKeysAsync(CancellationToken ct = default)
    {
        try
        {
            LogGettingActiveApiKeys(_logger);

            var keys = await _dbContext.ExternalApiKeys
                .Where(k => k.IsActive && (k.ExpiresAt == null || k.ExpiresAt > _timeProvider.UtcNow))
                .Select(k => new ApiKeyInfo(
                    k.Key,
                    k.AppName,
                    k.Scopes,
                    k.CreatedAt,
                    k.ExpiresAt,
                    k.LastUsedAt,
                    k.IsActive))
                .ToListAsync(ct);

            LogActiveApiKeysRetrieved(_logger, keys.Count);
            return Result.Success(keys);
        }
        catch (Exception ex)
        {
            LogGetActiveApiKeysFailed(_logger, ex);
            return Result.Failure<List<ApiKeyInfo>>($"Failed to get API keys: {ex.Message}");
        }
    }

    #region LoggerMessage Definitions
    [LoggerMessage(Level = LogLevel.Information, Message = "Generating API key for application: {AppName}")]
    private static partial void LogGeneratingApiKey(ILogger logger, string appName);

    [LoggerMessage(Level = LogLevel.Information, Message = "API key generated for application {AppName} with ID {ApiKeyId}")]
    private static partial void LogApiKeyGenerated(ILogger logger, string appName, Guid apiKeyId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to generate API key for application {AppName}")]
    private static partial void LogGenerateApiKeyFailed(ILogger logger, string appName, Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "API key not found or inactive")]
    private static partial void LogApiKeyNotFound(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "API key for application {AppName} has expired")]
    private static partial void LogApiKeyExpired(ILogger logger, string appName);

    [LoggerMessage(Level = LogLevel.Information, Message = "API key validated for application {AppName}")]
    private static partial void LogApiKeyValidated(ILogger logger, string appName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to validate API key")]
    private static partial void LogValidateApiKeyFailed(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Revoking API key")]
    private static partial void LogRevokingApiKey(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "API key revoked for application {AppName}")]
    private static partial void LogApiKeyRevoked(ILogger logger, string appName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to revoke API key")]
    private static partial void LogRevokeApiKeyFailed(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Getting active API keys")]
    private static partial void LogGettingActiveApiKeys(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Retrieved {Count} active API keys")]
    private static partial void LogActiveApiKeysRetrieved(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to get active API keys")]
    private static partial void LogGetActiveApiKeysFailed(ILogger logger, Exception ex);
    #endregion

    private static string GenerateSecureApiKey()
    {
        // Generate a 32-byte random key
        var bytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }

        // Convert to base64 with URL-safe characters
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}

