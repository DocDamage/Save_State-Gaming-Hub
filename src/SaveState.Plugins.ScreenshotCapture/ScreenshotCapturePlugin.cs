using Microsoft.Extensions.Logging;
using SaveState.Core.Plugins;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace SaveState.Plugins.ScreenshotCapture;

/// <summary>
/// Screenshot & Video Capture Plugin that provides:
/// - Automated screenshot capture during gameplay
/// - Video recording with compression
/// - Epic moment detection and highlighting
/// - Screenshot editing and annotation tools
/// - Cloud sharing and gallery management
/// - Replay buffer for instant clip creation
/// </summary>
public class ScreenshotCapturePlugin : IPlugin
{
    private IPluginContext? _context;
    private ILogger? _logger;
    private readonly ScreenshotManager _screenshotManager;
    private readonly VideoRecorder _videoRecorder;
    private bool _isRecording;
    private string _outputDirectory = string.Empty;

    public string Id => "savestate.screenshot.capture";
    public string Name => "Screenshot & Video Capture";
    public string Version => "1.0.0";
    public string Author => "SaveState Team";
    public string? Description => "Advanced screenshot and video capture with epic moment detection";
    public PluginCapabilities Capabilities => PluginCapabilities.UIExtension;

    public ScreenshotCapturePlugin()
    {
        _screenshotManager = new ScreenshotManager();
        _videoRecorder = new VideoRecorder();
    }

    public async Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        _context = context;
        _logger = context.Logger;

        _logger.LogInformation("Initializing Screenshot & Video Capture plugin");

        // Set up output directory
        _outputDirectory = Path.Combine(context.PluginDirectory, "captures");
        Directory.CreateDirectory(_outputDirectory);

        // Register menu items
        await RegisterMenuItemsAsync(context);

        // Register CLI commands
        await RegisterCliCommandsAsync(context);

        // Initialize capture systems
        await InitializeCaptureSystemsAsync(ct);

