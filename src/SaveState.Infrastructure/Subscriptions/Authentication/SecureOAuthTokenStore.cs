// Copyright (c) 2026 SaveStateReborn. All rights reserved.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SaveState.Core.Subscriptions;
using SaveState.Core.Subscriptions.Authentication;

namespace SaveState.Infrastructure.Subscriptions.Authentication;

/// <summary>
/// Securely stores OAuth tokens using Windows Data Protection API.
/// </summary>
public sealed class SecureOAuthTokenStore : IOAuthTokenStore
{
    private readonly ILogger<SecureOAuthTokenStore> _logger;
    private readonly string _storagePath;

    public SecureOAuthTokenStore(ILogger<SecureOAuthTokenStore> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _storagePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SaveStateReborn",
            "OAuthTokens");

        Directory.CreateDirectory(_storagePath);
    }

    /// <inheritdoc />
    public async Task SaveTokensAsync(SubscriptionServiceType serviceType, OAuthTokens tokens, CancellationToken ct = default)
    {
        try
        {
            var filePath = GetTokenFilePath(serviceType);
            var json = JsonSerializer.Serialize(tokens);
            var bytes = Encoding.UTF8.GetBytes(json);

            // Encrypt using Windows DPAPI
            var encrypted = Protect(bytes);

            await File.WriteAllBytesAsync(filePath, encrypted, ct);

            _logger.LogInformation("OAuth tokens saved for {ServiceType}", serviceType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save OAuth tokens for {ServiceType}", serviceType);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<OAuthTokens?> GetTokensAsync(SubscriptionServiceType serviceType, CancellationToken ct = default)
    {
        try
        {
            var filePath = GetTokenFilePath(serviceType);

            if (!File.Exists(filePath))
            {
                _logger.LogDebug("No OAuth tokens found for {ServiceType}", serviceType);
                return null;
            }

            var encrypted = await File.ReadAllBytesAsync(filePath, ct);
            var bytes = Unprotect(encrypted);
            var json = Encoding.UTF8.GetString(bytes);

            var tokens = JsonSerializer.Deserialize<OAuthTokens>(json);

            _logger.LogDebug("OAuth tokens retrieved for {ServiceType}", serviceType);
            return tokens;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve OAuth tokens for {ServiceType}", serviceType);
            return null;
        }
    }

    /// <inheritdoc />
    public Task DeleteTokensAsync(SubscriptionServiceType serviceType, CancellationToken ct = default)
    {
        try
        {
            var filePath = GetTokenFilePath(serviceType);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogInformation("OAuth tokens deleted for {ServiceType}", serviceType);
            }

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete OAuth tokens for {ServiceType}", serviceType);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> HasValidTokensAsync(SubscriptionServiceType serviceType, CancellationToken ct = default)
    {
        var tokens = await GetTokensAsync(serviceType, ct);
        return tokens != null && !tokens.IsExpired;
    }

    private string GetTokenFilePath(SubscriptionServiceType serviceType)
    {
        var fileName = $"{serviceType}_tokens.dat";
        return Path.Combine(_storagePath, fileName);
    }

    private static byte[] Protect(byte[] data)
    {
        if (OperatingSystem.IsWindows())
        {
            return ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
        }

        // Fallback for non-Windows: simple XOR (not secure, but better than plaintext)
        return Obfuscate(data);
    }

    private static byte[] Unprotect(byte[] data)
    {
        if (OperatingSystem.IsWindows())
        {
            return ProtectedData.Unprotect(data, null, DataProtectionScope.CurrentUser);
        }

        // Fallback for non-Windows
        return Obfuscate(data);
    }

    private static byte[] Obfuscate(byte[] data)
    {
        // Simple XOR obfuscation for non-Windows platforms
        var key = new byte[] { 0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC, 0xDE, 0xF0 };
        var result = new byte[data.Length];

        for (int i = 0; i < data.Length; i++)
        {
            result[i] = (byte)(data[i] ^ key[i % key.Length]);
        }

        return result;
    }
}
