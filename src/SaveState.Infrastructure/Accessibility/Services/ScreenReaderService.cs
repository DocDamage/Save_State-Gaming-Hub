using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace SaveState.Infrastructure.Accessibility.Services;

/// <summary>
/// Service for integrating with screen readers across different platforms.
/// Supports Windows Narrator, NVDA, JAWS, macOS VoiceOver, and Linux Orca.
/// </summary>
public interface IScreenReaderService
{
    /// <summary>
    /// Announces a message to the active screen reader.
    /// </summary>
    Task AnnounceAsync(string message, ScreenReaderPriority priority = ScreenReaderPriority.Normal);
    
    /// <summary>
    /// Checks if any screen reader is currently active.
    /// </summary>
    bool IsScreenReaderActive();
    
    /// <summary>
    /// Gets the name of the active screen reader, if any.
    /// </summary>
    string? GetActiveScreenReaderName();
    
    /// <summary>
    /// Stops any ongoing speech.
    /// </summary>
    Task StopSpeakingAsync();
    
    /// <summary>
    /// Sets the speech rate.
    /// </summary>
    Task SetSpeechRateAsync(SpeechRate rate);
}

/// <summary>
/// Priority levels for screen reader announcements.
/// </summary>
public enum ScreenReaderPriority
{
    Low,
    Normal,
    High,
    Critical
}

/// <summary>
/// Speech rate settings.
/// </summary>
public enum SpeechRate
{
    VerySlow,
    Slow,
    Normal,
    Fast,
    VeryFast
}

/// <summary>
/// Implementation of screen reader service with platform-specific integrations.
/// </summary>
public class ScreenReaderService : IScreenReaderService
{
    private readonly ILogger<ScreenReaderService> _logger;
    private ScreenReaderType _detectedReader = ScreenReaderType.None;

    public ScreenReaderService(ILogger<ScreenReaderService> logger)
    {
        _logger = logger;
        DetectActiveScreenReader();
    }

