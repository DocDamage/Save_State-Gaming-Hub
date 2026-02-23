using System.Runtime.CompilerServices;

namespace SaveState.EndToEndTests.Infrastructure;

/// <summary>
/// Helper class for capturing screenshots on test failures.
/// </summary>
public static class ScreenshotHelper
{
    private static readonly string ScreenshotDirectory = Path.Combine(
        Directory.GetCurrentDirectory(), 
        "test-screenshots");

    static ScreenshotHelper()
    {
        if (!Directory.Exists(ScreenshotDirectory))
        {
            Directory.CreateDirectory(ScreenshotDirectory);
        }
    }

    /// <summary>
    /// Executes a test action and captures a screenshot if it fails.
    /// </summary>
    public static async Task CaptureOnFailureAsync(
        Func<Task> testAction,
        AvaloniaTestHost host,
        [CallerMemberName] string testName = "",
        [CallerFilePath] string filePath = "")
    {
        try
        {
            await testAction();
        }
        catch (Exception ex)
        {
            try
            {
                var screenshot = await host.CaptureScreenshotAsync();
                var className = Path.GetFileNameWithoutExtension(filePath);
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var filename = $"{className}_{testName}_{timestamp}.png";
                var filepath = Path.Combine(ScreenshotDirectory, filename);
                
                await File.WriteAllBytesAsync(filepath, screenshot);
                Console.WriteLine($"Screenshot captured: {filepath}");
            }
            catch (Exception screenshotEx)
            {
                Console.WriteLine($"Failed to capture screenshot: {screenshotEx.Message}");
            }

            throw;
        }
    }

    /// <summary>
    /// Captures a screenshot with the specified name.
    /// </summary>
    public static async Task CaptureScreenshotAsync(
        AvaloniaTestHost host,
        string name,
        [CallerMemberName] string testName = "",
        [CallerFilePath] string filePath = "")
    {
        var screenshot = await host.CaptureScreenshotAsync();
        var className = Path.GetFileNameWithoutExtension(filePath);
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var filename = $"{className}_{testName}_{name}_{timestamp}.png";
        var filepath = Path.Combine(ScreenshotDirectory, filename);
        
        await File.WriteAllBytesAsync(filepath, screenshot);
        Console.WriteLine($"Screenshot captured: {filepath}");
    }

    /// <summary>
    /// Cleans up old screenshots older than the specified age.
    /// </summary>
    public static void CleanupOldScreenshots(TimeSpan? maxAge = null)
    {
        var age = maxAge ?? TimeSpan.FromDays(7);
        var cutoff = DateTime.Now - age;

        try
        {
            var files = Directory.GetFiles(ScreenshotDirectory, "*.png");
            foreach (var file in files)
            {
                if (File.GetLastWriteTime(file) < cutoff)
                {
                    File.Delete(file);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to cleanup old screenshots: {ex.Message}");
        }
    }
}
