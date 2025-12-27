using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using SaveState.Core.Services.Ai;
using Serilog;

namespace SaveState.Core.Services.Media
{
    public enum RecordingStatus
    {
        Idle,
        Recording,
        Paused,
        Processing,
        Complete,
        Error
    }

    public class Recording
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string FilePath { get; set; } = string.Empty;
        public string GameId { get; set; } = string.Empty;
        public string GameName { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public TimeSpan Duration { get; set; }
        public long FileSizeBytes { get; set; }
        public List<HighlightMoment> Highlights { get; set; } = new();
        public string? Title { get; set; }
        public string? Description { get; set; }
    }

    public class HighlightMoment
    {
        public TimeSpan Timestamp { get; set; }
        public string Type { get; set; } = string.Empty; // "kill", "death", "achievement", "funny", "skill"
        public string Description { get; set; } = string.Empty;
        public int Importance { get; set; } = 5; // 1-10
    }

    public class RecordingService
    {
        private static RecordingService? _instance;
        private readonly ILogger _logger = Log.ForContext<RecordingService>();
        private readonly string _recordingsPath;
        private readonly List<Recording> _recordings = new();
        private Recording? _currentRecording;
        private RecordingStatus _status = RecordingStatus.Idle;
        private DateTime _recordingStartTime;

        public static RecordingService Instance => _instance ??= new RecordingService();
        public RecordingStatus Status => _status;
        public bool IsRecording => _status == RecordingStatus.Recording;

        public event EventHandler<RecordingStatus>? StatusChanged;
        public event EventHandler<HighlightMoment>? HighlightDetected;

        private RecordingService()
        {
            _recordingsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "SaveState2", "data", "recordings");
            if (!Directory.Exists(_recordingsPath)) Directory.CreateDirectory(_recordingsPath);
            LoadRecordings();
        }

        public Recording StartRecording(string gameId, string gameName)
        {
            if (_status == RecordingStatus.Recording)
            {
                StopRecording();
            }

            _recordingStartTime = DateTime.Now;
            var fileName = $"recording_{gameId}_{_recordingStartTime:yyyyMMdd_HHmmss}.mp4";
            
            _currentRecording = new Recording
            {
                FilePath = Path.Combine(_recordingsPath, fileName),
                GameId = gameId,
                GameName = gameName,
                StartedAt = _recordingStartTime
            };

            _status = RecordingStatus.Recording;
            StatusChanged?.Invoke(this, _status);

            _logger.Information("Started recording: {GameName}", gameName);

            // In production: Start actual video capture using FFmpeg or similar
            return _currentRecording;
        }

        public Recording? StopRecording()
        {
            if (_currentRecording == null || _status != RecordingStatus.Recording)
                return null;

            _currentRecording.EndedAt = DateTime.Now;
            _currentRecording.Duration = DateTime.Now - _recordingStartTime;
            
            _status = RecordingStatus.Processing;
            StatusChanged?.Invoke(this, _status);

            _logger.Information("Stopped recording: {Duration:F1}s", _currentRecording.Duration.TotalSeconds);

            // In production: Finalize video file
            _recordings.Add(_currentRecording);
            SaveRecordingMetadata(_currentRecording);

            _status = RecordingStatus.Complete;
            StatusChanged?.Invoke(this, _status);

            var completed = _currentRecording;
            _currentRecording = null;
            
            _status = RecordingStatus.Idle;
            return completed;
        }

        public void PauseRecording()
        {
            if (_status == RecordingStatus.Recording)
            {
                _status = RecordingStatus.Paused;
                StatusChanged?.Invoke(this, _status);
            }
        }

        public void ResumeRecording()
        {
            if (_status == RecordingStatus.Paused)
            {
                _status = RecordingStatus.Recording;
                StatusChanged?.Invoke(this, _status);
            }
        }

        public void AddHighlight(string type, string description, int importance = 7)
        {
            if (_currentRecording == null) return;

            var highlight = new HighlightMoment
            {
                Timestamp = DateTime.Now - _recordingStartTime,
                Type = type,
                Description = description,
                Importance = importance
            };

            _currentRecording.Highlights.Add(highlight);
            HighlightDetected?.Invoke(this, highlight);

            _logger.Debug("Highlight: {Type} at {Timestamp:F1}s", type, highlight.Timestamp.TotalSeconds);
        }

        public void MarkBossKill() => AddHighlight("boss_kill", "Boss defeated!", 10);
        public void MarkDeath() => AddHighlight("death", "Player died", 5);
        public void MarkAchievement(string name) => AddHighlight("achievement", name, 8);
        public void MarkFunnyMoment() => AddHighlight("funny", "Funny moment", 6);
        public void MarkSkillPlay() => AddHighlight("skill", "Skillful play", 7);

        public List<Recording> GetAllRecordings() => _recordings.OrderByDescending(r => r.StartedAt).ToList();

        public List<Recording> GetRecordingsForGame(string gameId) =>
            _recordings.Where(r => r.GameId == gameId).OrderByDescending(r => r.StartedAt).ToList();

        public Recording? GetRecording(string id) => _recordings.FirstOrDefault(r => r.Id == id);

        public bool DeleteRecording(string id)
        {
            var recording = _recordings.FirstOrDefault(r => r.Id == id);
            if (recording == null) return false;

            try
            {
                if (File.Exists(recording.FilePath))
                    File.Delete(recording.FilePath);
                
                var metaPath = Path.ChangeExtension(recording.FilePath, ".json");
                if (File.Exists(metaPath))
                    File.Delete(metaPath);

                _recordings.Remove(recording);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public TimeSpan GetTotalRecordingTime()
        {
            return TimeSpan.FromSeconds(_recordings.Sum(r => r.Duration.TotalSeconds));
        }

        public long GetTotalStorageUsed()
        {
            return _recordings.Sum(r => r.FileSizeBytes);
        }

        public string GetRecordingsPath() => _recordingsPath;

        private void SaveRecordingMetadata(Recording recording)
        {
            var metaPath = Path.ChangeExtension(recording.FilePath, ".json");
            var json = JsonSerializer.Serialize(recording, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(metaPath, json);
        }

        private void LoadRecordings()
        {
            if (!Directory.Exists(_recordingsPath)) return;

            foreach (var metaFile in Directory.GetFiles(_recordingsPath, "*.json"))
            {
                try
                {
                    var json = File.ReadAllText(metaFile);
                    var recording = JsonSerializer.Deserialize<Recording>(json);
                    if (recording != null)
                    {
                        _recordings.Add(recording);
                    }
                }
                catch (Exception ex) { _logger.Warning(ex, "Failed to load recording metadata"); }
            }
        }
    }
}
