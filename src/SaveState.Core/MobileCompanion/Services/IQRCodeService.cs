using SaveState.Core.Common;

namespace SaveState.Core.MobileCompanion.Services;

/// <summary>
/// Interface for generating and reading QR codes for mobile companion pairing.
/// </summary>
public interface IQRCodeService
{
    /// <summary>
    /// Generates a pairing QR code as a byte array (PNG image).
    /// </summary>
    /// <param name="info">The pairing information to encode.</param>
    /// <param name="size">The size of the QR code image in pixels.</param>
    /// <returns>A byte array containing the PNG image.</returns>
    Task<Result<byte[]>> GeneratePairingQRCodeAsync(PairingInfo info, int size = 256);

    /// <summary>
    /// Reads pairing information from a QR code image.
    /// </summary>
    /// <param name="imageStream">The stream containing the QR code image.</param>
    /// <returns>The decoded pairing information, or null if not found.</returns>
    Task<Result<PairingInfo?>> ReadQRCodeAsync(Stream imageStream);

    /// <summary>
    /// Generates a pairing URL that can be encoded in a QR code.
    /// </summary>
    /// <param name="info">The pairing information.</param>
    /// <returns>A URL string containing the pairing data.</returns>
    string GeneratePairingUrl(PairingInfo info);

    /// <summary>
    /// Parses pairing information from a URL.
    /// </summary>
    /// <param name="url">The URL to parse.</param>
    /// <returns>The parsed pairing information.</returns>
    Result<PairingInfo> ParsePairingUrl(string url);

    /// <summary>
    /// Generates a pairing code for manual entry as a fallback.
    /// </summary>
    /// <returns>A 6-digit pairing code.</returns>
    string GenerateManualPairingCode();

    /// <summary>
    /// Validates a manual pairing code.
    /// </summary>
    /// <param name="code">The code to validate.</param>
    /// <returns>True if valid; otherwise, false.</returns>
    bool ValidateManualPairingCode(string code);
}

/// <summary>
/// Information encoded in a pairing QR code.
/// </summary>
public class PairingInfo
{
    /// <summary>
    /// Unique identifier for the hub/desktop instance.
    /// </summary>
    public string HubId { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the hub.
    /// </summary>
    public string HubName { get; set; } = string.Empty;

    /// <summary>
    /// IP address for connecting to the hub.
    /// </summary>
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// Port number for the SignalR hub.
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// Authentication token for the pairing session.
    /// </summary>
    public string? Token { get; set; }

    /// <summary>
    /// Protocol to use (http or https).
    /// </summary>
    public string Protocol { get; set; } = "https";

    /// <summary>
    /// The SignalR hub path.
    /// </summary>
    public string HubPath { get; set; } = "/mobile-hub";

    /// <summary>
    /// Version of the pairing protocol.
    /// </summary>
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// Expiration timestamp of the pairing code.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Gets the full SignalR hub URL.
    /// </summary>
    public string GetHubUrl() => $"{Protocol}://{IpAddress}:{Port}{HubPath}";
}

/// <summary>
/// Configuration options for QR code generation.
/// </summary>
public class QRCodeOptions
{
    /// <summary>
    /// Default size of the QR code in pixels.
    /// </summary>
    public int DefaultSize { get; set; } = 256;

    /// <summary>
    /// Error correction level (L, M, Q, H).
    /// </summary>
    public string ErrorCorrectionLevel { get; set; } = "M";

    /// <summary>
    /// Foreground color (hex).
    /// </summary>
    public string ForegroundColor { get; set; } = "#000000";

    /// <summary>
    /// Background color (hex).
    /// </summary>
    public string BackgroundColor { get; set; } = "#FFFFFF";

    /// <summary>
    /// Whether to include a logo in the center of the QR code.
    /// </summary>
    public bool IncludeLogo { get; set; } = false;

    /// <summary>
    /// Path to the logo image.
    /// </summary>
    public string? LogoPath { get; set; }

    /// <summary>
    /// Margin around the QR code in pixels.
    /// </summary>
    public int Margin { get; set; } = 4;
}
