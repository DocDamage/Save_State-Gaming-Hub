using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Constants;
using SaveState.Core.UserManagement.DTOs;
using SaveState.Core.UserManagement.Entities;
using SaveState.Core.UserManagement.Repositories;
using SaveState.Core.UserManagement.Services;

namespace SaveState.Infrastructure.UserManagement;

/// <summary>
/// Implementation of API key service for managing user API keys.
/// </summary>
public partial class ApiKeyService : IApiKeyService
{
    private readonly IApiKeyRepository _apiKeyRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<ApiKeyService> _logger;

    public ApiKeyService(
        IApiKeyRepository apiKeyRepository,
        IUserRepository userRepository,
        ILogger<ApiKeyService> logger)
    {
        _apiKeyRepository = apiKeyRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<Result<ApiKeyDto>> CreateApiKeyAsync(Guid userId, string name, string description, DateTimeOffset? expiresAt, CancellationToken ct = default)
    {
        try
        {
            LogCreatingApiKey(_logger, userId, name);

            var user = await _userRepository.GetByIdAsync(userId, ct);
            if (user == null)
            {
                LogUserNotFound(_logger, userId);
                return Result.Failure<ApiKeyDto>(ErrorMessages.UserNotFound);
            }

            var (apiKey, plainKey) = ApiKey.Create(user, name, description, expiresAt);

            await _apiKeyRepository.AddAsync(apiKey, ct);

            var dto = new ApiKeyDto(
                apiKey.Id,
                apiKey.Name,
                apiKey.Description,
                apiKey.KeyPrefix,
                apiKey.CreatedAt,
                apiKey.ExpiresAt,
                apiKey.LastUsedAt,
                apiKey.IsActive,
                apiKey.IsExpired());

            LogApiKeyCreated(_logger, userId, apiKey.Id, name);
            return Result.Success(dto);
        }
        catch (Exception ex)
        {
            LogCreateApiKeyFailed(_logger, userId, name, ex);
            return Result.Failure<ApiKeyDto>(ErrorMessages.CreateFailed);
        }
    }

    public async Task<Result<IReadOnlyList<ApiKeyDto>>> GetUserApiKeysAsync(Guid userId, CancellationToken ct = default)
    {
        try
        {
            LogGettingUserApiKeys(_logger, userId);

            var apiKeys = await _apiKeyRepository.GetByUserIdAsync(userId, ct);
            var dtos = apiKeys.Select(ak => new ApiKeyDto(
                ak.Id,
                ak.Name,
                ak.Description,
                ak.KeyPrefix,
                ak.CreatedAt,
                ak.ExpiresAt,
                ak.LastUsedAt,
                ak.IsActive,
                ak.IsExpired())).ToList();

            LogUserApiKeysRetrieved(_logger, userId, dtos.Count);
            return Result.Success<IReadOnlyList<ApiKeyDto>>(dtos);
        }
        catch (Exception ex)
        {
            LogGetUserApiKeysFailed(_logger, userId, ex);
            return Result.Failure<IReadOnlyList<ApiKeyDto>>(ErrorMessages.OperationFailed);
        }
    }

    public async Task<Result> RevokeApiKeyAsync(Guid userId, Guid apiKeyId, CancellationToken ct = default)
    {
        try
        {
            LogRevokingApiKey(_logger, userId, apiKeyId);

            var apiKey = await _apiKeyRepository.GetByIdAsync(apiKeyId, ct);
            if (apiKey == null)
            {
                LogApiKeyNotFound(_logger, apiKeyId);
                return Result.Failure(ErrorMessages.ApiKeyNotFound);
            }

            if (apiKey.UserId != userId)
            {
                LogApiKeyAccessDenied(_logger, userId, apiKeyId);
                return Result.Failure(ErrorMessages.AccessDenied);
            }

            apiKey.Revoke();
            await _apiKeyRepository.UpdateAsync(apiKey, ct);

            LogApiKeyRevoked(_logger, userId, apiKeyId, apiKey.Name);
            return Result.Success();
        }
        catch (Exception ex)
        {
            LogRevokeApiKeyFailed(_logger, userId, apiKeyId, ex);
            return Result.Failure(ErrorMessages.OperationFailed);
        }
    }

    public async Task<Result<Guid>> ValidateApiKeyAsync(string apiKey, CancellationToken ct = default)
    {
        try
        {
            LogValidatingApiKey(_logger);

            var user = await _apiKeyRepository.GetUserByApiKeyAsync(apiKey, ct);
            if (user == null)
            {
                LogApiKeyInvalid(_logger);
                return Result.Failure<Guid>(ErrorMessages.InvalidApiKey);
            }

            LogApiKeyValidated(_logger, user.Id);
            return Result.Success(user.Id);
        }
        catch (Exception ex)
        {
            LogValidateApiKeyFailed(_logger, ex);
            return Result.Failure<Guid>(ErrorMessages.OperationFailed);
        }
    }


    #region LoggerMessage Definitions
    [LoggerMessage(Level = LogLevel.Information, Message = "Creating API key '{Name}' for user {UserId}")]
    private static partial void LogCreatingApiKey(ILogger logger, Guid userId, string name);

    [LoggerMessage(Level = LogLevel.Information, Message = "API key {ApiKeyId} created for user {UserId} with name '{Name}'")]
    private static partial void LogApiKeyCreated(ILogger logger, Guid userId, Guid apiKeyId, string name);

    [LoggerMessage(Level = LogLevel.Warning, Message = "User {UserId} not found when creating API key")]
    private static partial void LogUserNotFound(ILogger logger, Guid userId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to create API key '{Name}' for user {UserId}")]
    private static partial void LogCreateApiKeyFailed(ILogger logger, Guid userId, string name, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Getting API keys for user {UserId}")]
    private static partial void LogGettingUserApiKeys(ILogger logger, Guid userId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Retrieved {Count} API keys for user {UserId}")]
    private static partial void LogUserApiKeysRetrieved(ILogger logger, Guid userId, int count);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to get API keys for user {UserId}")]
    private static partial void LogGetUserApiKeysFailed(ILogger logger, Guid userId, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Revoking API key {ApiKeyId} for user {UserId}")]
    private static partial void LogRevokingApiKey(ILogger logger, Guid userId, Guid apiKeyId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "API key {ApiKeyId} not found")]
    private static partial void LogApiKeyNotFound(ILogger logger, Guid apiKeyId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Access denied: user {UserId} attempted to revoke API key {ApiKeyId}")]
    private static partial void LogApiKeyAccessDenied(ILogger logger, Guid userId, Guid apiKeyId);

    [LoggerMessage(Level = LogLevel.Information, Message = "API key {ApiKeyId} revoked for user {UserId} ('{Name}')")]
    private static partial void LogApiKeyRevoked(ILogger logger, Guid userId, Guid apiKeyId, string name);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to revoke API key {ApiKeyId} for user {UserId}")]
    private static partial void LogRevokeApiKeyFailed(ILogger logger, Guid userId, Guid apiKeyId, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Validating API key")]
    private static partial void LogValidatingApiKey(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Invalid API key provided")]
    private static partial void LogApiKeyInvalid(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "API key validated for user {UserId}")]
    private static partial void LogApiKeyValidated(ILogger logger, Guid userId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to validate API key")]
    private static partial void LogValidateApiKeyFailed(ILogger logger, Exception ex);
    #endregion
}