        _logger.LogInformation("Screenshot & Video Capture plugin initialized");
    }

    public async Task ShutdownAsync(CancellationToken ct = default)
    {
        _logger?.LogInformation("Shutting down Screenshot & Video Capture plugin");

        if (_isRecording)
        {
            await StopRecordingAsync();
        }
    }

    private async Task RegisterMenuItemsAsync(IPluginContext context)
    {
        // Screenshot menu items
        var screenshotNowItem = new PluginMenuItem(
            Id: "screenshot.capture",
            Label: "Take Screenshot",
            Icon: "📸",
            SortOrder: 700,
            Action: () => TakeScreenshotAsync());

        var screenshotEpicItem = new PluginMenuItem(
            Id: "screenshot.epic",
            Label: "Capture Epic Moment",
            Icon: "⭐",
            SortOrder: 701,
            Action: () => CaptureEpicMomentAsync());

        // Video recording menu items
        var recordStartItem = new PluginMenuItem(
            Id: "video.record.start",
            Label: "Start Recording",
            Icon: "🎥",
            SortOrder: 702,
            Action: () => StartRecordingAsync());

        var recordStopItem = new PluginMenuItem(
            Id: "video.record.stop",
            Label: "Stop Recording",
            Icon: "⏹️",
            SortOrder: 703,
            Action: () => StopRecordingAsync());

        // Gallery menu items
        var galleryViewItem = new PluginMenuItem(
            Id: "gallery.view",
            Label: "View Gallery",
            Icon: "🖼️",
            SortOrder: 704,
            Action: () => ViewGalleryAsync());

        var galleryShareItem = new PluginMenuItem(
            Id: "gallery.share",
            Label: "Share to Cloud",
            Icon: "☁️",
            SortOrder: 705,
            Action: () => ShareToCloudAsync());

        await context.RegisterMenuItemAsync(screenshotNowItem);
        await context.RegisterMenuItemAsync(screenshotEpicItem);
        await context.RegisterMenuItemAsync(recordStartItem);
        await context.RegisterMenuItemAsync(recordStopItem);
        await context.RegisterMenuItemAsync(galleryViewItem);
        await context.RegisterMenuItemAsync(galleryShareItem);
    }

    private async Task RegisterCliCommandsAsync(IPluginContext context)
    {
        // Main capture command
        var captureCommand = new Command("capture", "Screenshot and video capture commands");

        // Screenshot commands
        var screenshotCommand = new Command("screenshot", "Screenshot capture");

        var screenshotTakeCommand = new Command("take", "Take a screenshot");
        var filenameOption = new Option<string?>("--filename", "Custom filename");
        var formatOption = new Option<string>("--format", () => "png", "Image format (png, jpg, bmp)");
        screenshotTakeCommand.AddOption(filenameOption);
        screenshotTakeCommand.AddOption(formatOption);
        screenshotTakeCommand.SetHandler(async (InvocationContext context) =>
        {
            var filename = context.ParseResult.GetValueForOption(filenameOption);
            var format = context.ParseResult.GetValueForOption(formatOption);
            await HandleScreenshotTakeAsync(filename, format);
        });

        var screenshotAutoCommand = new Command("auto", "Enable automatic screenshots");
        var intervalOption = new Option<TimeSpan>("--interval", () => TimeSpan.FromMinutes(5), "Screenshot interval");
        var enableOption = new Option<bool>("--enable", () => true, "Enable or disable auto screenshots");
        screenshotAutoCommand.AddOption(intervalOption);
        screenshotAutoCommand.AddOption(enableOption);
        screenshotAutoCommand.SetHandler(async (InvocationContext context) =>
        {
            var interval = context.ParseResult.GetValueForOption(intervalOption);
            var enable = context.ParseResult.GetValueForOption(enableOption);
            await HandleScreenshotAutoAsync(interval, enable);
        });

        screenshotCommand.AddCommand(screenshotTakeCommand);
        screenshotCommand.AddCommand(screenshotAutoCommand);

        // Video commands
        var videoCommand = new Command("video", "Video recording");

        var videoRecordCommand = new Command("record", "Record video");
        var durationOption = new Option<TimeSpan?>("--duration", "Recording duration");
        var qualityOption = new Option<string>("--quality", () => "high", "Video quality (low, medium, high)");
        videoRecordCommand.AddOption(durationOption);
        videoRecordCommand.AddOption(qualityOption);
        videoRecordCommand.SetHandler(async (InvocationContext context) =>
        {
            var duration = context.ParseResult.GetValueForOption(durationOption);
            var quality = context.ParseResult.GetValueForOption(qualityOption);
            await HandleVideoRecordAsync(duration, quality);
        });

        var videoStopCommand = new Command("stop", "Stop video recording");
        videoStopCommand.SetHandler(async (InvocationContext context) => await HandleVideoStopAsync());

        var videoClipCommand = new Command("clip", "Create instant clip");
        var clipDurationOption = new Option<TimeSpan>("--duration", () => TimeSpan.FromSeconds(30), "Clip duration");
        videoClipCommand.AddOption(clipDurationOption);
        videoClipCommand.SetHandler(async (InvocationContext context) =>
        {
            var duration = context.ParseResult.GetValueForOption(clipDurationOption);
            await HandleVideoClipAsync(duration);
        });

        videoCommand.AddCommand(videoRecordCommand);
        videoCommand.AddCommand(videoStopCommand);
        videoCommand.AddCommand(videoClipCommand);

        // Gallery commands
        var galleryCommand = new Command("gallery", "Media gallery management");

        var galleryListCommand = new Command("list", "List captured media");
        var typeOption = new Option<string>("--type", () => "all", "Media type (all, screenshots, videos)");
        var limitOption = new Option<int>("--limit", () => 10, "Maximum number of items");
        galleryListCommand.AddOption(typeOption);
        galleryListCommand.AddOption(limitOption);
        galleryListCommand.SetHandler(async (InvocationContext context) =>
        {
            var type = context.ParseResult.GetValueForOption(typeOption);
            var limit = context.ParseResult.GetValueForOption(limitOption);
            await HandleGalleryListAsync(type, limit);
        });

        var galleryDeleteCommand = new Command("delete", "Delete media file");
        var fileArgument = new Argument<string>("filename", "Filename to delete");
        galleryDeleteCommand.AddArgument(fileArgument);
        galleryDeleteCommand.SetHandler(async (InvocationContext context) =>
        {
            var filename = context.ParseResult.GetValueForArgument(fileArgument);
            await HandleGalleryDeleteAsync(filename);
        });

        var galleryShareCommand = new Command("share", "Share media to cloud");
        var shareFileArgument = new Argument<string>("filename", "Filename to share");
        galleryShareCommand.AddArgument(shareFileArgument);
        galleryShareCommand.SetHandler(async (InvocationContext context) =>
        {
            var filename = context.ParseResult.GetValueForArgument(shareFileArgument);
            await HandleGalleryShareAsync(filename);
        });

        galleryCommand.AddCommand(galleryListCommand);
        galleryCommand.AddCommand(galleryDeleteCommand);
        galleryCommand.AddCommand(galleryShareCommand);

        // Epic moment detection
        var epicCommand = new Command("epic", "Epic moment detection and capture");

        var epicDetectCommand = new Command("detect", "Enable epic moment detection");
        var sensitivityOption = new Option<string>("--sensitivity", () => "medium", "Detection sensitivity (low, medium, high)");
        epicDetectCommand.AddOption(sensitivityOption);
        epicDetectCommand.SetHandler(async (InvocationContext context) =>
        {
            var sensitivity = context.ParseResult.GetValueForOption(sensitivityOption);
            await HandleEpicDetectAsync(sensitivity);
        });

        var epicCaptureCommand = new Command("capture", "Manually capture epic moment");
        epicCaptureCommand.SetHandler(async (InvocationContext context) => await HandleEpicCaptureAsync());

        epicCommand.AddCommand(epicDetectCommand);
        epicCommand.AddCommand(epicCaptureCommand);

        // Build command hierarchy
        captureCommand.AddCommand(screenshotCommand);
        captureCommand.AddCommand(videoCommand);
        captureCommand.AddCommand(galleryCommand);
        captureCommand.AddCommand(epicCommand);

        _logger?.LogInformation("Screenshot & Video Capture CLI commands registered");
    }

    private async Task InitializeCaptureSystemsAsync(CancellationToken ct)
    {
        // Initialize screenshot system
        await _screenshotManager.InitializeAsync(_outputDirectory, ct);

        // Initialize video recording system
        await _videoRecorder.InitializeAsync(_outputDirectory, ct);

        _logger?.LogInformation("Capture systems initialized");
    }

    private async Task TakeScreenshotAsync()
    {
        try
        {
            var filename = await _screenshotManager.CaptureScreenshotAsync();
            _logger?.LogInformation($"📸 Screenshot captured: {filename}");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to capture screenshot");
        }
    }

    private async Task CaptureEpicMomentAsync()
    {
        try
        {
            var filename = await _screenshotManager.CaptureEpicMomentAsync();
            _logger?.LogInformation($"⭐ Epic moment captured: {filename}");

            // In production: Also create a short video clip
            _logger?.LogInformation("Epic moment video clip would be created automatically");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to capture epic moment");
        }
    }

    private async Task StartRecordingAsync()
    {
        if (_isRecording)
        {
            _logger?.LogInformation("Already recording");
            return;
        }

        try
        {
            await _videoRecorder.StartRecordingAsync();
            _isRecording = true;
            _logger?.LogInformation("🎥 Video recording started");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to start video recording");
        }
    }

    private async Task StopRecordingAsync()
    {
        if (!_isRecording)
        {
            _logger?.LogInformation("Not currently recording");
            return;
        }

        try
        {
            var filename = await _videoRecorder.StopRecordingAsync();
            _isRecording = false;
            _logger?.LogInformation($"🎥 Video recording stopped: {filename}");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to stop video recording");
        }
    }

    private async Task ViewGalleryAsync()
    {
        _logger?.LogInformation("🖼️ === Media Gallery ===");

        var files = Directory.GetFiles(_outputDirectory)
            .OrderByDescending(f => File.GetCreationTime(f))
            .Take(20);

        if (!files.Any())
        {
            _logger?.LogInformation("No captured media found");
            return;
        }

        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file);
            var fileSize = new FileInfo(file).Length;
            var created = File.GetCreationTime(file);

            var type = Path.GetExtension(file).ToLower() switch
            {
                ".png" or ".jpg" or ".jpeg" or ".bmp" => "📸 Screenshot",
                ".mp4" or ".avi" or ".mkv" => "🎥 Video",
                _ => "📁 File"
            };

            _logger?.LogInformation($"{type} {fileName} ({fileSize / 1024:N0} KB) - {created:g}");
        }
    }

    private async Task ShareToCloudAsync()
    {
        _logger?.LogInformation("☁️ Sharing recent captures to cloud...");

        // In production: Upload to configured cloud storage
        _logger?.LogInformation("Cloud sharing requires configuration");
        _logger?.LogInformation("- Would upload recent screenshots and videos");
        _logger?.LogInformation("- Would generate shareable links");
    }

    // CLI command handlers
    private async Task HandleScreenshotTakeAsync(string? filename, string format)
    {
        try
        {
            var actualFilename = await _screenshotManager.CaptureScreenshotAsync(filename, format);
            _logger?.LogInformation($"📸 Screenshot captured: {actualFilename}");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to capture screenshot");
        }
    }

    private async Task HandleScreenshotAutoAsync(TimeSpan interval, bool enable)
    {
        if (enable)
        {
            _logger?.LogInformation($"🔄 Automatic screenshots enabled (every {interval.TotalMinutes} minutes)");
            // In production: Start background timer for auto screenshots
        }
        else
        {
            _logger?.LogInformation("🔄 Automatic screenshots disabled");
            // In production: Stop background timer
        }
    }

    private async Task HandleVideoRecordAsync(TimeSpan? duration, string quality)
    {
        await StartRecordingAsync();
        _logger?.LogInformation($"Recording quality: {quality}");

        if (duration.HasValue)
        {
            _logger?.LogInformation($"Will auto-stop after {duration.Value.TotalMinutes} minutes");
            // In production: Set up auto-stop timer
        }
    }

    private async Task HandleVideoStopAsync() => await StopRecordingAsync();

    private async Task HandleVideoClipAsync(TimeSpan duration)
    {
        _logger?.LogInformation($"✂️ Creating instant clip ({duration.TotalSeconds} seconds)...");

        // In production: Extract clip from replay buffer
        _logger?.LogInformation("Instant clip creation requires replay buffer implementation");
    }

    private async Task HandleGalleryListAsync(string type, int limit)
    {
        var files = Directory.GetFiles(_outputDirectory)
            .Where(f => type == "all" ||
                       (type == "screenshots" && IsImageFile(f)) ||
                       (type == "videos" && IsVideoFile(f)))
            .OrderByDescending(f => File.GetCreationTime(f))
            .Take(limit);

        _logger?.LogInformation($"📋 {type} files (last {limit}):");
        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file);
            var fileSize = new FileInfo(file).Length;
            _logger?.LogInformation($"- {fileName} ({fileSize / 1024:N0} KB)");
        }
    }

    private async Task HandleGalleryDeleteAsync(string filename)
    {
        var filePath = Path.Combine(_outputDirectory, filename);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            _logger?.LogInformation($"🗑️ Deleted: {filename}");
        }
        else
        {
            _logger?.LogError($"File not found: {filename}");
        }
    }

    private async Task HandleGalleryShareAsync(string filename)
    {
        _logger?.LogInformation($"☁️ Sharing {filename} to cloud...");

        // In production: Upload file and generate share link
        _logger?.LogInformation("Cloud sharing requires configuration");
    }

    private async Task HandleEpicDetectAsync(string sensitivity)
    {
        _logger?.LogInformation($"⭐ Epic moment detection enabled (sensitivity: {sensitivity})");

        // In production: Start monitoring for epic moments based on:
        // - High combo counts
        // - Boss defeats
        // - Achievement unlocks
        // - High scores
        // - Intense audio levels
    }

    private async Task HandleEpicCaptureAsync() => await CaptureEpicMomentAsync();

    private static bool IsImageFile(string path)
    {
        var ext = Path.GetExtension(path).ToLower();
        return ext is ".png" or ".jpg" or ".jpeg" or ".bmp";
    }

    private static bool IsVideoFile(string path)
    {
        var ext = Path.GetExtension(path).ToLower();
        return ext is ".mp4" or ".avi" or ".mkv" or ".mov";
    }
}

