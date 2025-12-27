using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SaveState.Core.Services.Mods
{
    /// <summary>
    /// Central gateway for loading, managing, and executing mods.
    /// Provides a secure interface between mods and the core application.
    /// </summary>
    public interface IModGateway
    {
        Task<ModLoadResult> LoadModAsync(string modPath);
        Task<bool> UnloadModAsync(string modId);
        Task<IReadOnlyList<LoadedMod>> GetLoadedModsAsync();
        Task<ModExecutionResult> ExecuteModAsync(string modId, string action, Dictionary<string, object> parameters);
        void RegisterModEventHandler(string eventType, Func<ModEvent, Task> handler);
        Task<bool> EnableModAsync(string modId);
        Task<bool> DisableModAsync(string modId);
    }

    public class ModGateway : IModGateway
    {
        private readonly IModValidator _validator;
        private readonly ISandboxEnvironment _sandbox;
        private readonly ILogger<ModGateway>? _logger;
        
        private readonly Dictionary<string, LoadedMod> _loadedMods = new();
        private readonly Dictionary<string, List<Func<ModEvent, Task>>> _eventHandlers = new();
        private readonly object _lock = new();

        public ModGateway(IModValidator validator, ISandboxEnvironment sandbox, ILogger<ModGateway>? logger = null)
        {
            _validator = validator;
            _sandbox = sandbox;
            _logger = logger;
        }

        public async Task<ModLoadResult> LoadModAsync(string modPath)
        {
            var result = new ModLoadResult { Success = false };

            try
            {
                // Step 1: Validate the mod
                var manifest = await ParseModManifestAsync(modPath);
                if (manifest == null)
                {
                    result.ErrorMessage = "Invalid mod manifest";
                    return result;
                }

                var validationResult = await _validator.ValidateModAsync(modPath, manifest);
                if (!validationResult.IsValid)
                {
                    result.ErrorMessage = $"Mod validation failed: {string.Join(", ", validationResult.Errors)}";
                    result.ValidationErrors = validationResult.Errors.ToList();
                    return result;
                }

                // Step 2: Check for conflicts
                var conflicts = await CheckConflictsAsync(manifest);
                if (conflicts.Any())
                {
                    result.Warnings.AddRange(conflicts.Select(c => $"Conflict with mod: {c}"));
                }

                // Step 3: Load into sandbox
                var sandboxResult = await _sandbox.LoadModAsync(modPath, manifest);
                if (!sandboxResult.Success)
                {
                    result.ErrorMessage = $"Sandbox load failed: {sandboxResult.ErrorMessage}";
                    return result;
                }

                // Step 4: Register the mod
                var loadedMod = new LoadedMod
                {
                    ModId = manifest.Id,
                    Name = manifest.Name,
                    Version = manifest.Version,
                    Author = manifest.Author,
                    Description = manifest.Description,
                    Path = modPath,
                    Manifest = manifest,
                    LoadedAt = DateTime.UtcNow,
                    IsEnabled = true,
                    SandboxContext = sandboxResult.Context
                };

                lock (_lock)
                {
                    _loadedMods[manifest.Id] = loadedMod;
                }

                _logger?.LogInformation("Mod loaded: {ModId} v{Version}", manifest.Id, manifest.Version);

                result.Success = true;
                result.LoadedMod = loadedMod;

                // Fire mod loaded event
                await FireEventAsync(new ModEvent
                {
                    EventType = "ModLoaded",
                    ModId = manifest.Id,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to load mod from {ModPath}", modPath);
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        public async Task<bool> UnloadModAsync(string modId)
        {
            LoadedMod? mod;
            lock (_lock)
            {
                if (!_loadedMods.TryGetValue(modId, out mod))
                    return false;
            }

            try
            {
                // Unload from sandbox
                await _sandbox.UnloadModAsync(mod.SandboxContext);

                lock (_lock)
                {
                    _loadedMods.Remove(modId);
                }

                _logger?.LogInformation("Mod unloaded: {ModId}", modId);

                await FireEventAsync(new ModEvent
                {
                    EventType = "ModUnloaded",
                    ModId = modId,
                    Timestamp = DateTime.UtcNow
                });

                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to unload mod {ModId}", modId);
                return false;
            }
        }

        public Task<IReadOnlyList<LoadedMod>> GetLoadedModsAsync()
        {
            lock (_lock)
            {
                return Task.FromResult<IReadOnlyList<LoadedMod>>(_loadedMods.Values.ToList());
            }
        }

        public async Task<ModExecutionResult> ExecuteModAsync(
            string modId, 
            string action, 
            Dictionary<string, object> parameters)
        {
            var result = new ModExecutionResult { Success = false };

            LoadedMod? mod;
            lock (_lock)
            {
                if (!_loadedMods.TryGetValue(modId, out mod))
                {
                    result.ErrorMessage = "Mod not loaded";
                    return result;
                }
            }

            if (!mod.IsEnabled)
            {
                result.ErrorMessage = "Mod is disabled";
                return result;
            }

            try
            {
                // Execute in sandbox
                var sandboxResult = await _sandbox.ExecuteAsync(
                    mod.SandboxContext,
                    action,
                    parameters
                );

                result.Success = sandboxResult.Success;
                result.Result = sandboxResult.Result;
                result.ErrorMessage = sandboxResult.ErrorMessage;
                result.ExecutionTimeMs = sandboxResult.ExecutionTimeMs;

                // Log execution metrics
                _logger?.LogDebug(
                    "Mod {ModId} action {Action} completed in {TimeMs}ms",
                    modId, action, result.ExecutionTimeMs
                );

                await FireEventAsync(new ModEvent
                {
                    EventType = "ModExecuted",
                    ModId = modId,
                    Timestamp = DateTime.UtcNow,
                    Data = new Dictionary<string, object>
                    {
                        ["Action"] = action,
                        ["Success"] = result.Success,
                        ["ExecutionTimeMs"] = result.ExecutionTimeMs
                    }
                });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Mod execution failed: {ModId}/{Action}", modId, action);
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        public void RegisterModEventHandler(string eventType, Func<ModEvent, Task> handler)
        {
            lock (_lock)
            {
                if (!_eventHandlers.ContainsKey(eventType))
                    _eventHandlers[eventType] = new List<Func<ModEvent, Task>>();

                _eventHandlers[eventType].Add(handler);
            }
        }

        public async Task<bool> EnableModAsync(string modId)
        {
            lock (_lock)
            {
                if (_loadedMods.TryGetValue(modId, out var mod))
                {
                    mod.IsEnabled = true;
                    return true;
                }
            }
            
            await FireEventAsync(new ModEvent
            {
                EventType = "ModEnabled",
                ModId = modId,
                Timestamp = DateTime.UtcNow
            });
            
            return false;
        }

        public async Task<bool> DisableModAsync(string modId)
        {
            lock (_lock)
            {
                if (_loadedMods.TryGetValue(modId, out var mod))
                {
                    mod.IsEnabled = false;
                    return true;
                }
            }
            
            await FireEventAsync(new ModEvent
            {
                EventType = "ModDisabled",
                ModId = modId,
                Timestamp = DateTime.UtcNow
            });
            
            return false;
        }

        private async Task<ModManifest?> ParseModManifestAsync(string modPath)
        {
            var manifestPath = Path.Combine(modPath, "mod.json");
            if (!File.Exists(manifestPath))
                return null;

            try
            {
                var json = await File.ReadAllTextAsync(manifestPath);
                return System.Text.Json.JsonSerializer.Deserialize<ModManifest>(json);
            }
            catch
            {
                return null;
            }
        }

        private Task<List<string>> CheckConflictsAsync(ModManifest manifest)
        {
            var conflicts = new List<string>();

            lock (_lock)
            {
                foreach (var loadedMod in _loadedMods.Values)
                {
                    // Check for version conflicts
                    if (loadedMod.ModId == manifest.Id && loadedMod.Version != manifest.Version)
                    {
                        conflicts.Add($"{loadedMod.Name} (different version loaded)");
                    }

                    // Check for declared incompatibilities
                    if (manifest.IncompatibleWith?.Contains(loadedMod.ModId) == true)
                    {
                        conflicts.Add(loadedMod.Name);
                    }
                }
            }

            return Task.FromResult(conflicts);
        }

        private async Task FireEventAsync(ModEvent evt)
        {
            List<Func<ModEvent, Task>>? handlers;
            lock (_lock)
            {
                if (!_eventHandlers.TryGetValue(evt.EventType, out handlers))
                    return;
                
                handlers = handlers.ToList(); // Copy to avoid lock during execution
            }

            foreach (var handler in handlers)
            {
                try
                {
                    await handler(evt);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error in mod event handler for {EventType}", evt.EventType);
                }
            }
        }
    }

    #region Models

    public class ModManifest
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = "1.0.0";
        public string Author { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string EntryPoint { get; set; } = string.Empty;
        public string[]? Dependencies { get; set; }
        public string[]? IncompatibleWith { get; set; }
        public ModPermissions Permissions { get; set; } = new();
        public Dictionary<string, object>? Config { get; set; }
    }

    public class ModPermissions
    {
        public bool CanReadMemory { get; set; }
        public bool CanWriteMemory { get; set; }
        public bool CanAccessNetwork { get; set; }
        public bool CanAccessFileSystem { get; set; }
        public bool CanModifyUI { get; set; }
        public bool CanInjectCode { get; set; }
        public string[] AllowedApis { get; set; } = Array.Empty<string>();
    }

    public class LoadedMod
    {
        public string ModId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public ModManifest Manifest { get; set; } = new();
        public DateTime LoadedAt { get; set; }
        public bool IsEnabled { get; set; }
        public SandboxContext SandboxContext { get; set; } = new();
    }

    public class ModLoadResult
    {
        public bool Success { get; set; }
        public LoadedMod? LoadedMod { get; set; }
        public string? ErrorMessage { get; set; }
        public List<string> ValidationErrors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }

    public class ModExecutionResult
    {
        public bool Success { get; set; }
        public object? Result { get; set; }
        public string? ErrorMessage { get; set; }
        public long ExecutionTimeMs { get; set; }
    }

    public class ModEvent
    {
        public string EventType { get; set; } = string.Empty;
        public string ModId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public Dictionary<string, object>? Data { get; set; }
    }

    #endregion
}
