using Microsoft.Extensions.Logging;

namespace SaveState.Infrastructure.WebBrowser.ExtensionSupport;

/// <summary>
/// Provides JavaScript shim for extension APIs.
/// This allows extensions to call browser APIs that we map to SaveState functionality.
/// </summary>
public class ExtensionApiShim
{
    private readonly ILogger<ExtensionApiShim> _logger;
    private readonly Dictionary<string, Func<object?[], Task<object?>>> _apiHandlers = new();

    public ExtensionApiShim(ILogger<ExtensionApiShim> logger)
    {
        _logger = logger;
        RegisterDefaultHandlers();
    }

    /// <summary>
    /// Gets the JavaScript code for the API shim.
    /// </summary>
    public string GetShimJavaScript()
    {
        return @"
// SaveState Extension API Shim
(function() {
    'use strict';

    // Prevent double-injection
    if (window.saveStateExtensionApi) return;

    // Storage API
    const storage = {
        local: {
            get: (keys) => saveStateApiCall('storage.local.get', [keys]),
            set: (items) => saveStateApiCall('storage.local.set', [items]),
            remove: (keys) => saveStateApiCall('storage.local.remove', [keys]),
            clear: () => saveStateApiCall('storage.local.clear', [])
        },
        sync: {
            get: (keys) => saveStateApiCall('storage.sync.get', [keys]),
            set: (items) => saveStateApiCall('storage.sync.set', [items]),
            remove: (keys) => saveStateApiCall('storage.sync.remove', [keys]),
            clear: () => saveStateApiCall('storage.sync.clear', [])
        }
    };

    // Tabs API
    const tabs = {
        query: (queryInfo) => saveStateApiCall('tabs.query', [queryInfo]),
        create: (createProperties) => saveStateApiCall('tabs.create', [createProperties]),
        update: (tabId, updateProperties) => saveStateApiCall('tabs.update', [tabId, updateProperties]),
        remove: (tabId) => saveStateApiCall('tabs.remove', [tabId]),
        executeScript: (tabId, details) => saveStateApiCall('tabs.executeScript', [tabId, details]),
        insertCSS: (tabId, details) => saveStateApiCall('tabs.insertCSS', [tabId, details]),
        removeCSS: (tabId, details) => saveStateApiCall('tabs.removeCSS', [tabId, details])
    };

    // Runtime API
    const runtime = {
        sendMessage: (message) => saveStateApiCall('runtime.sendMessage', [message]),
        onMessage: {
            addListener: (callback) => saveStateApiCall('runtime.onMessage.addListener', [callback]),
            removeListener: (callback) => saveStateApiCall('runtime.onMessage.removeListener', [callback])
        },
        getManifest: () => saveStateApiCall('runtime.getManifest', []),
        getURL: (path) => saveStateApiCall('runtime.getURL', [path])
    };

    // WebRequest API (limited)
    const webRequest = {
        onBeforeRequest: {
            addListener: (callback, filter, optExtraInfo) => 
                saveStateApiCall('webRequest.onBeforeRequest.addListener', [filter, optExtraInfo])
        },
        onHeadersReceived: {
            addListener: (callback, filter, optExtraInfo) => 
                saveStateApiCall('webRequest.onHeadersReceived.addListener', [filter, optExtraInfo])
        }
    };

    // Notifications API
    const notifications = {
        create: (notificationId, options) => 
            saveStateApiCall('notifications.create', [notificationId, options]),
        clear: (notificationId) => saveStateApiCall('notifications.clear', [notificationId]),
        onClicked: {
            addListener: (callback) => saveStateApiCall('notifications.onClicked.addListener', [callback])
        }
    };

    // Context Menus API
    const contextMenus = {
        create: (createProperties, callback) => 
            saveStateApiCall('contextMenus.create', [createProperties, callback]),
        remove: (menuItemId, callback) => saveStateApiCall('contextMenus.remove', [menuItemId, callback]),
        onClicked: {
            addListener: (callback) => saveStateApiCall('contextMenus.onClicked.addListener', [callback])
        }
    };

    // Expose APIs
    window.chrome = {
        storage,
        tabs,
        runtime,
        webRequest,
        notifications,
        contextMenus,
        extension: {
            getURL: runtime.getURL
        },
        browserAction: {
            setIcon: (details) => saveStateApiCall('browserAction.setIcon', [details]),
            setBadgeText: (details) => saveStateApiCall('browserAction.setBadgeText', [details]),
            setBadgeBackgroundColor: (details) => saveStateApiCall('browserAction.setBadgeBackgroundColor', [details]),
            onClicked: {
                addListener: (callback) => saveStateApiCall('browserAction.onClicked.addListener', [callback])
            }
        }
    };

    // Firefox compatibility
    window.browser = window.chrome;

    // Mark as injected
    window.saveStateExtensionApi = true;

    // Helper function that will be replaced by native code
    function saveStateApiCall(apiName, args) {
        // This function is intercepted by the browser control
        // and routed to native handlers
        if (window.saveStateNativeApi && window.saveStateNativeApi.call) {
            return window.saveStateNativeApi.call(apiName, args);
        }
        return Promise.reject(new Error('SaveState native API not available'));
    }

    console.log('SaveState Extension API Shim loaded');
})();
";
    }

