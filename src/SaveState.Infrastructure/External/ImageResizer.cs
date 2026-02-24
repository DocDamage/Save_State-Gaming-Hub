using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;

namespace SaveState.Infrastructure.External;

public interface IImageResizer
{
    Task<Result<ImageResizeResult>> ResizeImageAsync(byte[] imageData, ImageResizeOptions options, CancellationToken ct = default);
    Task<Result<byte[]>> OptimizeImageAsync(byte[] imageData, ImageOptimizationOptions options, CancellationToken ct = default);
}

public sealed record ImageResizeOptions(
    int MaxWidth,
    int MaxHeight,
    bool MaintainAspectRatio = true,
    ImageFormat? OutputFormat = null)
{
    public ImageFormat Format => OutputFormat ?? ImageFormat.Jpeg;
}

public sealed record ImageOptimizationOptions(
    long MaxFileSizeBytes = 1024 * 1024, // 1MB default
    int Quality = 85,
    ImageFormat? OutputFormat = null)
{
    public ImageFormat Format => OutputFormat ?? ImageFormat.Jpeg;
}

public sealed record ImageResizeResult(
    byte[] Data,
    int OriginalWidth,
    int OriginalHeight,
    int NewWidth,
    int NewHeight,
    long FileSizeBytes);

public class ImageResizer : IImageResizer
{
    private readonly ILogger<ImageResizer> _logger;

    public ImageResizer(ILogger<ImageResizer> logger)
    {
        _logger = logger;
    }

    public async Task<Result<ImageResizeResult>> ResizeImageAsync(byte[] imageData, ImageResizeOptions options, CancellationToken ct = default)
    {
        try
        {
            using var originalImage = await LoadImageFromBytesAsync(imageData, ct);
            var originalSize = new Size(originalImage.Width, originalImage.Height);

            var newSize = CalculateNewSize(originalSize, options.MaxWidth, options.MaxHeight, options.MaintainAspectRatio);

            if (newSize == originalSize)
            {
                // No resizing needed
                return Result.Success<ImageResizeResult>(new ImageResizeResult(
                    imageData,
                    originalSize.Width,
                    originalSize.Height,
                    originalSize.Width,
                    originalSize.Height,
                    imageData.Length));
            }

            using var resizedImage = new Bitmap(newSize.Width, newSize.Height);
            using var graphics = Graphics.FromImage(resizedImage);

            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.CompositingQuality = CompositingQuality.HighQuality;

            graphics.DrawImage(originalImage, 0, 0, newSize.Width, newSize.Height);

            var resizedData = await ImageToBytesAsync(resizedImage, options.OutputFormat, ct);

            return Result.Success<ImageResizeResult>(new ImageResizeResult(
                resizedData,
                originalSize.Width,
                originalSize.Height,
                newSize.Width,
                newSize.Height,
                resizedData.Length));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resize image");
            return Result.Failure<ImageResizeResult>($"Image resize failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<byte[]>> OptimizeImageAsync(byte[] imageData, ImageOptimizationOptions options, CancellationToken ct = default)
    {
        try
        {
            using var image = await LoadImageFromBytesAsync(imageData, ct);

            // If already under size limit, return as-is
            if (imageData.Length <= options.MaxFileSizeBytes)
            {
                return Result.Success<byte[]>(imageData);
            }

            // Try different quality levels until under size limit
            var quality = options.Quality;
            var minQuality = 50; // Don't go below 50% quality

            while (quality >= minQuality)
            {
                var optimizedData = await ImageToBytesAsync(image, options.OutputFormat, quality, ct);

                if (optimizedData.Length <= options.MaxFileSizeBytes)
                {
                    return Result.Success<byte[]>(optimizedData);
                }

                quality -= 10; // Reduce quality by 10%
            }

            // If we still can't get under the limit, return the best we have
            var finalData = await ImageToBytesAsync(image, options.OutputFormat, minQuality, ct);
            return Result.Success<byte[]>(finalData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to optimize image");
            return Result.Failure<byte[]>($"Image optimization failed: {ex.Message}", ErrorType.Internal);
        }
    }

    private static async Task<Image> LoadImageFromBytesAsync(byte[] imageData, CancellationToken ct)
    {
        using var stream = new MemoryStream(imageData);
        var image = await Task.Run(() => Image.FromStream(stream), ct);
        return image;
    }

    private static async Task<byte[]> ImageToBytesAsync(Image image, ImageFormat format, CancellationToken ct)
    {
        return await ImageToBytesAsync(image, format, 85, ct); // Default quality
    }

    private static async Task<byte[]> ImageToBytesAsync(Image image, ImageFormat format, int quality, CancellationToken ct)
    {
        using var stream = new MemoryStream();

        if (format == ImageFormat.Jpeg)
        {
            var encoder = ImageCodecInfo.GetImageEncoders().FirstOrDefault(e => e.FormatID == ImageFormat.Jpeg.Guid);
            if (encoder != null)
            {
                var encoderParams = new EncoderParameters(1);
                encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
                await Task.Run(() => image.Save(stream, encoder, encoderParams), ct);
            }
            else
            {
                await Task.Run(() => image.Save(stream, format), ct);
            }
        }
        else
        {
            await Task.Run(() => image.Save(stream, format), ct);
        }

        return stream.ToArray();
    }

    private static Size CalculateNewSize(Size originalSize, int maxWidth, int maxHeight, bool maintainAspectRatio)
    {
        if (!maintainAspectRatio)
        {
            return new Size(Math.Min(originalSize.Width, maxWidth), Math.Min(originalSize.Height, maxHeight));
        }

        var ratioX = (double)maxWidth / originalSize.Width;
        var ratioY = (double)maxHeight / originalSize.Height;
        var ratio = Math.Min(ratioX, ratioY);

        var newWidth = (int)(originalSize.Width * ratio);
        var newHeight = (int)(originalSize.Height * ratio);

        return new Size(newWidth, newHeight);
    }
}
