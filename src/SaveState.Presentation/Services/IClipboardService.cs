using System.Threading.Tasks;

namespace SaveState.Presentation.Services;

/// <summary>
/// Service for interacting with the system clipboard.
/// </summary>
public interface IClipboardService
{
    /// <summary>
    /// Sets the text content of the clipboard.
    /// </summary>
    Task SetTextAsync(string text);

    /// <summary>
    /// Gets the text content from the clipboard.
    /// </summary>
    Task<string?> GetTextAsync();

    /// <summary>
    /// Clears the clipboard.
    /// </summary>
    Task ClearAsync();

    /// <summary>
    /// Sets an image to the clipboard from a file path.
    /// </summary>
    Task SetImageAsync(string imagePath);
}
