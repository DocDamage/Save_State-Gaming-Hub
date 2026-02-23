using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Http.Connections.Client;

namespace SaveState.IntegrationTests.Helpers;

/// <summary>
/// Test client for SignalR hub connections.
/// </summary>
public class SignalRTestClient : IAsyncDisposable
{
    private HubConnection? _connection;
    private readonly List<object> _receivedMessages = new();
    private readonly Dictionary<string, List<object>> _messagesByMethod = new();

    /// <summary>
    /// Connects to a SignalR hub at the specified URL.
    /// </summary>
    public async Task<SignalRTestClient> ConnectAsync(string hubUrl, Action<HttpConnectionOptions>? configureOptions = null)
    {
        var builder = new HubConnectionBuilder()
            .WithUrl(hubUrl, configureOptions ?? (_ => { }))
            .WithAutomaticReconnect();

        _connection = builder.Build();

        _connection.Closed += error =>
        {
            Console.WriteLine($"Connection closed. Error: {error?.Message}");
            return Task.CompletedTask;
        };

        _connection.Reconnecting += error =>
        {
            Console.WriteLine($"Reconnecting. Error: {error?.Message}");
            return Task.CompletedTask;
        };

        _connection.Reconnected += connectionId =>
        {
            Console.WriteLine($"Reconnected with ID: {connectionId}");
            return Task.CompletedTask;
        };

        await _connection.StartAsync();
        return this;
    }

    /// <summary>
    /// Gets the connection ID.
    /// </summary>
    public string? ConnectionId => _connection?.ConnectionId;

    /// <summary>
    /// Gets whether the connection is active.
    /// </summary>
    public bool IsConnected => _connection?.State == HubConnectionState.Connected;

    /// <summary>
    /// Gets the current connection state.
    /// </summary>
    public HubConnectionState State => _connection?.State ?? HubConnectionState.Disconnected;

    /// <summary>
    /// Registers a handler for a hub method.
    /// </summary>
    public void On<T>(string methodName, Action<T> handler)
    {
        if (_connection == null)
            throw new InvalidOperationException("Not connected. Call ConnectAsync first.");

        _connection.On<T>(methodName, message =>
        {
            _receivedMessages.Add(message!);
            
            if (!_messagesByMethod.ContainsKey(methodName))
                _messagesByMethod[methodName] = new List<object>();
            _messagesByMethod[methodName].Add(message!);

            handler(message);
        });
    }

    /// <summary>
    /// Registers an async handler for a hub method.
    /// </summary>
    public void On<T>(string methodName, Func<T, Task> handler)
    {
        if (_connection == null)
            throw new InvalidOperationException("Not connected. Call ConnectAsync first.");

        _connection.On<T>(methodName, async message =>
        {
            _receivedMessages.Add(message!);
            
            if (!_messagesByMethod.ContainsKey(methodName))
                _messagesByMethod[methodName] = new List<object>();
            _messagesByMethod[methodName].Add(message!);

            await handler(message);
        });
    }

    /// <summary>
    /// Invokes a hub method.
    /// </summary>
    public async Task InvokeAsync(string methodName, params object?[] args)
    {
        if (_connection == null)
            throw new InvalidOperationException("Not connected. Call ConnectAsync first.");

        await _connection.InvokeAsync(methodName, args);
    }

    /// <summary>
    /// Invokes a hub method and returns the result.
    /// </summary>
    public async Task<T?> InvokeAsync<T>(string methodName, params object?[] args)
    {
        if (_connection == null)
            throw new InvalidOperationException("Not connected. Call ConnectAsync first.");

        return await _connection.InvokeAsync<T>(methodName, args);
    }

    /// <summary>
    /// Sends a message to the hub (fire-and-forget).
    /// </summary>
    public async Task SendAsync(string methodName, params object?[] args)
    {
        if (_connection == null)
            throw new InvalidOperationException("Not connected. Call ConnectAsync first.");

        await _connection.SendAsync(methodName, args);
    }

    /// <summary>
    /// Gets all received messages.
    /// </summary>
    public IReadOnlyList<object> GetAllReceivedMessages() => _receivedMessages.AsReadOnly();

    /// <summary>
    /// Gets messages received for a specific method.
    /// </summary>
    public IReadOnlyList<object> GetMessagesForMethod(string methodName)
    {
        if (_messagesByMethod.TryGetValue(methodName, out var messages))
            return messages.AsReadOnly();
        return new List<object>().AsReadOnly();
    }

    /// <summary>
    /// Waits for a message to be received for a specific method.
    /// </summary>
    public async Task<bool> WaitForMessageAsync(string methodName, TimeSpan? timeout = null)
    {
        var maxWait = timeout ?? TimeSpan.FromSeconds(5);
        var startTime = DateTime.UtcNow;

        while (DateTime.UtcNow - startTime < maxWait)
        {
            if (_messagesByMethod.TryGetValue(methodName, out var messages) && messages.Count > 0)
                return true;

            await Task.Delay(100);
        }

        return false;
    }

    /// <summary>
    /// Clears all received messages.
    /// </summary>
    public void ClearMessages()
    {
        _receivedMessages.Clear();
        _messagesByMethod.Clear();
    }

    /// <summary>
    /// Disconnects from the hub.
    /// </summary>
    public async Task DisconnectAsync()
    {
        if (_connection != null)
        {
            await _connection.StopAsync();
            await _connection.DisposeAsync();
            _connection = null;
        }
    }

    /// <summary>
    /// Disposes the client.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
    }
}
