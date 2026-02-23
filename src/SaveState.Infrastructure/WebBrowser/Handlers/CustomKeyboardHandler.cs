using CefSharp;
using CefSharp.Enums;
using Microsoft.Extensions.Logging;

namespace SaveState.Infrastructure.WebBrowser.Handlers;

/// <summary>
/// Handles keyboard events for the browser, allowing custom shortcuts.
/// </summary>
public sealed class CustomKeyboardHandler : IKeyboardHandler
{
    private readonly ILogger _logger;
    private readonly Func<Guid, KeyType, int, int, bool> _onKeyEvent;
    private Guid _tabId;

    public CustomKeyboardHandler(
        ILogger logger,
        Func<Guid, KeyType, int, int, bool> onKeyEvent)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _onKeyEvent = onKeyEvent ?? throw new ArgumentNullException(nameof(onKeyEvent));
    }

    public void SetTabId(Guid tabId)
    {
        _tabId = tabId;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "Keyboard handler requires switch statement for all key codes")]
    public bool OnKeyEvent(
        IWebBrowser chromiumWebBrowser,
        IBrowser browser,
        KeyType type,
        int windowsKeyCode,
        int nativeKeyCode,
        CefEventFlags modifiers,
        bool isSystemKey)
    {
        _logger.LogTrace("Key event: {KeyType}, Code: {KeyCode}, Modifiers: {Modifiers}", 
            type, windowsKeyCode, modifiers);

        // Handle common shortcuts
        if (type == KeyType.KeyUp && modifiers == CefEventFlags.ControlDown)
        {
            switch (windowsKeyCode)
            {
                case 84: // Ctrl+T - New Tab
                    _logger.LogDebug("New Tab shortcut detected");
                    return _onKeyEvent(_tabId, type, windowsKeyCode, (int)modifiers);
                    
                case 87: // Ctrl+W - Close Tab
                    _logger.LogDebug("Close Tab shortcut detected");
                    return _onKeyEvent(_tabId, type, windowsKeyCode, (int)modifiers);
                    
                case 82: // Ctrl+R - Refresh
                    browser.Reload();
                    return true;
                    
                case 76: // Ctrl+L - Focus Address Bar
                    _logger.LogDebug("Focus Address Bar shortcut detected");
                    return _onKeyEvent(_tabId, type, windowsKeyCode, (int)modifiers);
                    
                case 80: // Ctrl+P - Print
                    browser.Print();
                    return true;
                    
                case 83: // Ctrl+S - Save
                    // Handle save
                    return true;
            }
        }

        // DevTools shortcut (F12 or Ctrl+Shift+I)
        if (type == KeyType.KeyUp)
        {
            if (windowsKeyCode == 123) // F12
            {
                browser.ShowDevTools();
                return true;
            }
            
            if (modifiers == (CefEventFlags.ControlDown | CefEventFlags.ShiftDown) && windowsKeyCode == 73) // Ctrl+Shift+I
            {
                browser.ShowDevTools();
                return true;
            }
        }

        // Zoom shortcuts
        if (type == KeyType.KeyUp && modifiers == CefEventFlags.ControlDown)
        {
            if (windowsKeyCode == 187 || windowsKeyCode == 107) // Ctrl++ or Ctrl+NumpadPlus
            {
                var currentZoom = browser.GetZoomLevelAsync().Result;
                browser.SetZoomLevel(currentZoom + 1);
                return true;
            }
            
            if (windowsKeyCode == 189 || windowsKeyCode == 109) // Ctrl+- or Ctrl+NumpadMinus
            {
                var currentZoom = browser.GetZoomLevelAsync().Result;
                browser.SetZoomLevel(currentZoom - 1);
                return true;
            }
            
            if (windowsKeyCode == 48 || windowsKeyCode == 96) // Ctrl+0 or Ctrl+Numpad0
            {
                browser.SetZoomLevel(0);
                return true;
            }
        }

        // Navigation shortcuts
        if (type == KeyType.KeyUp && modifiers == CefEventFlags.AltDown)
        {
            if (windowsKeyCode == 37) // Alt+Left
            {
                if (browser.CanGoBack)
                    browser.Back();
                return true;
            }
            
            if (windowsKeyCode == 39) // Alt+Right
            {
                if (browser.CanGoForward)
                    browser.Forward();
                return true;
            }
        }

        // Return false to allow default processing
        return _onKeyEvent(_tabId, type, windowsKeyCode, (int)modifiers);
    }

    public bool OnPreKeyEvent(
        IWebBrowser chromiumWebBrowser,
        IBrowser browser,
        KeyType type,
        int windowsKeyCode,
        int nativeKeyCode,
        CefEventFlags modifiers,
        bool isSystemKey,
        ref bool isKeyboardShortcut)
    {
        isKeyboardShortcut = false;
        
        // Mark certain combinations as keyboard shortcuts
        if (modifiers == CefEventFlags.ControlDown)
        {
            isKeyboardShortcut = windowsKeyCode is 84 or 87 or 82 or 76 or 80 or 83;
        }
        
        return false; // Allow the event to be processed
    }
}