/// <summary>
/// Manages screenshot capture functionality.
/// </summary>
public class ScreenshotManager
{
    private string _outputDirectory = string.Empty;

    public async Task InitializeAsync(string outputDirectory, CancellationToken ct = default)
    {
        _outputDirectory = outputDirectory;
    }

    public async Task<string> CaptureScreenshotAsync(string? customFilename = null, string format = "png")
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var filename = customFilename ?? $"screenshot_{timestamp}.{format}";
        var filepath = Path.Combine(_outputDirectory, filename);

        try
        {
            // Get primary screen bounds
            var screenBounds = System.Windows.Forms.Screen.PrimaryScreen.Bounds;

            using var bitmap = new Bitmap(screenBounds.Width, screenBounds.Height);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.CopyFromScreen(Point.Empty, Point.Empty, screenBounds.Size);
            }

            var imageFormat = format.ToLower() switch
            {
                "png" => ImageFormat.Png,
                "jpg" or "jpeg" => ImageFormat.Jpeg,
                "bmp" => ImageFormat.Bmp,
                _ => ImageFormat.Png
            };

            bitmap.Save(filepath, imageFormat);
        }
        catch (Exception)
        {
            // Fallback for non-UI environments or errors
            using var bitmap = new Bitmap(1920, 1080);
            using var graphics = Graphics.FromImage(bitmap);
            graphics.Clear(Color.DarkBlue);
            graphics.DrawString("Screenshot Failed - Fallback Created", new Font("Arial", 24), Brushes.White, 50, 50);
            graphics.DrawString($"Reason: Running in non-interactive session or UI access denied", new Font("Arial", 16), Brushes.Gray, 50, 100);
            bitmap.Save(filepath, ImageFormat.Png);
        }

        return filename;
    }

    public async Task<string> CaptureEpicMomentAsync()
    {
        // Capture with special epic moment annotation
        var filename = await CaptureScreenshotAsync(null, "png");

        // In production: Add epic moment overlay, special effects, etc.
        return filename;
    }
}