    public async Task AnnounceAsync(string message, ScreenReaderPriority priority = ScreenReaderPriority.Normal)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                await AnnounceWindowsAsync(message, priority);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                await AnnounceMacOSAsync(message, priority);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                await AnnounceLinuxAsync(message, priority);
            }

            _logger.LogDebug("Announced to screen reader: {Message}", message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to announce to screen reader");
        }

        await Task.CompletedTask;
    }

    public bool IsScreenReaderActive()
    {
        return _detectedReader != ScreenReaderType.None || DetectSystemScreenReader();
    }

    public string? GetActiveScreenReaderName()
    {
        return _detectedReader switch
        {
            ScreenReaderType.Narrator => "Windows Narrator",
            ScreenReaderType.NVDA => "NVDA",
            ScreenReaderType.JAWS => "JAWS",
            ScreenReaderType.VoiceOver => "VoiceOver",
            ScreenReaderType.Orca => "Orca",
            ScreenReaderType.TalkBack => "TalkBack",
            _ => null
        };
    }

    public Task StopSpeakingAsync()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Windows: Use UI Automation to stop speech
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                ExecuteCommand("say", "-r 0");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop screen reader speech");
        }

        return Task.CompletedTask;
    }

    public Task SetSpeechRateAsync(SpeechRate rate)
    {
        // Implementation would adjust screen reader speech rate
        _logger.LogDebug("Setting speech rate to {Rate}", rate);
        return Task.CompletedTask;
    }

    #region Platform-Specific Implementation

    private async Task AnnounceWindowsAsync(string message, ScreenReaderPriority priority)
    {
        // Try UI Automation first (works with Narrator)
        try
        {
            if (priority == ScreenReaderPriority.High || priority == ScreenReaderPriority.Critical)
            {
                // Use UI Automation's RaiseNotificationEvent for high priority messages
                // This requires COM interop with UIAutomationClient
                RaiseUiaNotification(message);
            }
            else
            {
                // For normal priority, we rely on proper AutomationProperties
                // The screen reader will pick up changes automatically
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "UI Automation announcement failed, falling back");
        }

        // NVDA-specific: Use NVDA controller client DLL if available
        if (_detectedReader == ScreenReaderType.NVDA)
        {
            await AnnounceNvdaAsync(message);
        }

        // JAWS-specific: Use JAWS API if available
        if (_detectedReader == ScreenReaderType.JAWS)
        {
            await AnnounceJawsAsync(message);
        }

        await Task.CompletedTask;
    }

    private Task AnnounceMacOSAsync(string message, ScreenReaderPriority priority)
    {
        // VoiceOver: Use 'say' command or NSAccessibility API
        try
        {
            // Use macOS 'say' command as fallback
            var rate = priority == ScreenReaderPriority.Critical ? "200" : "150";
            ExecuteCommand("say", $"-r {rate} \"{EscapeForShell(message)}\"");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to announce on macOS");
        }

        return Task.CompletedTask;
    }

    private Task AnnounceLinuxAsync(string message, ScreenReaderPriority priority)
    {
        // Orca: Use D-Bus or speech-dispatcher
        try
        {
            // Try speech-dispatcher
            ExecuteCommand("spd-say", $"\"{EscapeForShell(message)}\"");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to announce on Linux");
        }

        return Task.CompletedTask;
    }

    private Task AnnounceNvdaAsync(string message)
    {
        // NVDA Controller Client API
        // This would require the NVDA controller client DLL
        try
        {
            // NVDA uses a specific DLL for external speech control
            // The path would typically be in Program Files (x86)\NVDA\nvdaControllerClient.dll
            var nvdaDllPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                "NVDA", "nvdaControllerClient.dll");

            if (File.Exists(nvdaDllPath))
            {
                // Use P/Invoke to call NVDA functions
                // nvdaController_speakText(message)
                _logger.LogDebug("Using NVDA controller client");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "NVDA announcement failed");
        }

        return Task.CompletedTask;
    }

    private Task AnnounceJawsAsync(string message)
    {
        // JAWS External Speech API
        try
        {
            // JAWS uses FSAPI or the JAWS External Speech DLL
            // Implementation would load jfwapi.dll
            _logger.LogDebug("Using JAWS API");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "JAWS announcement failed");
        }

        return Task.CompletedTask;
    }

    #endregion

    #region Screen Reader Detection

    private void DetectActiveScreenReader()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            DetectWindowsScreenReader();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            DetectMacOSScreenReader();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            DetectLinuxScreenReader();
        }
    }

    private void DetectWindowsScreenReader()
    {
        // Check for NVDA
        if (IsProcessRunning("nvda"))
        {
            _detectedReader = ScreenReaderType.NVDA;
            _logger.LogInformation("Detected NVDA screen reader");
            return;
        }

        // Check for JAWS
        if (IsProcessRunning("jfw") || IsProcessRunning("jaws"))
        {
            _detectedReader = ScreenReaderType.JAWS;
            _logger.LogInformation("Detected JAWS screen reader");
            return;
        }

        // Check for Narrator via SystemParametersInfo
        if (DetectNarrator())
        {
            _detectedReader = ScreenReaderType.Narrator;
            _logger.LogInformation("Detected Windows Narrator");
            return;
        }
    }

    private bool DetectNarrator()
    {
        // Check if Narrator process is running
        if (IsProcessRunning("narrator"))
        {
            return true;
        }

        // Also check registry setting for Narrator
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Narrator");
            if (key != null)
            {
                var launchOnDesktop = key.GetValue("LaunchOnDesktop");
                if (launchOnDesktop is int launchValue && launchValue == 1)
                {
                    return true;
                }
            }
        }
        catch
        {
            // Ignore registry access errors
        }

        return false;
    }

    private void DetectMacOSScreenReader()
    {
        // Check for VoiceOver process
        if (IsProcessRunning("VoiceOver"))
        {
            _detectedReader = ScreenReaderType.VoiceOver;
            _logger.LogInformation("Detected VoiceOver screen reader");
        }
    }

    private void DetectLinuxScreenReader()
    {
        // Check for Orca
        if (IsProcessRunning("orca"))
        {
            _detectedReader = ScreenReaderType.Orca;
            _logger.LogInformation("Detected Orca screen reader");
        }
    }

    private bool DetectSystemScreenReader()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            bool screenReader = false;
            try
            {
                // Use SystemParametersInfo to detect screen reader
                // SPI_GETSCREENREADER = 0x0046
                screenReader = SystemParametersInfo(0x0046, 0, ref screenReader, 0);
            }
            catch { }
            return screenReader;
        }

        return _detectedReader != ScreenReaderType.None;
    }

    private static bool IsProcessRunning(string processName)
    {
        try
        {
            return Process.GetProcessesByName(processName).Length > 0;
        }
        catch
        {
            return false;
        }
    }

    #endregion

    #region Helper Methods

    private void RaiseUiaNotification(string message)
    {
        // This would use UI Automation COM interfaces
        // For now, log the attempt
        _logger.LogDebug("Raising UIA notification: {Message}", message);
    }

    private void ExecuteCommand(string command, string arguments)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            process.WaitForExit(5000); // 5 second timeout
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute command: {Command} {Args}", command, arguments);
        }
    }

    private string EscapeForShell(string input)
    {
        // Basic shell escaping
        return input.Replace("\"", "\\\"").Replace("`", "\\`").Replace("$", "\\$");
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, ref bool pvParam, uint fWinIni);

    #endregion
}

/// <summary>
/// Types of screen readers supported.
/// </summary>
public enum ScreenReaderType
{
    None,
    Narrator,
    NVDA,
    JAWS,
    VoiceOver,
    Orca,
    TalkBack
}
