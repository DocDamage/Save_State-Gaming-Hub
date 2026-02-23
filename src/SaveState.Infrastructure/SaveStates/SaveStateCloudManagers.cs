using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.SaveStates;
using SaveState.Core.SaveStates.Services;
using SaveState.Core.SaveStates.Services.DTOs;
using SaveState.Core.Sync;
using SaveStateEntity = SaveState.Core.SaveStates.Entities.SaveState;

namespace SaveState.Infrastructure.SaveStates;

internal sealed class SaveStateCloudProviderResolver
{
    private readonly IUserPreferencesService _preferencesService;
    private readonly IReadOnlyList<ICloudStorageProvider> _cloudProviders;

    public SaveStateCloudProviderResolver(
        IUserPreferencesService preferencesService,
        IReadOnlyList<ICloudStorageProvider> cloudProviders)
    {
        _preferencesService = preferencesService;
        _cloudProviders = cloudProviders;
    }

    public async Task<Result<ICloudStorageProvider>> ResolveProviderAsync(CancellationToken ct)
    {
        if (_cloudProviders.Count == 0)
        {
            return Result.Failure<ICloudStorageProvider>(
                "No cloud storage provider is registered.",
                ErrorType.NotFound);
        }

        var preferredProviderName = await _preferencesService.GetPreferredCloudProviderAsync(ct).ConfigureAwait(false);
        var normalizedPreferredProviderName = SaveStateCloudHelpers.NormalizeProviderName(preferredProviderName);
        var provider = !string.IsNullOrWhiteSpace(normalizedPreferredProviderName)
            ? _cloudProviders.FirstOrDefault(p =>
                string.Equals(
                    SaveStateCloudHelpers.NormalizeProviderName(p.ProviderName),
                    normalizedPreferredProviderName,
                    StringComparison.Ordinal))
            : null;

        provider ??= _cloudProviders.FirstOrDefault(p => !SaveStateCloudHelpers.IsLocalProvider(p.ProviderName))
                    ?? _cloudProviders.FirstOrDefault();

        if (provider is null)
        {
            return Result.Failure<ICloudStorageProvider>(
                "No compatible cloud provider is available.",
                ErrorType.NotFound);
        }

        if (!provider.IsAuthenticated)
        {
            var authenticated = await provider.AuthenticateAsync(ct).ConfigureAwait(false);
            if (!authenticated)
            {
                return Result.Failure<ICloudStorageProvider>(
                    $"Authentication failed for cloud provider '{provider.ProviderName}'.",
                    ErrorType.Unauthorized);
            }
        }

        return Result.Success(provider);
    }
}

internal sealed class SaveStateCloudPayloadManager
{
    private readonly ICloudSaveEncryptionService _encryptionService;

    public SaveStateCloudPayloadManager(ICloudSaveEncryptionService encryptionService)
    {
        _encryptionService = encryptionService;
    }

    public Result<string?> ResolveEncryptionKeyFingerprint(string? encryptionKey)
    {
        if (string.IsNullOrWhiteSpace(encryptionKey))
        {
            return Result.Success<string?>(null);
        }

        var fingerprintResult = _encryptionService.GetKeyFingerprint(encryptionKey);
        if (fingerprintResult.IsFailure)
        {
            return Result.Failure<string?>(
                fingerprintResult.Error ?? "Failed to compute encryption key fingerprint.",
                fingerprintResult.ErrorType);
        }

        return Result.Success<string?>(fingerprintResult.Value);
    }

    public async Task<Result<string>> ResolveUploadPayloadPathAsync(
        string localSavePath,
        string? encryptionKey,
        ICollection<string> tempFiles,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(encryptionKey))
        {
            return Result.Success(localSavePath);
        }

        var encryptionResult = await _encryptionService.EncryptFileAsync(
            localSavePath,
            encryptionKey,
            ct).ConfigureAwait(false);

        if (encryptionResult.IsFailure || string.IsNullOrWhiteSpace(encryptionResult.Value))
        {
            return Result.Failure<string>(
                encryptionResult.Error ?? "Failed to encrypt save state.",
                encryptionResult.ErrorType);
        }

