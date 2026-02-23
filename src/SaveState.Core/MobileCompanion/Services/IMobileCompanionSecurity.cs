using SaveState.Core.Common;

namespace SaveState.Core.MobileCompanion.Services;

/// <summary>
/// Interface for Mobile Companion security operations including pairing codes,
/// token generation, and encryption.
/// </summary>
public interface IMobileCompanionSecurity
{
    /// <summary>
    /// Generates a new 6-digit pairing code.
    /// </summary>
    /// <returns>A 6-digit pairing code.</returns>
    string GeneratePairingCode();

    /// <summary>
    /// Generates a cryptographically secure token for session authentication.
    /// </summary>
    /// <returns>A secure token string.</returns>
    string GenerateToken();

    /// <summary>
    /// Validates a pairing code format and checks if it's not expired.
    /// </summary>
    /// <param name="code">The pairing code to validate.</param>
    /// <returns>True if the code is valid; otherwise, false.</returns>
    bool ValidatePairingCode(string code);

    /// <summary>
    /// Validates an authentication token.
    /// </summary>
    /// <param name="token">The token to validate.</param>
    /// <returns>True if the token is valid; otherwise, false.</returns>
    bool ValidateToken(string token);

    /// <summary>
    /// Encrypts data using the provided key.
    /// </summary>
    /// <param name="data">The data to encrypt.</param>
    /// <param name="key">The encryption key.</param>
    /// <returns>The encrypted data.</returns>
    byte[] EncryptData(byte[] data, byte[] key);

    /// <summary>
    /// Decrypts data using the provided key.
    /// </summary>
    /// <param name="data">The data to decrypt.</param>
    /// <param name="key">The decryption key.</param>
    /// <returns>The decrypted data.</returns>
    byte[] DecryptData(byte[] data, byte[] key);

    /// <summary>
    /// Generates an ECDH key pair for end-to-end encryption.
    /// </summary>
    /// <returns>A tuple containing the public and private keys.</returns>
    (byte[] publicKey, byte[] privateKey) GenerateKeyPair();

    /// <summary>
    /// Derives a shared secret from the private key and peer's public key.
    /// </summary>
    /// <param name="privateKey">Our private key.</param>
    /// <param name="peerPublicKey">The peer's public key.</param>
    /// <returns>The shared secret.</returns>
    byte[] DeriveSharedSecret(byte[] privateKey, byte[] peerPublicKey);

    /// <summary>
    /// Computes a hash of the input data.
    /// </summary>
    /// <param name="data">The data to hash.</param>
    /// <returns>The computed hash.</returns>
    byte[] ComputeHash(byte[] data);

    /// <summary>
    /// Generates a secure random byte array.
    /// </summary>
    /// <param name="length">The length of the byte array.</param>
    /// <returns>A random byte array.</returns>
    byte[] GenerateRandomBytes(int length);
}

/// <summary>
/// Options for configuring mobile companion security.
/// </summary>
public class MobileCompanionSecurityOptions
{
    /// <summary>
    /// The duration in minutes that a pairing code remains valid.
    /// </summary>
    public int PairingCodeExpirationMinutes { get; set; } = 10;

    /// <summary>
    /// The duration in hours that an authentication token remains valid.
    /// </summary>
    public int TokenExpirationHours { get; set; } = 168; // 7 days

    /// <summary>
    /// Whether to require HTTPS for connections.
    /// </summary>
    public bool RequireHttps { get; set; } = true;

    /// <summary>
    /// The certificate pinning hash for HTTPS connections.
    /// </summary>
    public string? CertificatePinningHash { get; set; }
}

/// <summary>
/// Represents a security context for a paired device.
/// </summary>
public class DeviceSecurityContext
{
    public Guid DeviceId { get; set; }
    public byte[]? SharedSecret { get; set; }
    public byte[]? PublicKey { get; set; }
    public DateTime EstablishedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public List<string> Permissions { get; set; } = new();
}

/// <summary>
/// Permissions that can be granted to mobile companion devices.
/// </summary>
public static class MobileCompanionPermissions
{
    public const string ViewLibrary = "library:view";
    public const string LaunchGames = "games:launch";
    public const string ControlMedia = "media:control";
    public const string ManageSaveStates = "savestates:manage";
    public const string ViewSystemStatus = "system:view";
    public const string SendInput = "input:send";
    public const string ReceiveNotifications = "notifications:receive";
    public const string TakeScreenshots = "screenshots:take";
    public const string StartRecording = "recording:start";
    public const string Admin = "admin";
}
