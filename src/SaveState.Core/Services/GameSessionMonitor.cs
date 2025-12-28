using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SaveState.Core.Entities;
using SaveState.Core.Services.Ai;
using SaveState.Core.Services.GameState;
using Serilog;

namespace SaveState.Core.Services
{
    public interface IGameSessionMonitor
    {
        Task StartMonitoringAsync(Guid gameId, int pid);
        Task StopMonitoringAsync();
        bool IsMonitoring { get; }
        int CurrentPid { get; }
        Guid CurrentGameId { get; }
    }

    public class GameSessionMonitor : IGameSessionMonitor
    {
        private readonly ILogger _logger = Log.ForContext<GameSessionMonitor>();
        private readonly IUltimateAiOrchestrator _orchestrator;
        private readonly IWorldStateService _worldStateService;
        private readonly SaveState.Core.Services.Memory.IMemoryReader _memoryReader;
        private readonly SaveState.Core.Services.Memory.IMemoryProfileService _profileService;
        private SaveState.Core.Services.Memory.GameMemoryProfile? _currentProfile;

        private CancellationTokenSource? _cts;
        private Task? _monitorTask;
        private Guid _currentGameId;
        private int _currentPid;
        private readonly Dictionary<string, long> _addressCache = new();

        public bool IsMonitoring => _monitorTask != null && !_monitorTask.IsCompleted;
        public int CurrentPid => _currentPid;
        public Guid CurrentGameId => _currentGameId;

        public GameSessionMonitor(
            IUltimateAiOrchestrator orchestrator,
            IWorldStateService worldStateService,
            SaveState.Core.Services.Memory.IMemoryProfileService profileService,
            SaveState.Core.Services.Memory.IMemoryReader memoryReader)
        {
            _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
            _worldStateService = worldStateService ?? throw new ArgumentNullException(nameof(worldStateService));
            _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
            _memoryReader = memoryReader ?? throw new ArgumentNullException(nameof(memoryReader));
        }

        public async Task StartMonitoringAsync(Guid gameId, int pid)
        {
            if (IsMonitoring) await StopMonitoringAsync();

            _currentGameId = gameId;
            _currentPid = pid;
            _addressCache.Clear();

            _currentProfile = await _profileService.GetProfileAsync(gameId);

            if (pid > 0)
            {
               _memoryReader.Attach(pid);
            }

            _cts = new CancellationTokenSource();
            _monitorTask = Task.Run(() => MonitorLoop(_cts.Token), _cts.Token);

            await _orchestrator.ExecuteAsync("System: Game Session Started",
                new PipelineContextData
                {
                    CustomData = new Dictionary<string, object>
                    {
                        { "GameId", gameId },
                        { "PID", pid },
                        { "ProfileLoaded", _currentProfile != null }
                    }
                });
        }

        public async Task StopMonitoringAsync()
        {
            if (_cts != null)
            {
                _cts.Cancel();
                try
                {
                    if (_monitorTask != null) await _monitorTask;
                }
                catch (OperationCanceledException)
                {
                    // Expected when stopping monitoring - cancellation token triggered
                }
                _cts.Dispose();
                _cts = null;
            }

            _memoryReader.Detach();

            await _orchestrator.ExecuteAsync("System: Game Session Ended",
                new PipelineContextData
                {
                    CustomData = new Dictionary<string, object> { { "GameId", _currentGameId } }
                });
        }

        private async Task MonitorLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    if (_memoryReader.IsAttached && _currentProfile != null)
                    {
                        foreach (var kvp in _currentProfile.MemoryMap)
                        {
                            var key = kvp.Key; // e.g., "GOLD"
                            var def = kvp.Value;

                            long baseAddr = 0;

                            // Check cache first for this value key
                            if (_addressCache.TryGetValue(key, out var cachedAddr))
                            {
                                baseAddr = cachedAddr;
                            }
                            else
                            {
                                // Resolve and cache
                                if (def.BaseAddress.Contains('+'))
                                {
                                    var parts = def.BaseAddress.Split('+');
                                    if (parts.Length == 2)
                                    {
                                        var moduleOrBase = parts[0].Trim();
                                        var offsetStr = parts[1].Trim().Replace("0x", "");

                                        if (moduleOrBase.StartsWith("0x") && long.TryParse(moduleOrBase.Replace("0x", ""), System.Globalization.NumberStyles.HexNumber, null, out var ptrBase))
                                        {
                                            baseAddr = ptrBase;
                                        }
                                        else
                                        {
                                            baseAddr = _memoryReader.GetModuleBaseAddress(moduleOrBase);
                                        }

                                        if (baseAddr > 0 && long.TryParse(offsetStr, System.Globalization.NumberStyles.HexNumber, null, out var offset))
                                        {
                                            baseAddr += offset;
                                        }
                                        else
                                        {
                                            baseAddr = 0;
                                        }
                                    }
                                }
                                else
                                {
                                    long.TryParse(def.BaseAddress.Replace("0x", ""), System.Globalization.NumberStyles.HexNumber, null, out baseAddr);
                                }

                                if (baseAddr > 0)
                                {
                                    _addressCache[key] = baseAddr;
                                }
                            }

                            if (baseAddr > 0)
                            {
                                // Apply pointer chain scan if def.Offsets is present
                                long finalAddress = baseAddr;
                                if (def.Offsets != null && def.Offsets.Length > 0)
                                {
                                    finalAddress = _memoryReader.ReadPointerChain(baseAddr, def.Offsets);
                                    if (finalAddress == 0) continue;
                                }

                                switch (def.Type)
                                {
                                    case SaveState.Core.Services.Memory.MemoryValueType.Int:
                                        var intVal = _memoryReader.ReadInt(finalAddress);
                                        _worldStateService.SetCounter(key, intVal, "memory");
                                        break;
                                    case SaveState.Core.Services.Memory.MemoryValueType.Float:
                                        var floatVal = _memoryReader.ReadFloat(finalAddress);
                                        _worldStateService.SetCounter(key, (int)(floatVal * 100), "memory");
                                        _worldStateService.SetCounter(key + "_RAW", (int)floatVal, "memory");
                                        break;
                                    case SaveState.Core.Services.Memory.MemoryValueType.Byte:
                                        var bytes = _memoryReader.ReadBytes(finalAddress, 1);
                                        if (bytes.Length > 0)
                                            _worldStateService.SetCounter(key, bytes[0], "memory");
                                        break;
                                    case SaveState.Core.Services.Memory.MemoryValueType.String:
                                        // Strings are handled differently
                                        break;
                                }
                            }
                        }
                    }
                    else if (_memoryReader.IsAttached)
                    {
                        // Fallback logic if no profile?
                    }

                    await Task.Delay(1000, ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.Warning(ex, "Monitor loop error occurred");
                    await Task.Delay(5000, ct);
                }
            }
        }
    }
}