    /// <summary>
    /// Handles an API call from JavaScript.
    /// </summary>
    public async Task<object?> HandleApiCallAsync(string apiName, object?[] args)
    {
        _logger.LogDebug("Extension API call: {Api}", apiName);

        if (_apiHandlers.TryGetValue(apiName, out var handler))
        {
            try
            {
                return await handler(args);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Extension API call failed: {Api}", apiName);
                throw;
            }
        }

        _logger.LogWarning("Unknown extension API call: {Api}", apiName);
        return null;
    }

    private void RegisterDefaultHandlers()
    {
        // Storage handlers
        _apiHandlers["storage.local.get"] = async args =>
        {
            var keys = args.ElementAtOrDefault(0) as IEnumerable<string>;
            // Implementation would fetch from local storage
            return new Dictionary<string, object?>();
        };

        _apiHandlers["storage.local.set"] = async args =>
        {
            var items = args.ElementAtOrDefault(0) as Dictionary<string, object?>;
            // Implementation would save to local storage
            return null;
        };

        _apiHandlers["storage.local.remove"] = async args =>
        {
            var keys = args.ElementAtOrDefault(0) as IEnumerable<string>;
            // Implementation would remove from local storage
            return null;
        };

        _apiHandlers["storage.local.clear"] = async _ =>
        {
            // Implementation would clear local storage
            return null;
        };

        // Tabs handlers
        _apiHandlers["tabs.query"] = async args =>
        {
            // Return current tab info
            return new[]
            {
                new
                {
                    id = 1,
                    url = "about:blank",
                    title = "Current Tab",
                    active = true
                }
            };
        };

        _apiHandlers["tabs.create"] = async args =>
        {
            var createProperties = args.ElementAtOrDefault(0) as Dictionary<string, object?>;
            var url = createProperties?.GetValueOrDefault("url")?.ToString();
            // Implementation would create new tab
            return new { id = 2, url };
        };

        // Runtime handlers
        _apiHandlers["runtime.sendMessage"] = async args =>
        {
            var message = args.ElementAtOrDefault(0);
            _logger.LogInformation("Extension message: {Message}", message);
            return new { success = true };
        };

        _apiHandlers["runtime.getManifest"] = async _ =>
        {
            // Return extension manifest
            return new
            {
                manifest_version = 2,
                name = "Extension",
                version = "1.0"
            };
        };

        _apiHandlers["runtime.getURL"] = async args =>
        {
            var path = args.ElementAtOrDefault(0)?.ToString() ?? "";
            return $"extension://{path}";
        };

        // Notifications handlers
        _apiHandlers["notifications.create"] = async args =>
        {
            var notificationId = args.ElementAtOrDefault(0)?.ToString();
            var options = args.ElementAtOrDefault(1) as Dictionary<string, object?>;
            var title = options?.GetValueOrDefault("title")?.ToString();
            var message = options?.GetValueOrDefault("message")?.ToString();

            _logger.LogInformation("Extension notification: {Title} - {Message}", title, message);

            // Show native notification
            return notificationId ?? Guid.NewGuid().ToString();
        };

        // Browser action handlers
        _apiHandlers["browserAction.setBadgeText"] = async args =>
        {
            var details = args.ElementAtOrDefault(0) as Dictionary<string, object?>;
            var text = details?.GetValueOrDefault("text")?.ToString();
            // Implementation would update badge
            return null;
        };

        _apiHandlers["browserAction.setBadgeBackgroundColor"] = async args =>
        {
            var details = args.ElementAtOrDefault(0) as Dictionary<string, object?>;
            var color = details?.GetValueOrDefault("color");
            // Implementation would update badge color
            return null;
        };
    }

    /// <summary>
    /// Registers a custom API handler.
    /// </summary>
    public void RegisterHandler(string apiName, Func<object?[], Task<object?>> handler)
    {
        _apiHandlers[apiName] = handler;
    }
}
