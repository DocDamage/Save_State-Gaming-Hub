using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SaveState.Core.Common;
using SaveState.Core.RetroArch;
using System.Net.Sockets;
using System.Text;

namespace SaveState.Infrastructure.RetroArch.Services.RetroArch;

/// <summary>
/// Engine for sending network commands to RetroArch.
/// </summary>
public partial class NetworkCommandEngine : INetworkCommandEngine
{
    private readonly ILogger<NetworkCommandEngine> _logger;
    private readonly RetroArchOptions _options;

    public NetworkCommandEngine(
        ILogger<NetworkCommandEngine> logger,
        IOptions<RetroArchOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async Task<Result<string>> SendCommandAsync(string command, CancellationToken ct = default)
    {
        if (!_options.NetworkCommandEnabled)
        {
            return Result.Failure<string>("RetroArch network command interface is not enabled");
        }

        try
        {
            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(_options.NetworkCommandHost, _options.NetworkCommandPort);
            var timeoutTask = Task.Delay(_options.NetworkCommandTimeout, ct);

            if (await Task.WhenAny(connectTask, timeoutTask) == timeoutTask)
            {
                return Result.Failure<string>($"Connection to RetroArch timed out after {_options.NetworkCommandTimeout}ms");
            }

            if (!client.Connected)
            {
                return Result.Failure<string>("Failed to connect to RetroArch network command interface");
            }

            using var stream = client.GetStream();
            var commandBytes = Encoding.UTF8.GetBytes(command + "\n");
            await stream.WriteAsync(commandBytes, ct);
            await stream.FlushAsync(ct);

            // Read response
            var buffer = new byte[1024];
            var bytesRead = await stream.ReadAsync(buffer, ct);
            var response = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

            LogCommandSent(_logger, command, response);
            return Result.Success(response);
        }
        catch (SocketException ex)
        {
            LogNetworkCommandError(_logger, command, ex);
            return Result.Failure<string>($"RetroArch network command failed: {ex.Message}. Ensure RetroArch is running with --network-cmd-enable flag.");
        }
        catch (Exception ex)
        {
            LogNetworkCommandError(_logger, command, ex);
            return Result.Failure<string>($"Error sending command to RetroArch: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<bool>> IsRunningAsync(CancellationToken ct = default)
    {
        if (!_options.NetworkCommandEnabled)
        {
            return Result.Success(false);
        }

        try
        {
            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(_options.NetworkCommandHost, _options.NetworkCommandPort);
            var timeoutTask = Task.Delay(1000, ct); // Short timeout for checking

            if (await Task.WhenAny(connectTask, timeoutTask) == timeoutTask)
            {
                return Result.Success(false);
            }

            var isConnected = client.Connected;
            if (isConnected)
            {
                // Send a simple VERSION command to verify it's actually RetroArch
                using var stream = client.GetStream();
                var commandBytes = Encoding.UTF8.GetBytes("VERSION\n");
                await stream.WriteAsync(commandBytes, ct);
                await stream.FlushAsync(ct);

                // Try to read response
                var buffer = new byte[256];
                var readTask = stream.ReadAsync(buffer, ct).AsTask();
                var readTimeout = Task.Delay(500, ct);

                if (await Task.WhenAny(readTask, readTimeout) == readTask)
                {
                    var response = Encoding.UTF8.GetString(buffer, 0, await readTask);
                    // If we got any response, RetroArch is running
                    return Result.Success(!string.IsNullOrWhiteSpace(response));
                }
            }

            return Result.Success(isConnected);
        }
        catch (SocketException)
        {
            // Connection failed - RetroArch is not running
            return Result.Success(false);
        }
        catch (TaskCanceledException)
        {
            // Request timed out - RetroArch is not responding
            return Result.Success(false);
        }
        catch (Exception ex)
        {
            LogIsRunningError(_logger, ex);
            return Result.Success(false);
        }
    }

    /// <inheritdoc />
    public async Task<Result<string>> GetVersionAsync(CancellationToken ct = default)
    {
        var result = await SendCommandAsync("VERSION", ct);
        if (result.IsSuccess && !string.IsNullOrWhiteSpace(result.Value))
        {
            return Result.Success(result.Value.Trim());
        }
        return Result.Failure<string>("Failed to get RetroArch version");
    }

    /// <inheritdoc />
    public async Task<Result> PauseAsync(CancellationToken ct = default)
    {
        var result = await SendCommandAsync("PAUSE_TOGGLE", ct);
        return result.IsSuccess ? Result.Success() : Result.Failure(result.Error ?? "Failed to pause");
    }

    /// <inheritdoc />
    public async Task<Result> ResumeAsync(CancellationToken ct = default)
    {
        var result = await SendCommandAsync("PAUSE_TOGGLE", ct);
        return result.IsSuccess ? Result.Success() : Result.Failure(result.Error ?? "Failed to resume");
    }

    /// <inheritdoc />
    public async Task<Result> ResetAsync(CancellationToken ct = default)
    {
        var result = await SendCommandAsync("RESET", ct);
        return result.IsSuccess ? Result.Success() : Result.Failure(result.Error ?? "Failed to reset");
    }

    /// <inheritdoc />
    public async Task<Result> ToggleMenuAsync(CancellationToken ct = default)
    {
        var result = await SendCommandAsync("MENU_TOGGLE", ct);
        return result.IsSuccess ? Result.Success() : Result.Failure(result.Error ?? "Failed to toggle menu");
    }

    /// <inheritdoc />
    public async Task<Result> QuitAsync(CancellationToken ct = default)
    {
        var result = await SendCommandAsync("QUIT", ct);
        return result.IsSuccess ? Result.Success() : Result.Failure(result.Error ?? "Failed to quit");
    }

    #region Logging

    [LoggerMessage(EventId = 401, Level = LogLevel.Debug, Message = "Sent command to RetroArch: {Command}, Response: {Response}")]
    static partial void LogCommandSent(ILogger logger, string command, string response);

    [LoggerMessage(EventId = 402, Level = LogLevel.Error, Message = "Network command error for command: {Command}")]
    static partial void LogNetworkCommandError(ILogger logger, string command, Exception ex);

    [LoggerMessage(EventId = 403, Level = LogLevel.Debug, Message = "Error checking if RetroArch is running")]
    static partial void LogIsRunningError(ILogger logger, Exception ex);

    #endregion
}
