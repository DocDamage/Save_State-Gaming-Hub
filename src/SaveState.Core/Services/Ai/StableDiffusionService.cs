using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using SaveState.Core.Helpers;

namespace SaveState.Core.Services.Ai
{
    public enum StableDiffusionStatus
    {
        NotInstalled,
        Installed,
        Starting,
        Running,
        Stopped,
        Error
    }

    public class ImageGenerationRequest
    {
        public string Prompt { get; set; } = string.Empty;
        public string NegativePrompt { get; set; } = "blurry, bad quality, distorted";
        public int Width { get; set; } = 512;
        public int Height { get; set; } = 512;
        public int Steps { get; set; } = 20;
        public double CfgScale { get; set; } = 7.0;
        public long Seed { get; set; } = -1;
        public string Sampler { get; set; } = "Euler a";
    }

    public class GeneratedImage
    {
        public byte[] ImageData { get; set; } = Array.Empty<byte>();
        public string Prompt { get; set; } = string.Empty;
        public long Seed { get; set; }
        public DateTime GeneratedAt { get; set; }
        public string? FilePath { get; set; }
    }

    public class StableDiffusionService
    {
        private static StableDiffusionService? _instance;
        private readonly HttpClient _httpClient;
        private string _apiUrl = "http://localhost:7860";
        private StableDiffusionStatus _status = StableDiffusionStatus.NotInstalled;
        private readonly string _outputPath;

        public event EventHandler<StableDiffusionStatus>? StatusChanged;
        public StableDiffusionStatus Status => _status;
        public bool IsRunning => _status == StableDiffusionStatus.Running;

        public static StableDiffusionService Instance => _instance ??= new StableDiffusionService();

        private StableDiffusionService()
        {
            _httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
            _outputPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "SaveState2", "data", "generated_images");
            if (!Directory.Exists(_outputPath)) Directory.CreateDirectory(_outputPath);
        }

        public void SetApiUrl(string url)
        {
            _apiUrl = url.TrimEnd('/');
        }

        public async Task<bool> CheckConnectionAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_apiUrl}/sdapi/v1/sd-models");
                if (response.IsSuccessStatusCode)
                {
                    SetStatus(StableDiffusionStatus.Running);
                    return true;
                }
            }
            catch { }

            SetStatus(StableDiffusionStatus.NotInstalled);
            return false;
        }

        public async Task<GeneratedImage?> GenerateImageAsync(ImageGenerationRequest request, IProgress<int>? progress = null)
        {
            if (!IsRunning && !await CheckConnectionAsync())
            {
                return null;
            }

            try
            {
                var payload = new
                {
                    prompt = request.Prompt,
                    negative_prompt = request.NegativePrompt,
                    width = request.Width,
                    height = request.Height,
                    steps = request.Steps,
                    cfg_scale = request.CfgScale,
                    seed = request.Seed,
                    sampler_name = request.Sampler
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                progress?.Report(10);

                var response = await _httpClient.PostAsync($"{_apiUrl}/sdapi/v1/txt2img", content);
                
                progress?.Report(90);

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseJson);

                if (doc.RootElement.TryGetProperty("images", out var images) && 
                    images.GetArrayLength() > 0)
                {
                    var base64Image = images[0].GetString();
                    if (string.IsNullOrEmpty(base64Image)) return null;

                    var imageData = Convert.FromBase64String(base64Image);
                    var seed = doc.RootElement.TryGetProperty("info", out var info) 
                        ? ParseSeedFromInfo(info.GetString()) 
                        : request.Seed;

                    var generated = new GeneratedImage
                    {
                        ImageData = imageData,
                        Prompt = request.Prompt,
                        Seed = seed,
                        GeneratedAt = DateTime.Now
                    };

                    // Save to disk
                    var fileName = $"sd_{DateTime.Now:yyyyMMdd_HHmmss}_{seed}.png";
                    generated.FilePath = Path.Combine(_outputPath, fileName);
                    await File.WriteAllBytesAsync(generated.FilePath, imageData);

                    progress?.Report(100);
                    return generated;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SD generation error: {ex.Message}");
            }

            return null;
        }

        private long ParseSeedFromInfo(string? infoJson)
        {
            if (string.IsNullOrEmpty(infoJson)) return -1;
            try
            {
                using var doc = JsonDocument.Parse(infoJson);
                if (doc.RootElement.TryGetProperty("seed", out var seedProp))
                {
                    return seedProp.GetInt64();
                }
            }
            catch { }
            return -1;
        }

        public async Task<List<string>> GetAvailableModelsAsync()
        {
            var models = new List<string>();
            
            try
            {
                var response = await _httpClient.GetStringAsync($"{_apiUrl}/sdapi/v1/sd-models");
                using var doc = JsonDocument.Parse(response);
                
                foreach (var model in doc.RootElement.EnumerateArray())
                {
                    if (model.TryGetProperty("model_name", out var name))
                    {
                        models.Add(name.GetString() ?? "");
                    }
                }
            }
            catch { }

            return models;
        }

        public async Task<List<string>> GetAvailableSamplersAsync()
        {
            var samplers = new List<string>();
            
            try
            {
                var response = await _httpClient.GetStringAsync($"{_apiUrl}/sdapi/v1/samplers");
                using var doc = JsonDocument.Parse(response);
                
                foreach (var sampler in doc.RootElement.EnumerateArray())
                {
                    if (sampler.TryGetProperty("name", out var name))
                    {
                        samplers.Add(name.GetString() ?? "");
                    }
                }
            }
            catch { }

            return samplers.Count > 0 ? samplers : new List<string> 
            { 
                "Euler a", "Euler", "LMS", "DPM++ 2M", "DPM++ SDE", "DDIM" 
            };
        }

        public async Task<bool> SetModelAsync(string modelName)
        {
            try
            {
                var payload = new { sd_model_checkpoint = modelName };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync($"{_apiUrl}/sdapi/v1/options", content);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public List<GeneratedImage> GetGeneratedImages(int limit = 50)
        {
            var images = new List<GeneratedImage>();
            
            if (!Directory.Exists(_outputPath)) return images;

            foreach (var file in Directory.GetFiles(_outputPath, "*.png")
                .OrderByDescending(File.GetCreationTime)
                .Take(limit))
            {
                images.Add(new GeneratedImage
                {
                    FilePath = file,
                    GeneratedAt = File.GetCreationTime(file)
                });
            }

            return images;
        }

        public void DeleteImage(string filePath)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        public string GetOutputPath() => _outputPath;

        public string GetSystemRequirements()
        {
            var sysInfo = SystemCapabilities.GetSystemInfo();
            var gpu = sysInfo.Gpu;

            if (gpu == null)
                return "❌ No GPU detected. Stable Diffusion requires a dedicated GPU.";

            if (!gpu.IsNvidia)
                return $"⚠️ {gpu.Name} detected. SD works best with NVIDIA GPUs (CUDA). AMD support is limited.";

            var vramGb = gpu.VramBytes / (1024.0 * 1024 * 1024);
            if (vramGb < 4)
                return $"⚠️ {gpu.Name} has {vramGb:F1}GB VRAM. SD needs 4GB+ for good performance.";

            if (vramGb >= 8)
                return $"✅ {gpu.Name} with {vramGb:F0}GB VRAM. Excellent for Stable Diffusion!";

            return $"✅ {gpu.Name} with {vramGb:F0}GB VRAM. Good for SD with medium settings.";
        }

        private void SetStatus(StableDiffusionStatus status)
        {
            if (_status != status)
            {
                _status = status;
                StatusChanged?.Invoke(this, status);
            }
        }
    }
}
