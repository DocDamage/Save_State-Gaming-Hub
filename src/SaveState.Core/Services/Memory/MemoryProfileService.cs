using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using System.Text.Json;
using System.IO;
using System.Linq;
using Serilog;

namespace SaveState.Core.Services.Memory
{
    public interface IMemoryProfileService
    {
        Task<GameMemoryProfile?> GetProfileAsync(Guid gameId);
        Task SaveProfileAsync(GameMemoryProfile profile);
    }

    public class MemoryProfileService : IMemoryProfileService
    {
        private readonly ILogger _logger = Log.ForContext<MemoryProfileService>();
        private readonly Dictionary<Guid, GameMemoryProfile> _profiles = new();
        private readonly string _storagePath;

        public MemoryProfileService()
        {
            _storagePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "SaveState2", "data", "memory_profiles.json");
            LoadProfiles();
        }

        private void LoadProfiles()
        {
            var dir = Path.GetDirectoryName(_storagePath);
            if (dir != null && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

            if (File.Exists(_storagePath))
            {
                try
                {
                    var json = File.ReadAllText(_storagePath);
                    var list = JsonSerializer.Deserialize<List<GameMemoryProfile>>(json);
                    if (list != null)
                    {
                        foreach (var profile in list)
                        {
                            _profiles[profile.GameId] = profile;
                        }
                    }
                    _logger.Information("Loaded {Count} memory profiles", _profiles.Count);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Failed to load memory profiles from {Path}", _storagePath);
                }
            }
        }

        public Task<GameMemoryProfile?> GetProfileAsync(Guid gameId)
        {
            if (_profiles.TryGetValue(gameId, out var profile))
            {
                return Task.FromResult<GameMemoryProfile?>(profile);
            }
            return Task.FromResult<GameMemoryProfile?>(null);
        }

        public async Task SaveProfileAsync(GameMemoryProfile profile)
        {
            _profiles[profile.GameId] = profile;
            
            try
            {
                var json = JsonSerializer.Serialize(_profiles.Values.ToList(), new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(_storagePath, json);
                _logger.Information("Saved memory profile for game {GameId}", profile.GameId);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to save memory profile for game {GameId}", profile.GameId);
            }
        }
    }
}
