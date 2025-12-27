using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SaveState.Core.Services.Media
{
    public class Screenshot
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string FilePath { get; set; } = string.Empty;
        public string GameId { get; set; } = string.Empty;
        public string GameName { get; set; } = string.Empty;
        public DateTime CapturedAt { get; set; }
        public string? Caption { get; set; }
        public List<string> Tags { get; set; } = new();
        public long FileSizeBytes { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }

    public class ScreenshotService
    {
        private static ScreenshotService? _instance;
        private readonly string _screenshotsPath;
        private readonly List<Screenshot> _screenshots = new();

        public static ScreenshotService Instance => _instance ??= new ScreenshotService();

        private ScreenshotService()
        {
            _screenshotsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "SaveState2", "data", "screenshots");
            if (!Directory.Exists(_screenshotsPath)) Directory.CreateDirectory(_screenshotsPath);
            LoadScreenshots();
        }

        public async Task<Screenshot?> CaptureWindowAsync(string gameId, string gameName, IntPtr windowHandle = default)
        {
            try
            {
                var timestamp = DateTime.Now;
                var fileName = $"screenshot_{gameId}_{timestamp:yyyyMMdd_HHmmss}.png";
                var filePath = Path.Combine(_screenshotsPath, fileName);

                // Platform-specific screen capture would go here
                // For now, create a placeholder
                await Task.Run(() =>
                {
                    // In production: Use platform APIs to capture window
                    Console.WriteLine($"📸 Capturing screenshot for {gameName}");
                });

                var screenshot = new Screenshot
                {
                    FilePath = filePath,
                    GameId = gameId,
                    GameName = gameName,
                    CapturedAt = timestamp
                };

                _screenshots.Add(screenshot);
                return screenshot;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Screenshot error: {ex.Message}");
                return null;
            }
        }

        public async Task<Screenshot?> CaptureFullScreenAsync(string gameId, string gameName)
        {
            return await CaptureWindowAsync(gameId, gameName);
        }

        public async Task<Screenshot?> CaptureRegionAsync(string gameId, string gameName, int x, int y, int width, int height)
        {
            var screenshot = await CaptureWindowAsync(gameId, gameName);
            if (screenshot != null)
            {
                screenshot.Width = width;
                screenshot.Height = height;
            }
            return screenshot;
        }

        public List<Screenshot> GetAllScreenshots() => _screenshots.OrderByDescending(s => s.CapturedAt).ToList();

        public List<Screenshot> GetScreenshotsForGame(string gameId) =>
            _screenshots.Where(s => s.GameId == gameId).OrderByDescending(s => s.CapturedAt).ToList();

        public List<Screenshot> SearchByTags(params string[] tags) =>
            _screenshots.Where(s => tags.Any(t => s.Tags.Contains(t, StringComparer.OrdinalIgnoreCase))).ToList();

        public void AddCaption(string screenshotId, string caption)
        {
            var screenshot = _screenshots.FirstOrDefault(s => s.Id == screenshotId);
            if (screenshot != null)
            {
                screenshot.Caption = caption;
            }
        }

        public void AddTags(string screenshotId, params string[] tags)
        {
            var screenshot = _screenshots.FirstOrDefault(s => s.Id == screenshotId);
            if (screenshot != null)
            {
                foreach (var tag in tags)
                {
                    if (!screenshot.Tags.Contains(tag))
                    {
                        screenshot.Tags.Add(tag);
                    }
                }
            }
        }

        public bool DeleteScreenshot(string screenshotId)
        {
            var screenshot = _screenshots.FirstOrDefault(s => s.Id == screenshotId);
            if (screenshot == null) return false;

            try
            {
                if (File.Exists(screenshot.FilePath))
                {
                    File.Delete(screenshot.FilePath);
                }
                _screenshots.Remove(screenshot);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public long GetTotalStorageUsed()
        {
            return _screenshots.Sum(s => s.FileSizeBytes);
        }

        public string GetScreenshotsPath() => _screenshotsPath;

        private void LoadScreenshots()
        {
            if (!Directory.Exists(_screenshotsPath)) return;

            foreach (var file in Directory.GetFiles(_screenshotsPath, "*.png"))
            {
                var fileInfo = new FileInfo(file);
                var parts = Path.GetFileNameWithoutExtension(file).Split('_');
                
                _screenshots.Add(new Screenshot
                {
                    FilePath = file,
                    GameId = parts.Length > 1 ? parts[1] : "unknown",
                    CapturedAt = fileInfo.CreationTime,
                    FileSizeBytes = fileInfo.Length
                });
            }
        }
    }
}
