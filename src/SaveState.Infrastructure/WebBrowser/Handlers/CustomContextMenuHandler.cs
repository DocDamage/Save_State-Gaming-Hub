using CefSharp;
using Microsoft.Extensions.Logging;
using SaveState.Core.WebBrowser.Models;

namespace SaveState.Infrastructure.WebBrowser.Handlers;

/// <summary>
/// Handles context menu (right-click) customization.
/// </summary>
public sealed class CustomContextMenuHandler : IContextMenuHandler
{
    private readonly ILogger _logger;
    private readonly Action<Guid, List<BrowserContextMenuItem>> _onContextMenuRequested;
    private Guid _tabId;

    public CustomContextMenuHandler(
        ILogger logger,
        Action<Guid, List<BrowserContextMenuItem>> onContextMenuRequested)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _onContextMenuRequested = onContextMenuRequested ?? throw new ArgumentNullException(nameof(onContextMenuRequested));
    }

    public void SetTabId(Guid tabId)
    {
        _tabId = tabId;
    }

    public void OnBeforeContextMenu(
        IWebBrowser chromiumWebBrowser,
        IBrowser browser,
        IFrame frame,
        IContextMenuParams parameters,
        IMenuModel model)
    {
        _logger.LogTrace("Context menu requested at ({X}, {Y})", parameters.XCoord, parameters.YCoord);

        // Build custom menu items
        var menuItems = new List<BrowserContextMenuItem>();

        // Navigation items
        if (parameters.LinkUrl != null)
        {
            menuItems.Add(new BrowserContextMenuItem
            {
                Id = "open_link",
                Label = "Open Link",
                Command = $"navigate:{parameters.LinkUrl}"
            });
            menuItems.Add(new BrowserContextMenuItem
            {
                Id = "open_link_new_tab",
                Label = "Open Link in New Tab",
                Command = $"newtab:{parameters.LinkUrl}"
            });
            menuItems.Add(new BrowserContextMenuItem { IsSeparator = true });
        }

        // Text selection items
        if (!string.IsNullOrEmpty(parameters.SelectionText))
        {
            menuItems.Add(new BrowserContextMenuItem
            {
                Id = "copy",
                Label = "Copy",
                Command = "copy"
            });
            menuItems.Add(new BrowserContextMenuItem
            {
                Id = "search",
                Label = $"Search for \"{parameters.SelectionText.Truncate(20)}\"",
                Command = $"search:{parameters.SelectionText}"
            });
            menuItems.Add(new BrowserContextMenuItem { IsSeparator = true });
        }

        // Page items
        menuItems.Add(new BrowserContextMenuItem
        {
            Id = "back",
            Label = "Back",
            Command = "back",
            IsEnabled = browser.CanGoBack
        });
        menuItems.Add(new BrowserContextMenuItem
        {
            Id = "forward",
            Label = "Forward",
            Command = "forward",
            IsEnabled = browser.CanGoForward
        });
        menuItems.Add(new BrowserContextMenuItem
        {
            Id = "reload",
            Label = "Reload",
            Command = "reload"
        });
        menuItems.Add(new BrowserContextMenuItem { IsSeparator = true });

        // Developer items
        menuItems.Add(new BrowserContextMenuItem
        {
            Id = "inspect",
            Label = "Inspect Element",
            Command = $"inspect:{parameters.XCoord}:{parameters.YCoord}"
        });
        menuItems.Add(new BrowserContextMenuItem
        {
            Id = "dev_tools",
            Label = "Developer Tools",
            Command = "devtools"
        });

        _onContextMenuRequested(_tabId, menuItems);

        // Clear default menu and add our custom items
        model.Clear();
        
        foreach (var item in menuItems)
        {
            if (item.IsSeparator)
            {
                model.AddSeparator();
            }
            else
            {
                model.AddItem((CefMenuCommand)item.Id.GetHashCode(), item.Label);
            }
        }
    }

    public bool OnContextMenuCommand(
        IWebBrowser chromiumWebBrowser,
        IBrowser browser,
        IFrame frame,
        IContextMenuParams parameters,
        CefMenuCommand commandId,
        CefEventFlags eventFlags)
    {
        var commandString = commandId.ToString();
        _logger.LogDebug("Context menu command: {Command}", commandString);

        // Handle commands based on the menu item clicked
        // The actual handling is done by the service layer through the event
        return false;
    }

    public void OnContextMenuDismissed(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame)
    {
        _logger.LogTrace("Context menu dismissed");
    }

    public bool RunContextMenu(
        IWebBrowser chromiumWebBrowser,
        IBrowser browser,
        IFrame frame,
        IContextMenuParams parameters,
        IMenuModel model,
        IRunContextMenuCallback callback)
    {
        // Return false to use default context menu rendering
        // Return true if you want to implement custom context menu UI
        return false;
    }
}

internal static class StringExtensions
{
    public static string Truncate(this string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return value.Length <= maxLength ? value : value.Substring(0, maxLength) + "...";
    }
}
