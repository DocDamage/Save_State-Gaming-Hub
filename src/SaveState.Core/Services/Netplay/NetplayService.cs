using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SaveState.Core.Services.Netplay
{
    public enum NetplayRole
    {
        Host,
        Client,
        Spectator
    }

    public enum NetplayState
    {
        Disconnected,
        Connecting,
        WaitingForPlayers,
        Synchronizing,
        Playing,
        Paused,
        Error
    }

    public class NetplayPlayer
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public NetplayRole Role { get; set; }
        public int PlayerNumber { get; set; }
        public int Latency { get; set; } // ms
        public bool IsReady { get; set; }
        public DateTime JoinedAt { get; set; }
    }

    public class NetplaySession
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Code { get; set; } = string.Empty; // Join code (e.g., "ABCD-1234")
        public string GameId { get; set; } = string.Empty;
        public string GameName { get; set; } = string.Empty;
        public string Platform { get; set; } = string.Empty;
        public string HostId { get; set; } = string.Empty;
        public List<NetplayPlayer> Players { get; set; } = new();
        public int MaxPlayers { get; set; } = 4;
        public NetplayState State { get; set; } = NetplayState.Disconnected;
        public bool AllowSpectators { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public int InputDelay { get; set; } = 0; // Frames of input delay
    }

    public class NetplayMessage
    {
        public string Type { get; set; } = string.Empty; // "input", "state", "chat", "sync"
        public string PlayerId { get; set; } = string.Empty;
        public int Frame { get; set; }
        public byte[]? Data { get; set; }
        public long Timestamp { get; set; }
    }

    public class NetplayService : IDisposable
    {
        private static NetplayService? _instance;
        private TcpListener? _listener;
        private TcpClient? _client;
        private NetworkStream? _stream;
        private CancellationTokenSource? _cts;
        private NetplaySession? _currentSession;
        private NetplayPlayer? _localPlayer;
        private readonly int _defaultPort = 55435;

        public event EventHandler<NetplayState>? StateChanged;
        public event EventHandler<NetplayPlayer>? PlayerJoined;
        public event EventHandler<NetplayPlayer>? PlayerLeft;
        public event EventHandler<string>? ChatMessageReceived;
        public event EventHandler<byte[]>? InputReceived;
        public event EventHandler<int>? LatencyUpdated;

        public static NetplayService Instance => _instance ??= new NetplayService();
        public NetplaySession? CurrentSession => _currentSession;
        public NetplayPlayer? LocalPlayer => _localPlayer;
        public bool IsConnected => _currentSession?.State == NetplayState.Playing;
        public bool IsHost => _localPlayer?.Role == NetplayRole.Host;

        private NetplayService() { }

        // Host a new game session
        public async Task<NetplaySession?> HostSessionAsync(string playerName, string gameId, 
            string gameName, string platform, int maxPlayers = 4)
        {
            try
            {
                _cts = new CancellationTokenSource();

                _currentSession = new NetplaySession
                {
                    Code = GenerateSessionCode(),
                    GameId = gameId,
                    GameName = gameName,
                    Platform = platform,
                    MaxPlayers = maxPlayers,
                    CreatedAt = DateTime.UtcNow,
                    State = NetplayState.WaitingForPlayers
                };

                _localPlayer = new NetplayPlayer
                {
                    Name = playerName,
                    Role = NetplayRole.Host,
                    PlayerNumber = 1,
                    IsReady = true,
                    JoinedAt = DateTime.UtcNow
                };

                _currentSession.HostId = _localPlayer.Id;
                _currentSession.Players.Add(_localPlayer);

                // Start listening for connections
                _listener = new TcpListener(IPAddress.Any, _defaultPort);
                _listener.Start();

                SetState(NetplayState.WaitingForPlayers);

                // Start accept loop
                _ = AcceptClientsAsync(_cts.Token);

                Console.WriteLine($"🎮 Hosting netplay session: {_currentSession.Code}");
                return _currentSession;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Host error: {ex.Message}");
                SetState(NetplayState.Error);
                return null;
            }
        }

        // Join an existing session
        public async Task<bool> JoinSessionAsync(string playerName, string hostAddress, int port = 0)
        {
            try
            {
                if (port == 0) port = _defaultPort;

                _cts = new CancellationTokenSource();
                _client = new TcpClient();
                
                SetState(NetplayState.Connecting);
                await _client.ConnectAsync(hostAddress, port);
                _stream = _client.GetStream();

                _localPlayer = new NetplayPlayer
                {
                    Name = playerName,
                    Role = NetplayRole.Client,
                    JoinedAt = DateTime.UtcNow
                };

                // Send join request
                await SendMessageAsync(new NetplayMessage
                {
                    Type = "join",
                    PlayerId = _localPlayer.Id,
                    Data = Encoding.UTF8.GetBytes(playerName)
                });

                // Start receive loop
                _ = ReceiveMessagesAsync(_cts.Token);

                SetState(NetplayState.Synchronizing);
                Console.WriteLine($"🎮 Joined netplay session");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Join error: {ex.Message}");
                SetState(NetplayState.Error);
                return false;
            }
        }

        // Join by session code (would need a relay server in production)
        public async Task<bool> JoinByCodeAsync(string playerName, string sessionCode)
        {
            // In production: Query relay server for host address
            // For now, this is a placeholder
            Console.WriteLine($"Looking up session: {sessionCode}");
            await Task.Delay(100);
            return false; // Would return JoinSessionAsync result
        }

        // Send input to other players
        public async Task SendInputAsync(int frame, byte[] inputData)
        {
            if (_stream == null || !IsConnected) return;

            await SendMessageAsync(new NetplayMessage
            {
                Type = "input",
                PlayerId = _localPlayer?.Id ?? "",
                Frame = frame,
                Data = inputData,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            });
        }

        // Send chat message
        public async Task SendChatAsync(string message)
        {
            await SendMessageAsync(new NetplayMessage
            {
                Type = "chat",
                PlayerId = _localPlayer?.Id ?? "",
                Data = Encoding.UTF8.GetBytes(message)
            });
        }

        // Send save state for sync
        public async Task SendSaveStateAsync(byte[] stateData)
        {
            await SendMessageAsync(new NetplayMessage
            {
                Type = "state",
                PlayerId = _localPlayer?.Id ?? "",
                Data = stateData
            });
        }

        // Signal ready to play
        public void SetReady(bool ready)
        {
            if (_localPlayer != null)
            {
                _localPlayer.IsReady = ready;
                _ = SendMessageAsync(new NetplayMessage
                {
                    Type = "ready",
                    PlayerId = _localPlayer.Id,
                    Data = BitConverter.GetBytes(ready)
                });
            }
        }

        // Start the game (host only)
        public async Task StartGameAsync()
        {
            if (!IsHost || _currentSession == null) return;

            if (_currentSession.Players.All(p => p.IsReady || p.Role == NetplayRole.Spectator))
            {
                await SendMessageAsync(new NetplayMessage { Type = "start" });
                SetState(NetplayState.Playing);
            }
        }

        // Disconnect from session
        public void Disconnect()
        {
            _cts?.Cancel();
            _stream?.Close();
            _client?.Close();
            _listener?.Stop();

            _currentSession = null;
            _localPlayer = null;

            SetState(NetplayState.Disconnected);
            Console.WriteLine("🎮 Disconnected from netplay");
        }

        // Update latency measurement
        public async Task<int> MeasureLatencyAsync()
        {
            if (_stream == null) return -1;

            var start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            await SendMessageAsync(new NetplayMessage { Type = "ping", Timestamp = start });
            
            // Response would come through receive loop
            // Placeholder - actual implementation tracks response
            return 0;
        }

        private async Task AcceptClientsAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested && _listener != null)
            {
                try
                {
                    var client = await _listener.AcceptTcpClientAsync(ct);
                    _ = HandleClientAsync(client, ct);
                }
                catch (OperationCanceledException) { break; }
                catch { }
            }
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken ct)
        {
            var stream = client.GetStream();
            // Handle client messages...
            await Task.Delay(100, ct);
        }

        private async Task ReceiveMessagesAsync(CancellationToken ct)
        {
            var buffer = new byte[65536];
            while (!ct.IsCancellationRequested && _stream != null)
            {
                try
                {
                    var bytesRead = await _stream.ReadAsync(buffer, ct);
                    if (bytesRead > 0)
                    {
                        var json = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        var message = JsonSerializer.Deserialize<NetplayMessage>(json);
                        if (message != null) HandleMessage(message);
                    }
                }
                catch (OperationCanceledException) { break; }
                catch { }
            }
        }

        private void HandleMessage(NetplayMessage message)
        {
            switch (message.Type)
            {
                case "input":
                    if (message.Data != null) InputReceived?.Invoke(this, message.Data);
                    break;
                case "chat":
                    if (message.Data != null)
                        ChatMessageReceived?.Invoke(this, Encoding.UTF8.GetString(message.Data));
                    break;
                case "pong":
                    var latency = (int)(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - message.Timestamp);
                    LatencyUpdated?.Invoke(this, latency);
                    break;
            }
        }

        private async Task SendMessageAsync(NetplayMessage message)
        {
            if (_stream == null) return;
            var json = JsonSerializer.Serialize(message);
            var data = Encoding.UTF8.GetBytes(json);
            await _stream.WriteAsync(data);
        }

        private void SetState(NetplayState state)
        {
            if (_currentSession != null) _currentSession.State = state;
            StateChanged?.Invoke(this, state);
        }

        private string GenerateSessionCode()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var random = new Random();
            var code = new char[8];
            for (int i = 0; i < 8; i++)
            {
                code[i] = chars[random.Next(chars.Length)];
                if (i == 3) code[i] = '-'; // Format: XXXX-XXXX
            }
            return new string(code).Insert(4, "-");
        }

        public void Dispose()
        {
            Disconnect();
            _cts?.Dispose();
        }
    }
}
