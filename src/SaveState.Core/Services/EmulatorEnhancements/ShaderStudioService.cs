using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Serilog;

namespace SaveState.Core.Services.EmulatorEnhancements
{
    public class ShaderPreset
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Dictionary<string, float> Parameters { get; set; } = new();
        public string? CustomCode { get; set; }
    }

    public class ShaderStudioService
    {
        private readonly ILogger _logger = Log.ForContext<ShaderStudioService>();
        private readonly List<ShaderPreset> _presets;
        private ShaderPreset? _activePreset;
        private readonly string _customShadersPath;

        public ShaderStudioService()
        {
            _customShadersPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "SaveState2", "data", "shaders");
            if (!Directory.Exists(_customShadersPath)) Directory.CreateDirectory(_customShadersPath);

            _presets = new List<ShaderPreset>
            {
                new ShaderPreset
                {
                    Id = "crt",
                    Name = "CRT Monitor",
                    Description = "Classic curved CRT monitor effect with scanlines",
                    Parameters = new() { ["curvature"] = 0.3f, ["scanlines"] = 0.8f, ["bloom"] = 0.2f, ["vignette"] = 0.3f }
                },
                new ShaderPreset
                {
                    Id = "lcd",
                    Name = "LCD Grid",
                    Description = "Simulates LCD subpixel grid pattern",
                    Parameters = new() { ["gridIntensity"] = 0.5f, ["brightness"] = 1.1f, ["contrast"] = 1.0f }
                },
                new ShaderPreset
                {
                    Id = "pixelate",
                    Name = "Pixelate",
                    Description = "Chunky pixel effect for low-res aesthetic",
                    Parameters = new() { ["pixelSize"] = 4.0f, ["colorDepth"] = 16.0f }
                },
                new ShaderPreset
                {
                    Id = "vhs",
                    Name = "VHS Tape",
                    Description = "Nostalgic VHS tracking and color bleeding",
                    Parameters = new() { ["noise"] = 0.3f, ["aberration"] = 0.5f, ["tracking"] = 0.2f, ["distortion"] = 0.1f }
                },
                new ShaderPreset
                {
                    Id = "bloom",
                    Name = "Bloom/Glow",
                    Description = "Soft glow effect for bright areas",
                    Parameters = new() { ["intensity"] = 0.5f, ["threshold"] = 0.7f, ["radius"] = 2.0f }
                }
            };

            LoadCustomShaders();
        }

        public List<ShaderPreset> GetPresets() => _presets;

        public ShaderPreset? GetActivePreset() => _activePreset;

        public void ApplyPreset(string presetId)
        {
            _activePreset = _presets.Find(p => p.Id == presetId);
        }

        public void DisableShader()
        {
            _activePreset = null;
        }

        public void UpdateParameter(string parameterId, float value)
        {
            if (_activePreset != null && _activePreset.Parameters.ContainsKey(parameterId))
            {
                _activePreset.Parameters[parameterId] = value;
            }
        }

        public ShaderPreset CreateCustomShader(string name, string description, string glslCode)
        {
            var preset = new ShaderPreset
            {
                Id = $"custom-{Guid.NewGuid().ToString()[..8]}",
                Name = name,
                Description = description,
                CustomCode = glslCode,
                Parameters = new() { ["intensity"] = 1.0f }
            };

            _presets.Add(preset);
            SaveCustomShader(preset);
            return preset;
        }

        private void SaveCustomShader(ShaderPreset preset)
        {
            var path = Path.Combine(_customShadersPath, $"{preset.Id}.json");
            var json = JsonSerializer.Serialize(preset, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }

        private void LoadCustomShaders()
        {
            if (!Directory.Exists(_customShadersPath)) return;

            foreach (var file in Directory.GetFiles(_customShadersPath, "*.json"))
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var preset = JsonSerializer.Deserialize<ShaderPreset>(json);
                    if (preset != null && !_presets.Exists(p => p.Id == preset.Id))
                    {
                        _presets.Add(preset);
                    }
                }
                catch (Exception ex) { _logger.Warning(ex, "Failed to load custom shader"); }
            }
        }

        public void DeleteCustomShader(string presetId)
        {
            var preset = _presets.Find(p => p.Id == presetId);
            if (preset != null && preset.Id.StartsWith("custom-"))
            {
                _presets.Remove(preset);
                var path = Path.Combine(_customShadersPath, $"{preset.Id}.json");
                if (File.Exists(path)) File.Delete(path);
            }
        }
    }
}
