using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using System.Threading.Tasks;

namespace SaveState.Presentation.Services;

/// <summary>
/// Service for interacting with the system clipboard.
/// </summary>
public class ClipboardService : IClipboardService
{
    public async Task SetTextAsync(string text)
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var clipboard = (desktop.MainWindow as TopLevel)?.Clipboard;
            if (clipboard != null)
            {
                await clipboard.SetTextAsync(text);
                return;
            }
        }

        // Fallback or retry for other lifetimes if necessary
        // For now, focusing on Desktop lifetime
    }

    public async Task<string?> GetTextAsync()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var clipboard = (desktop.MainWindow as TopLevel)?.Clipboard;
            if (clipboard != null)
            {
                return await clipboard.GetTextAsync();
            }
        }
        return null;
    }

    public async Task ClearAsync()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
             var clipboard = (desktop.MainWindow as TopLevel)?.Clipboard;
             if (clipboard != null)
             {
                 await clipboard.ClearAsync();
             }
        }
    }
}
