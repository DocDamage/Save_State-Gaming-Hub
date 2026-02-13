using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SaveState.Core.Common;
using SaveState.Core.RetroArch;
using System.Net.Sockets;
using System.Text;

namespace SaveState.Infrastructure.RetroArch;

/// <summary>
/// Client for communicating with RetroArch's network command interface.
/// </summary>
public class RetroArchNetworkClient
{
    private readonly ILogger<RetroArchNetworkClient> _logger;
    private readonly RetroArchOptions _options;

    public RetroArchNetworkClient(
        ILogger<RetroArchNetworkClient> logger,
        IOptions<RetroArchOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    /// <summary>
    /// Sends a command to RetroArch and returns the response.
    /// </summary>
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

            LogCommandSent(command, response);
            return Result.Success(response);
        }
        catch (SocketException ex)
        {
            LogNetworkCommandError(command, ex);
            return Result.Failure<string>($"RetroArch network command failed: {ex.Message}. Ensure RetroArch is running with --network-cmd-enable flag.");
        }
        catch (IOException ex)
        {
            LogNetworkCommandError(command, ex);
            return Result.Failure<string>($"Network I/O error: {ex.Message}");
        }
    }

    /// <summary>
    /// Checks if RetroArch is running by attempting a connection.
    /// </summary>
    public async Task<Result<bool>> IsRunningAsync(CancellationToken ct = default)
    {
        try
        {
            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(_options.NetworkCommandHost, _options.NetworkCommandPort);
            var timeoutTask = Task.Delay(1000, ct);

            if (await Task.WhenAny(connectTask, timeoutTask) == timeoutTask)
            {
                return Result.Success(false);
            }

            var isConnected = client.Connected;
            if (isConnected)
            {
                return await VerifyRetroArchAsync(client, ct);
            }

            return Result.Success(isConnected);
        }
        catch (SocketException)
        {
            return Result.Success(false);
        }
        catch (IOException)
        {
            return Result.Success(false);
        }
    }

    private async Task<Result<bool>> VerifyRetroArchAsync(TcpClient client, CancellationToken ct)
    {
        try
        {
            using var stream = client.GetStream();
            var commandBytes = Encoding.UTF8.GetBytes("VERSION\n");
            await stream.WriteAsync(commandBytes, ct);
            await stream.FlushAsync(ct);

            var buffer = new byte[256];
            var readTask = stream.ReadAsync(buffer, ct).AsTask();
            var readTimeout = Task.Delay(500, ct);

            if (await Task.WhenAny(readTask, readTimeout) == readTask)
            {
                var response = Encoding.UTF8.GetString(buffer, 0, await readTask);
                return Result.Success(!string.IsNullOrWhiteSpace(response));
            }

            return Result.Success(true);
        }
        catch
        {
            return Result.Success(true); // Connected but couldn't verify
        }
    }

    private void LogCommandSent(string command, string response)
    {
        _logger.LogDebug("Sent command '{Command}' to RetroArch, received: {Response}", command, response);
    }

    private void LogNetworkCommandError(string command, Exception ex)
    {
        _logger.LogWarning(ex, "Network command '{Command}' failed", command);
    }
}
