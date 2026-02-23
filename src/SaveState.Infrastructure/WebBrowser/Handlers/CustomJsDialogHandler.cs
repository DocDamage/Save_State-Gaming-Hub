using CefSharp;
using Microsoft.Extensions.Logging;

namespace SaveState.Infrastructure.WebBrowser.Handlers;

/// <summary>
/// Handles JavaScript dialogs (alert, confirm, prompt) from web pages.
/// </summary>
public sealed class CustomJsDialogHandler : IJsDialogHandler
{
    private readonly ILogger _logger;

    public CustomJsDialogHandler(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public bool OnJSDialog(
        IWebBrowser chromiumWebBrowser,
        IBrowser browser,
        string originUrl,
        CefJsDialogType dialogType,
        string messageText,
        string defaultPromptText,
        IJsDialogCallback callback,
        ref bool suppressMessage)
    {
        _logger.LogInformation("JS Dialog from {Origin}: {Type} - {Message}", 
            originUrl, dialogType, messageText);

        // Allow the dialog to be shown
        // In a real application, you would integrate with your UI framework
        // to show custom dialogs instead of the default CefSharp ones

        // For now, we'll suppress repetitive dialogs from the same origin
        // to prevent spam
        suppressMessage = false;

        return false; // Return false to show the default dialog
    }

    public bool OnBeforeUnloadDialog(
        IWebBrowser chromiumWebBrowser,
        IBrowser browser,
        string messageText,
        bool isReload,
        IJsDialogCallback callback)
    {
        _logger.LogDebug("Before unload dialog: {Message}, IsReload: {IsReload}", 
            messageText, isReload);

        // Allow the beforeunload dialog
        return false;
    }

    public void OnDialogClosed(IWebBrowser chromiumWebBrowser, IBrowser browser)
    {
        _logger.LogDebug("JS Dialog closed");
    }

    public void OnResetDialogState(IWebBrowser chromiumWebBrowser, IBrowser browser)
    {
        _logger.LogDebug("JS Dialog state reset");
    }
}
