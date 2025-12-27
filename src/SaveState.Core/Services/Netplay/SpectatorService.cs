using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SaveState.Core.Services.Netplay
{
    public class SpectatorInfo
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public DateTime JoinedAt { get; set; }
        public int BufferDelay { get; set; } = 30; // Frames behind live
    }

    public class StreamFrame
    {
        public int FrameNumber { get; set; }
        public byte[] VideoData { get; set; } = Array.Empty<byte>();
        public byte[] AudioData { get; set; } = Array.Empty<byte>();
        public byte[]? InputData { get; set; }
        public long Timestamp { get; set; }
    }

    public class SpectatorSession
    {
        public string SessionId { get; set; } = string.Empty;
        public string GameName { get; set; } = string.Empty;
        public string HostName { get; set; } = string.Empty;
        public int ViewerCount { get; set; }
        public bool IsLive { get; set; }
        public DateTime StartedAt { get; set; }
        public TimeSpan Duration => DateTime.UtcNow - StartedAt;
    }

    public class SpectatorService : IDisposable
    {
        private static SpectatorService? _instance;
        private readonly NetplayService _netplayService;
        private readonly Queue<StreamFrame> _frameBuffer = new();
        private readonly List<SpectatorInfo> _spectators = new();
        private CancellationTokenSource? _cts;
        private bool _isStreaming;
        private bool _isWatching;
        private int _bufferDelayFrames = 30;
        private const int MaxBufferFrames = 300; // 5 seconds at 60fps

#pragma warning disable CS0067 // Event is defined for future use
        public event EventHandler<StreamFrame>? FrameReceived;
#pragma warning restore CS0067
        public event EventHandler<SpectatorInfo>? SpectatorJoined;
        public event EventHandler<SpectatorInfo>? SpectatorLeft;
        public event EventHandler<int>? ViewerCountChanged;

        public static SpectatorService Instance => _instance ??= new SpectatorService();
        public bool IsStreaming => _isStreaming;
        public bool IsWatching => _isWatching;
        public int ViewerCount => _spectators.Count;
        public int BufferDelay => _bufferDelayFrames;
        public IReadOnlyList<SpectatorInfo> Spectators => _spectators;

        private SpectatorService()
        {
            _netplayService = NetplayService.Instance;
        }

        // Start streaming gameplay for spectators
        public void StartStreaming()
        {
            if (_isStreaming) return;

            _cts = new CancellationTokenSource();
            _isStreaming = true;
            _frameBuffer.Clear();

            Console.WriteLine("📺 Started spectator stream");
        }

        // Stop streaming
        public void StopStreaming()
        {
            _isStreaming = false;
            _cts?.Cancel();
            _frameBuffer.Clear();
            _spectators.Clear();

            Console.WriteLine("📺 Stopped spectator stream");
        }

        // Add a frame to the stream
        public void PushFrame(int frameNumber, byte[] videoData, byte[]? audioData = null, byte[]? inputData = null)
        {
            if (!_isStreaming) return;

            var frame = new StreamFrame
            {
                FrameNumber = frameNumber,
                VideoData = videoData,
                AudioData = audioData ?? Array.Empty<byte>(),
                InputData = inputData,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            _frameBuffer.Enqueue(frame);

            // Trim buffer if too large
            while (_frameBuffer.Count > MaxBufferFrames)
            {
                _frameBuffer.Dequeue();
            }

            // Broadcast to spectators (in production: use actual network)
            BroadcastFrame(frame);
        }

        private void BroadcastFrame(StreamFrame frame)
        {
            // In production: Send to all connected spectators
            // This would use UDP for low latency
        }

        // Join as spectator
        public Task<bool> StartWatchingAsync(string hostAddress, int port = 55436)
        {
            try
            {
                _cts = new CancellationTokenSource();
                _isWatching = true;
                _frameBuffer.Clear();

                // In production: Connect to stream server
                Console.WriteLine($"📺 Connecting to spectator stream at {hostAddress}:{port}");

                // Start receive loop
                _ = ReceiveFramesAsync(_cts.Token);

                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Watch error: {ex.Message}");
                _isWatching = false;
                return Task.FromResult(false);
            }
        }

        // Stop watching
        public void StopWatching()
        {
            _isWatching = false;
            _cts?.Cancel();
            _frameBuffer.Clear();

            Console.WriteLine("📺 Stopped watching stream");
        }

        // Get next frame from buffer (with delay)
        public StreamFrame? GetNextFrame()
        {
            if (_frameBuffer.Count < _bufferDelayFrames)
                return null; // Need more buffer

            return _frameBuffer.Dequeue();
        }

        // Set buffer delay
        public void SetBufferDelay(int frames)
        {
            _bufferDelayFrames = Math.Clamp(frames, 10, 120);
        }

        private async Task ReceiveFramesAsync(CancellationToken ct)
        {
            // In production: Receive frames from network stream
            while (!ct.IsCancellationRequested && _isWatching)
            {
                try
                {
                    await Task.Delay(16, ct); // ~60fps
                    
                    // Simulated frame receive
                    // In production: Read from network socket
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Operation failed: {ex.Message}"); }
            }
        }

        // Get available streams to watch
        public List<SpectatorSession> GetAvailableSessions()
        {
            // In production: Query matchmaking/relay server
            return new List<SpectatorSession>();
        }

        // Get stream stats
        public (int bufferSize, int viewerCount, long latencyMs) GetStreamStats()
        {
            var latency = _frameBuffer.Count > 0 
                ? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - _frameBuffer.Peek().Timestamp
                : 0;

            return (_frameBuffer.Count, _spectators.Count, latency);
        }

        // Record stream for replay
        public async Task StartRecordingAsync(string outputPath)
        {
            // In production: Write frames to file for later playback
            await Task.Yield();
            Console.WriteLine($"📺 Recording stream to: {outputPath}");
        }

        public void StopRecording()
        {
            Console.WriteLine("📺 Stopped recording stream");
        }

        // Handle spectator connection
        internal void AddSpectator(SpectatorInfo spectator)
        {
            _spectators.Add(spectator);
            SpectatorJoined?.Invoke(this, spectator);
            ViewerCountChanged?.Invoke(this, _spectators.Count);
        }

        internal void RemoveSpectator(string spectatorId)
        {
            var spectator = _spectators.FirstOrDefault(s => s.Id == spectatorId);
            if (spectator != null)
            {
                _spectators.Remove(spectator);
                SpectatorLeft?.Invoke(this, spectator);
                ViewerCountChanged?.Invoke(this, _spectators.Count);
            }
        }

        // Kick a spectator (host only)
        public void KickSpectator(string spectatorId)
        {
            RemoveSpectator(spectatorId);
            // In production: Send disconnect message to spectator
        }

        public void Dispose()
        {
            StopStreaming();
            StopWatching();
            _cts?.Dispose();
        }
    }
}
