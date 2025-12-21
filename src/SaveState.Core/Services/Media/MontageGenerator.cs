using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using SaveState.Core.Services.Ai;

namespace SaveState.Core.Services.Media
{
    public class MontageClip
    {
        public string SourceRecordingId { get; set; } = string.Empty;
        public string SourcePath { get; set; } = string.Empty;
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public TimeSpan Duration => EndTime - StartTime;
        public string Label { get; set; } = string.Empty;
        public int Importance { get; set; } = 5;
        public string? TransitionEffect { get; set; } // "fade", "cut", "wipe", "zoom"
    }

    public class MontageSettings
    {
        public TimeSpan TargetDuration { get; set; } = TimeSpan.FromMinutes(3);
        public TimeSpan ClipDuration { get; set; } = TimeSpan.FromSeconds(8);
        public string TransitionStyle { get; set; } = "fade";
        public bool IncludeSlowMo { get; set; } = true;
        public bool AddMusic { get; set; } = true;
        public string? MusicTrackPath { get; set; }
        public string TitleText { get; set; } = string.Empty;
        public bool ShowGameNames { get; set; } = true;
    }

    public class Montage
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string OutputPath { get; set; } = string.Empty;
        public List<MontageClip> Clips { get; set; } = new();
        public MontageSettings Settings { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public TimeSpan TotalDuration { get; set; }
        public string Status { get; set; } = "pending"; // pending, generating, complete, error
        public string? LlmNarration { get; set; }
    }

    public class MontageGenerator
    {
        private static MontageGenerator? _instance;
        private readonly string _outputPath;
        private readonly RecordingService _recordingService;
        private readonly ILlmService? _llmService;
        private readonly List<Montage> _montages = new();

        public event EventHandler<(string montageId, int percent)>? GenerationProgress;

        public static MontageGenerator Instance => _instance ??= new MontageGenerator();

        private MontageGenerator(ILlmService? llmService = null)
        {
            _llmService = llmService ?? new LlmService();
            _recordingService = RecordingService.Instance;
            _outputPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "SaveState2", "data", "montages");
            if (!Directory.Exists(_outputPath)) Directory.CreateDirectory(_outputPath);
            LoadMontages();
        }

        // Auto-generate montage from recent highlights
        public async Task<Montage> GenerateAutoMontageAsync(string title, MontageSettings? settings = null)
        {
            settings ??= new MontageSettings();
            
            var montage = new Montage
            {
                Title = title,
                Settings = settings,
                CreatedAt = DateTime.Now,
                Status = "generating"
            };

            // Gather all highlights from recordings
            var allHighlights = new List<(Recording recording, HighlightMoment highlight)>();
            
            foreach (var recording in _recordingService.GetAllRecordings())
            {
                foreach (var highlight in recording.Highlights)
                {
                    allHighlights.Add((recording, highlight));
                }
            }

            // Sort by importance and select top clips
            var selectedHighlights = allHighlights
                .OrderByDescending(h => h.highlight.Importance)
                .Take(GetMaxClipsForDuration(settings.TargetDuration, settings.ClipDuration))
                .ToList();

            // Create clips
            foreach (var (recording, highlight) in selectedHighlights)
            {
                var clip = new MontageClip
                {
                    SourceRecordingId = recording.Id,
                    SourcePath = recording.FilePath,
                    StartTime = highlight.Timestamp - TimeSpan.FromSeconds(2), // 2s before
                    EndTime = highlight.Timestamp + settings.ClipDuration - TimeSpan.FromSeconds(2),
                    Label = highlight.Description,
                    Importance = highlight.Importance,
                    TransitionEffect = settings.TransitionStyle
                };

                // Clamp to valid range
                if (clip.StartTime < TimeSpan.Zero) clip.StartTime = TimeSpan.Zero;
                if (clip.EndTime > recording.Duration) clip.EndTime = recording.Duration;

                montage.Clips.Add(clip);
            }

            montage.TotalDuration = TimeSpan.FromTicks(montage.Clips.Sum(c => c.Duration.Ticks));

            // Generate AI narration/description
            if (_llmService?.IsAvailable == true)
            {
                montage.LlmNarration = await GenerateNarrationAsync(montage);
                montage.Description = await GenerateDescriptionAsync(montage);
            }

            // Generate the actual video (in production, use FFmpeg)
            await GenerateVideoAsync(montage);

            montage.Status = "complete";
            _montages.Add(montage);
            SaveMontages();

            return montage;
        }