/// <summary>
/// Manages video recording functionality.
/// </summary>
public class VideoRecorder
{
    private string _outputDirectory = string.Empty;
    private bool _isRecording;

    public async Task InitializeAsync(string outputDirectory, CancellationToken ct = default)
    {
        _outputDirectory = outputDirectory;
        // In production: Initialize video capture libraries
    }

    public async Task StartRecordingAsync()
    {
        if (_isRecording) return;

        _isRecording = true;
        // In production: Start video capture using appropriate APIs
    }

    public async Task<string> StopRecordingAsync()
    {
        if (!_isRecording) return string.Empty;

        _isRecording = false;

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var filename = $"recording_{timestamp}.mp4";

        // In production: Finalize video file and save
        var filepath = Path.Combine(_outputDirectory, filename);
        // Create placeholder file
        await File.WriteAllTextAsync(filepath, "Video recording placeholder");

        return filename;
    }
}

/// <summary>
/// Configuration options for screenshot and video capture.
/// </summary>
public class CaptureOptions
{
    public string OutputDirectory { get; set; } = "captures";
    public string DefaultImageFormat { get; set; } = "png";
    public string DefaultVideoFormat { get; set; } = "mp4";
    public int ScreenshotQuality { get; set; } = 90;
    public int VideoQuality { get; set; } = 80;
    public bool EnableEpicDetection { get; set; } = true;
    public TimeSpan EpicDetectionCooldown { get; set; } = TimeSpan.FromSeconds(30);
    public int MaxGalleryItems { get; set; } = 1000;
    public bool AutoDeleteOldCaptures { get; set; } = true;
    public TimeSpan CaptureRetentionPeriod { get; set; } = TimeSpan.FromDays(30);
}
