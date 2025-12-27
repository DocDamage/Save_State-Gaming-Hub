using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace SaveState.Core.Services.EmulatorEnhancements
{
    public class FrameSnapshot
    {
        public int FrameNumber { get; set; }
        public DateTime Timestamp { get; set; }
        public byte[] StateData { get; set; } = Array.Empty<byte>();
        public string? Bookmark { get; set; }
    }

    public class RewindSession
    {
        public string Id { get; set; } = string.Empty;
        public string GameId { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public List<FrameSnapshot> Snapshots { get; set; } = new();
        public int CurrentFrame { get; set; }
        public int MaxSnapshots { get; set; } = 1000;
    }

    public class RetroRewindService
    {
        private RewindSession? _currentSession;
        private readonly string _sessionsPath;
        private readonly int _maxSnapshots = 1000;

        public RetroRewindService()
        {
            _sessionsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), 
                "SaveState2", "data", "rewind_sessions");
            if (!Directory.Exists(_sessionsPath)) Directory.CreateDirectory(_sessionsPath);
        }

        public RewindSession StartSession(string gameId)
        {
            _currentSession = new RewindSession
            {
                Id = Guid.NewGuid().ToString(),
                GameId = gameId,
                StartTime = DateTime.Now,
                MaxSnapshots = _maxSnapshots
            };
            return _currentSession;
        }

        public void CaptureFrame(byte[] stateData)
        {
            if (_currentSession == null) return;

            var snapshot = new FrameSnapshot
            {
                FrameNumber = _currentSession.Snapshots.Count,
                Timestamp = DateTime.Now,
                StateData = stateData
            };

            _currentSession.Snapshots.Add(snapshot);
            _currentSession.CurrentFrame = snapshot.FrameNumber;

            // Trim old snapshots if over limit
            while (_currentSession.Snapshots.Count > _maxSnapshots)
            {
                _currentSession.Snapshots.RemoveAt(0);
            }
        }

        public FrameSnapshot? Rewind(int frames = 1)
        {
            if (_currentSession == null || _currentSession.Snapshots.Count == 0) return null;

            int targetFrame = Math.Max(0, _currentSession.CurrentFrame - frames);
            if (targetFrame < _currentSession.Snapshots.Count)
            {
                _currentSession.CurrentFrame = targetFrame;
                return _currentSession.Snapshots[targetFrame];
            }
            return null;
        }

        public FrameSnapshot? FastForward(int frames = 1)
        {
            if (_currentSession == null || _currentSession.Snapshots.Count == 0) return null;

            int targetFrame = Math.Min(_currentSession.Snapshots.Count - 1, _currentSession.CurrentFrame + frames);
            _currentSession.CurrentFrame = targetFrame;
            return _currentSession.Snapshots[targetFrame];
        }

        public void AddBookmark(string name)
        {
            if (_currentSession == null || _currentSession.CurrentFrame >= _currentSession.Snapshots.Count) return;
            _currentSession.Snapshots[_currentSession.CurrentFrame].Bookmark = name;
        }

        public FrameSnapshot? JumpToBookmark(string name)
        {
            if (_currentSession == null) return null;
            var snapshot = _currentSession.Snapshots.Find(s => s.Bookmark == name);
            if (snapshot != null)
            {
                _currentSession.CurrentFrame = snapshot.FrameNumber;
            }
            return snapshot;
        }

        public List<FrameSnapshot> GetBookmarks()
        {
            if (_currentSession == null) return new();
            return _currentSession.Snapshots.FindAll(s => !string.IsNullOrEmpty(s.Bookmark));
        }

        public void ExportSession(string filePath)
        {
            if (_currentSession == null) return;
            var json = JsonSerializer.Serialize(_currentSession, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }

        public RewindSession? ImportSession(string filePath)
        {
            if (!File.Exists(filePath)) return null;
            var json = File.ReadAllText(filePath);
            _currentSession = JsonSerializer.Deserialize<RewindSession>(json);
            return _currentSession;
        }

        public RewindSession? GetCurrentSession() => _currentSession;

        public void EndSession()
        {
            if (_currentSession != null)
            {
                var path = Path.Combine(_sessionsPath, $"{_currentSession.Id}.json");
                ExportSession(path);
                _currentSession = null;
            }
        }
    }
}
