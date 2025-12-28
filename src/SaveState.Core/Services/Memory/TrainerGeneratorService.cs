using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;

namespace SaveState.Core.Services.Memory
{
    public interface ITrainerGeneratorService
    {
        Task<int> StartScanAsync(int pid, MemoryValueType type, string value);
        Task<int> NextScanAsync(string value);
        Task<bool> SaveCheatAsync(Guid gameId, string cheatName);
        void Reset();
        int ResultCount { get; }
        IEnumerable<long> GetResults();
    }

    public class TrainerGeneratorService : ITrainerGeneratorService
    {
        private readonly ILogger _logger = Log.ForContext<TrainerGeneratorService>();
        private readonly IMemoryReader _memoryReader;
        private readonly IMemoryProfileService _profileService;

        private List<long> _currentResults = new();
        private MemoryValueType _currentType;
        private int _currentPid;

        public int ResultCount => _currentResults.Count;

        public TrainerGeneratorService(IMemoryReader memoryReader, IMemoryProfileService profileService)
        {
            _memoryReader = memoryReader ?? throw new ArgumentNullException(nameof(memoryReader));
            _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
        }

        public void Reset()
        {
            _currentResults.Clear();
            _currentType = MemoryValueType.Int;
            _currentPid = 0;
            _logger.Information("Trainer Generator reset");
        }

        public IEnumerable<long> GetResults() => _currentResults.AsReadOnly();

        public async Task<int> StartScanAsync(int pid, MemoryValueType type, string value)
        {
            _currentPid = pid;
            _currentType = type;
            _memoryReader.Attach(pid);

            string aobPattern = ConvertValueToAob(type, value);
            if (string.IsNullOrEmpty(aobPattern))
            {
                _logger.Warning("Invalid value for scan: {Value}", value);
                return 0;
            }

            _logger.Information("Starting scan for value {Value} ({Type}) in Process {PID}", value, type, pid);
            _currentResults = await _memoryReader.ScanAobAsync(aobPattern);
            _logger.Information("Scan complete. Found {Count} results", _currentResults.Count);

            return _currentResults.Count;
        }

        public Task<int> NextScanAsync(string value)
        {
            if (!_memoryReader.IsAttached || _currentResults.Count == 0)
            {
                _logger.Warning("Cannot perform Next Scan: No active session or previous results");
                return Task.FromResult(0);
            }

            _logger.Information("Performing Next Scan for value {Value} on {Count} addresses", value, _currentResults.Count);

            var nextResults = new List<long>();

            // Naive iteration - optimize later if needed
            foreach (var addr in _currentResults)
            {
                bool match = CheckAddressValue(addr, _currentType, value);
                if (match)
                {
                    nextResults.Add(addr);
                }
            }

            _currentResults = nextResults;
            _logger.Information("Next Scan complete. {Count} results remain", _currentResults.Count);

            return Task.FromResult(_currentResults.Count);
        }

        public async Task<bool> SaveCheatAsync(Guid gameId, string cheatName)
        {
            if (_currentResults.Count == 0)
            {
                _logger.Warning("No address found to save");
                return false;
            }

            // Using the first result. Ideally allow user to pick if multiple.
            long address = _currentResults.First();

            // Try to resolve to module relative if possible
            string baseAddressStr = $"0x{address:X}";

            // Check modules to see if we can make it relative
            // (Basic check against main module or common ones could be here)
            // For now, save as absolute hex.

            var profile = await _profileService.GetProfileAsync(gameId);
            if (profile == null)
            {
                profile = new GameMemoryProfile
                {
                    GameId = gameId,
                    GameTitle = "Unknown Game"
                };
            }

            profile.MemoryMap[cheatName] = new MemoryValueDefinition
            {
                BaseAddress = baseAddressStr,
                Type = _currentType,
                Offsets = null // Pointer scanning not yet implemented
            };

            await _profileService.SaveProfileAsync(profile);
            _logger.Information("Saved cheat '{Name}' to profile {GameId}", cheatName, gameId);

            return true;
        }

        private string ConvertValueToAob(MemoryValueType type, string value)
        {
            try
            {
                if (type == MemoryValueType.Int)
                {
                    if (int.TryParse(value, out int intVal))
                    {
                        var bytes = BitConverter.GetBytes(intVal);
                        return BitConverter.ToString(bytes).Replace("-", " ");
                    }
                }
                else if (type == MemoryValueType.Float)
                {
                    if (float.TryParse(value, out float floatVal))
                    {
                        var bytes = BitConverter.GetBytes(floatVal);
                        return BitConverter.ToString(bytes).Replace("-", " ");
                    }
                }
                else if (type == MemoryValueType.Byte)
                {
                    if (byte.TryParse(value, out byte byteVal))
                    {
                        return byteVal.ToString("X2");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error converting value to AOB");
            }
            return string.Empty;
        }

        private bool CheckAddressValue(long address, MemoryValueType type, string expectedValueStr)
        {
            if (type == MemoryValueType.Int)
            {
                int val = _memoryReader.ReadInt(address);
                 if (int.TryParse(expectedValueStr, out int expected))
                 {
                     return val == expected;
                 }
            }
            else if (type == MemoryValueType.Float)
            {
                float val = _memoryReader.ReadFloat(address);
                if (float.TryParse(expectedValueStr, out float expected))
                {
                    // Epsilon check for floats
                    return Math.Abs(val - expected) < 0.001f;
                }
            }
            return false;
        }
    }
}