        // Generate montage from specific recordings
        public async Task<Montage> GenerateMontageFromRecordingsAsync(
            List<string> recordingIds, 
            string title, 
            MontageSettings? settings = null)
        {
            settings ??= new MontageSettings();
            
            var montage = new Montage
            {
                Title = title,
                Settings = settings,
                CreatedAt = DateTime.Now,
                Status = "generating"
            };

            foreach (var recordingId in recordingIds)
            {
                var recording = _recordingService.GetRecording(recordingId);
                if (recording == null) continue;

                // Get highlights or create evenly spaced clips
                if (recording.Highlights.Count > 0)
                {
                    foreach (var highlight in recording.Highlights.Take(5))
                    {
                        montage.Clips.Add(new MontageClip
                        {
                            SourceRecordingId = recording.Id,
                            SourcePath = recording.FilePath,
                            StartTime = highlight.Timestamp,
                            EndTime = highlight.Timestamp + settings.ClipDuration,
                            Label = highlight.Description,
                            Importance = highlight.Importance
                        });
                    }
                }
                else
                {
                    // Create clips at intervals
                    var interval = recording.Duration.TotalSeconds / 5;
                    for (int i = 0; i < 5; i++)
                    {
                        var start = TimeSpan.FromSeconds(i * interval);
                        montage.Clips.Add(new MontageClip
                        {
                            SourceRecordingId = recording.Id,
                            SourcePath = recording.FilePath,
                            StartTime = start,
                            EndTime = start + settings.ClipDuration,
                            Label = $"Clip {i + 1}"
                        });
                    }
                }
            }

            montage.TotalDuration = TimeSpan.FromTicks(montage.Clips.Sum(c => c.Duration.Ticks));

            if (_llmService?.IsAvailable == true)
            {
                montage.LlmNarration = await GenerateNarrationAsync(montage);
            }

            await GenerateVideoAsync(montage);

            montage.Status = "complete";
            _montages.Add(montage);
            SaveMontages();

            return montage;
        }

        // Generate montage from a single game's recordings
        public async Task<Montage> GenerateGameMontageAsync(string gameId, string gameTitle)
        {
            var recordings = _recordingService.GetRecordingsForGame(gameId);
            var recordingIds = recordings.Select(r => r.Id).ToList();

            return await GenerateMontageFromRecordingsAsync(
                recordingIds, 
                $"{gameTitle} Highlights", 
                new MontageSettings { TitleText = gameTitle });
        }

        private async Task<string> GenerateNarrationAsync(Montage montage)
        {
            if (_llmService?.IsAvailable != true) return "";

            var clipDescriptions = string.Join("\n", montage.Clips.Select((c, i) => 
                $"Clip {i + 1}: {c.Label} (importance: {c.Importance}/10)"));

            var prompt = $@"Create exciting narration for a gaming montage titled '{montage.Title}'.
The montage contains these highlights:
{clipDescriptions}

Write a brief, hype narration script (max 100 words) that could be used as voiceover.
Make it exciting and match the gaming highlight reel style.";

            return await _llmService.CompleteAsync(prompt, 
                "You are an esports commentator creating exciting highlight reel narration.");
        }

        private async Task<string> GenerateDescriptionAsync(Montage montage)
        {
            if (_llmService?.IsAvailable != true) return "";

            var prompt = $@"Write a short description (2-3 sentences) for a gaming highlight video titled '{montage.Title}' with {montage.Clips.Count} clips totaling {montage.TotalDuration.TotalSeconds:F0} seconds.";

            return await _llmService.CompleteAsync(prompt, 
                "You are a gaming content creator writing video descriptions.");
        }

        private async Task GenerateVideoAsync(Montage montage)
        {
            // In production, this would use FFmpeg to:
            // 1. Extract clips from source recordings
            // 2. Apply transitions
            // 3. Add music track
            // 4. Overlay titles/labels
            // 5. Export final video

            montage.OutputPath = Path.Combine(_outputPath, $"montage_{montage.Id}.mp4");

            // Simulate generation progress
            for (int i = 0; i <= 100; i += 10)
            {
                GenerationProgress?.Invoke(this, (montage.Id, i));
                await Task.Delay(100);
            }

            Console.WriteLine($"🎬 Montage generated: {montage.Title} ({montage.Clips.Count} clips)");
        }

        private int GetMaxClipsForDuration(TimeSpan targetDuration, TimeSpan clipDuration)
        {
            return (int)(targetDuration.TotalSeconds / clipDuration.TotalSeconds);
        }

        public List<Montage> GetAllMontages() => _montages.OrderByDescending(m => m.CreatedAt).ToList();

        public Montage? GetMontage(string id) => _montages.FirstOrDefault(m => m.Id == id);

        public bool DeleteMontage(string id)
        {
            var montage = _montages.FirstOrDefault(m => m.Id == id);
            if (montage == null) return false;

            try
            {
                if (File.Exists(montage.OutputPath))
                    File.Delete(montage.OutputPath);
                
                _montages.Remove(montage);
                SaveMontages();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void LoadMontages()
        {
            var listPath = Path.Combine(_outputPath, "montages.json");
            if (File.Exists(listPath))
            {
                try
                {
                    var json = File.ReadAllText(listPath);
                    var list = JsonSerializer.Deserialize<List<Montage>>(json);
                    if (list != null)
                    {
                        _montages.Clear();
                        _montages.AddRange(list);
                    }
                }
                catch { }
            }
        }

        private void SaveMontages()
        {
            var listPath = Path.Combine(_outputPath, "montages.json");
            var json = JsonSerializer.Serialize(_montages, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(listPath, json);
        }

        public string GetOutputPath() => _outputPath;

        // Suggested transitions for different highlight types
        public string SuggestTransition(string highlightType)
        {
            return highlightType.ToLower() switch
            {
                "boss_kill" => "zoom",
                "death" => "fade",
                "achievement" => "wipe",
                "skill" => "cut",
                "funny" => "fade",
                _ => "fade"
            };
        }
    }
}
