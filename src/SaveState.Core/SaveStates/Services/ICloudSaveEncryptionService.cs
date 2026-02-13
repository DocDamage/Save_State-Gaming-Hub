using SaveState.Core.Common;

namespace SaveState.Core.SaveStates.Services;

/// <summary>
/// Provides client-side encryption for cloud save state payloads.
/// </summary>
public interface ICloudSaveEncryptionService
{
    /// <summary>
    /// Encrypts the specified file and returns a temporary encrypted file path.
    /// The caller is responsible for deleting the temporary file.
    /// </summary>
    Task<Result<string>> EncryptFileAsync(
        string sourceFilePath,
        string encryptionKey,
        CancellationToken ct = default);

    /// <summary>
    /// Decrypts the specified encrypted file and returns a temporary decrypted file path.
    /// The caller is responsible for deleting the temporary file.
    /// </summary>
    Task<Result<string>> DecryptFileAsync(
        string encryptedFilePath,
        string encryptionKey,
        CancellationToken ct = default);

    /// <summary>
    /// Computes a non-reversible fingerprint for the key.
    /// </summary>
    string GetKeyFingerprint(string encryptionKey);
}
