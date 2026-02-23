using System;
using System.IO;
using System.Threading.Tasks;
using QRCoder;
using SkiaSharp;
using SaveState.Core.MobileCompanion.Models;
using Microsoft.Extensions.Logging;

namespace SaveState.Presentation.Services;

/// <summary>
/// Service for generating and reading QR codes for mobile companion pairing.
/// </summary>
public interface IQRCodeService
{
    /// <summary>
    /// Generates a QR code image for pairing.
    /// </summary>
    Task<byte[]> GeneratePairingQRCodeAsync(PairingInfo info, int size = 256);
    
    /// <summary>
    /// Reads pairing information from a QR code image.
    /// </summary>
    Task<PairingInfo?> ReadQRCodeAsync(Stream imageStream);
    
    /// <summary>
    /// Displays the pairing QR code dialog.
    /// </summary>
    Task DisplayPairingQRCodeAsync();
}

/// <summary>
/// Information encoded in a pairing QR code.
/// </summary>
public record PairingInfo
{
    public string HubId { get; set; } = string.Empty;
    public string HubName { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public int Port { get; set; }
    public string? Token { get; set; }
    public string? PairingCode { get; set; }
    
    /// <summary>
    /// Serializes to JSON format for QR encoding.
    /// </summary>
    public string ToJson()
    {
        return System.Text.Json.JsonSerializer.Serialize(this);
    }
    
    /// <summary>
    /// Deserializes from JSON.
    /// </summary>
    public static PairingInfo? FromJson(string json)
    {
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<PairingInfo>(json);
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>
/// Implementation of QR code service using QRCoder and SkiaSharp.
/// </summary>
public class QRCodeService : IQRCodeService
{
    private readonly ILogger<QRCodeService> _logger;
    private readonly IDialogService _dialogService;

    public QRCodeService(ILogger<QRCodeService> logger, IDialogService dialogService)
    {
        _logger = logger;
        _dialogService = dialogService;
    }

    /// <inheritdoc />
    public Task<byte[]> GeneratePairingQRCodeAsync(PairingInfo info, int size = 256)
    {
        try
        {
            var jsonData = info.ToJson();
            
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(jsonData, QRCodeGenerator.ECCLevel.H);
            using var qrCode = new PngByteQRCode(qrCodeData);
            
            var qrCodeBytes = qrCode.GetGraphic(20);
            
            // Add styling with SkiaSharp if needed
            if (size != 256)
            {
                qrCodeBytes = ResizeImage(qrCodeBytes, size, size);
            }
            
            _logger.LogDebug("Generated QR code for hub {HubId}", info.HubId);
            return Task.FromResult(qrCodeBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate QR code");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<PairingInfo?> ReadQRCodeAsync(Stream imageStream)
    {
        try
        {
            // In a real implementation, use a QR code reading library
            // For now, this is a placeholder that would integrate with
            // camera capture and QR reading libraries
            _logger.LogWarning("QR code reading not fully implemented");
            return await Task.FromResult<PairingInfo?>(null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read QR code");
            return null;
        }
    }

    /// <inheritdoc />
    public async Task DisplayPairingQRCodeAsync()
    {
        // Show the pairing dialog which includes QR code
        await _dialogService.ShowPairingDialogAsync();
    }

    private byte[] ResizeImage(byte[] imageData, int width, int height)
    {
        using var original = SKBitmap.Decode(imageData);
        using var resized = original.Resize(new SKImageInfo(width, height), SKFilterQuality.High);
        using var image = SKImage.FromBitmap(resized);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }
}

/// <summary>
/// Extension methods for IDialogService to support QR code pairing.
/// </summary>
public static class QRCodeDialogExtensions
{
    /// <summary>
    /// Shows the pairing dialog with QR code.
    /// </summary>
    public static async Task ShowPairingDialogAsync(this IDialogService dialogService)
    {
        // This would show the PairingDialog.axaml
        // Implementation depends on your dialog service pattern
        await Task.CompletedTask;
    }
}