        tempFiles.Add(encryptionResult.Value);
        return Result.Success(encryptionResult.Value);
    }

    public async Task<Result<string>> DownloadCloudPayloadAsync(
        ICloudStorageProvider provider,
        SaveStateCloudVersion cloudVersion,
        SaveStateCloudMetadata metadata,
        string downloadPath,
        ICollection<string> tempFiles,
        CancellationToken ct)
    {
        var downloadResult = await provider.DownloadFileAsync(
            cloudVersion.StoragePath,
            downloadPath,
            ct).ConfigureAwait(false);

        if (downloadResult.IsFailure || !downloadResult.Value || !File.Exists(downloadPath))
        {
            return Result.Failure<string>(
                downloadResult.Error ?? $"Failed to download save state from {provider.ProviderName}.",
                downloadResult.IsFailure ? downloadResult.ErrorType : ErrorType.External);
        }

        if (!cloudVersion.IsEncrypted)
        {
            return Result.Success(downloadPath);
        }

        if (string.IsNullOrWhiteSpace(metadata.EncryptionKey))
        {
            return Result.Failure<string>(
                "Cloud save state is encrypted but no encryption key was provided.",
                ErrorType.Validation);
        }

        if (!string.IsNullOrWhiteSpace(cloudVersion.EncryptionKeyFingerprint))
        {
            var fingerprintResult = _encryptionService.GetKeyFingerprint(metadata.EncryptionKey);
            if (fingerprintResult.IsFailure || string.IsNullOrWhiteSpace(fingerprintResult.Value))
            {
                return Result.Failure<string>(
                    fingerprintResult.Error ?? "Failed to validate encryption key fingerprint.",
                    fingerprintResult.ErrorType);
            }

            if (!string.Equals(
                    fingerprintResult.Value,
                    cloudVersion.EncryptionKeyFingerprint,
                    StringComparison.OrdinalIgnoreCase))
            {
                return Result.Failure<string>(
                    "Provided encryption key does not match cloud payload fingerprint.",
                    ErrorType.Validation);
            }
        }

        var decryptResult = await _encryptionService.DecryptFileAsync(
            downloadPath,
            metadata.EncryptionKey,
            ct).ConfigureAwait(false);
        if (decryptResult.IsFailure || string.IsNullOrWhiteSpace(decryptResult.Value))
        {
            return Result.Failure<string>(
                decryptResult.Error ?? "Failed to decrypt cloud save state payload.",
                decryptResult.ErrorType);
        }

        tempFiles.Add(decryptResult.Value);
        return Result.Success(decryptResult.Value);
    }
}

internal static class SaveStateCloudHelpers
{
    private const string CloudRootPath = "savestates";

    public static string BuildCloudSavePath(SaveStateEntity saveState, bool isEncrypted)
    {
        var extension = Path.GetExtension(saveState.FilePath);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".state";
        }

        var fileName = $"{saveState.Id}{extension}";
        if (isEncrypted)
        {
            fileName += ".enc";
        }

        return $"{CloudRootPath}/{saveState.GameId}/{fileName}";
    }

    public static string ResolveRestoreExtension(string storagePath, bool isEncrypted)
    {
        var fileName = Path.GetFileName(storagePath.Replace('\\', '/'));
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return ".state";
        }

        if (isEncrypted && fileName.EndsWith(".enc", StringComparison.OrdinalIgnoreCase))
        {
            fileName = Path.GetFileNameWithoutExtension(fileName);
        }

        var extension = Path.GetExtension(fileName);
        return string.IsNullOrWhiteSpace(extension) ? ".state" : extension;
    }

    public static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Best effort cleanup.
        }
    }

    public static string NormalizeProviderName(string? providerName)
    {
        if (string.IsNullOrWhiteSpace(providerName))
        {
            return string.Empty;
        }

        var normalizedChars = providerName
            .Where(char.IsLetterOrDigit)
            .Select(char.ToLowerInvariant)
            .ToArray();
        return new string(normalizedChars);
    }

    public static bool IsLocalProvider(string? providerName)
    {
        var normalizedName = NormalizeProviderName(providerName);
        return normalizedName is "local" or "localstorage" or "filesystem";
    }
}
