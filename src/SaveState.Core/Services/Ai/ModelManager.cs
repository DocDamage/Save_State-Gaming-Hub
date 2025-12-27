using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SaveState.Core.Helpers;

namespace SaveState.Core.Services.Ai
{
    public enum ModelDownloadStatus
    {
        NotStarted,
        Downloading,
        Completed,
        Failed,
        Cancelled
    }

    public class ModelInfo
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
        public string Description { get; set; } = string.Empty;
        public bool IsInstalled { get; set; }
        public DateTime? InstalledAt { get; set; }
        public string[] Capabilities { get; set; } = Array.Empty<string>();
        public int ParameterCount { get; set; } // In billions
        public bool RequiresGpu { get; set; }
    }

    public class DownloadProgress
    {
        public string ModelName { get; set; } = string.Empty;
        public ModelDownloadStatus Status { get; set; }
        public long BytesDownloaded { get; set; }
        public long TotalBytes { get; set; }
        public double PercentComplete => TotalBytes > 0 ? (BytesDownloaded * 100.0 / TotalBytes) : 0;
        public string? ErrorMessage { get; set; }
    }

    public class ModelManager
    {
        private static ModelManager? _instance;
        private readonly HttpClient _httpClient;
        private readonly Dictionary<string, CancellationTokenSource> _activeDownloads = new();

        public event EventHandler<DownloadProgress>? DownloadProgressChanged;
        
        public static ModelManager Instance => _instance ??= new ModelManager();

        // Available models catalog
        public static readonly ModelInfo[] AvailableModels = new[]
        {
            new ModelInfo 
            { 
                Name = "phi", 
                DisplayName = "Phi-2", 
                Description = "Microsoft's compact 2.7B model - fastest, lowest resource usage",
                ParameterCount = 3,
                SizeBytes = 1_600_000_000,
                Capabilities = new[] { "chat", "creative" },
                RequiresGpu = false
            },
            new ModelInfo 
            { 
                Name = "llama3.2", 
                DisplayName = "Llama 3.2 (3B)", 
                Description = "Meta's newest small model - best quality for size",
                ParameterCount = 3,
                SizeBytes = 2_000_000_000,
                Capabilities = new[] { "chat", "reasoning", "creative" },
                RequiresGpu = false
            },
            new ModelInfo 
            { 
                Name = "mistral", 
                DisplayName = "Mistral 7B", 
                Description = "Fast and creative - great for gaming commentary",
                ParameterCount = 7,
                SizeBytes = 4_100_000_000,
                Capabilities = new[] { "chat", "creative", "analysis" },
                RequiresGpu = false
            },
            new ModelInfo 
            { 
                Name = "llama2", 
                DisplayName = "Llama 2 (7B)", 
                Description = "Meta's well-rounded model - reliable all-purpose",
                ParameterCount = 7,
                SizeBytes = 3_800_000_000,
                Capabilities = new[] { "chat", "reasoning" },
                RequiresGpu = false
            },
            new ModelInfo 
            { 
                Name = "codellama", 
                DisplayName = "Code Llama", 
                Description = "Specialized for code generation",
                ParameterCount = 7,
                SizeBytes = 3_800_000_000,
                Capabilities = new[] { "code", "analysis" },
                RequiresGpu = true
            },
            new ModelInfo 
            { 
                Name = "llava", 
                DisplayName = "LLaVA (Vision)", 
                Description = "Can understand and describe images",
                ParameterCount = 7,
                SizeBytes = 4_500_000_000,
                Capabilities = new[] { "vision", "chat" },
                RequiresGpu = true
            },
            new ModelInfo 
            { 
                Name = "gemma", 
                DisplayName = "Gemma (7B)", 
                Description = "Google's efficient open model",
                ParameterCount = 7,
                SizeBytes = 5_000_000_000,
                Capabilities = new[] { "chat", "reasoning" },
                RequiresGpu = false
            },
        };

        private ModelManager()
        {
            _httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(30) };
        }

        public async Task<List<ModelInfo>> GetInstalledModelsAsync()
        {
            var installed = new List<ModelInfo>();
            var ollamaModels = await OllamaManager.Instance.GetInstalledModelsAsync();

            foreach (var modelName in ollamaModels)
            {
                var baseName = modelName.Split(':')[0];
                var catalogModel = AvailableModels.FirstOrDefault(m => 
                    m.Name.Equals(baseName, StringComparison.OrdinalIgnoreCase));

                if (catalogModel != null)
                {
                    var copy = new ModelInfo
                    {
                        Name = catalogModel.Name,
                        DisplayName = catalogModel.DisplayName,
                        Description = catalogModel.Description,
                        SizeBytes = catalogModel.SizeBytes,
                        ParameterCount = catalogModel.ParameterCount,
                        Capabilities = catalogModel.Capabilities,
                        RequiresGpu = catalogModel.RequiresGpu,
                        IsInstalled = true,
                        InstalledAt = DateTime.Now
                    };
                    installed.Add(copy);
                }
                else
                {
                    installed.Add(new ModelInfo
                    {
                        Name = baseName,
                        DisplayName = baseName,
                        Description = "Custom model",
                        IsInstalled = true
                    });
                }
            }

            return installed;
        }

        public async Task<List<ModelInfo>> GetRecommendedModelsAsync()
        {
            var sysInfo = SystemCapabilities.GetSystemInfo();
            var recommendations = SystemCapabilities.GetRecommendedModels();
            var installed = await GetInstalledModelsAsync();

            return AvailableModels
                .Where(m => recommendations.Any(r => r.ModelName == m.Name))
                .Select(m => 
                {
                    m.IsInstalled = installed.Any(i => i.Name == m.Name);
                    return m;
                })
                .ToList();
        }

        public async Task<bool> DownloadModelAsync(string modelName, IProgress<DownloadProgress>? progress = null)
        {
            if (!OllamaManager.Instance.IsRunning)
            {
                await OllamaManager.Instance.CheckAndStartAsync();
                if (!OllamaManager.Instance.IsRunning) return false;
            }

            var cts = new CancellationTokenSource();
            _activeDownloads[modelName] = cts;

            var downloadProgress = new DownloadProgress
            {
                ModelName = modelName,
                Status = ModelDownloadStatus.Downloading
            };

            try
            {
                // Use Ollama CLI to pull model
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = OllamaManager.Instance.GetOllamaPath(),
                    Arguments = $"pull {modelName}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = System.Diagnostics.Process.Start(psi);
                if (process == null)
                {
                    downloadProgress.Status = ModelDownloadStatus.Failed;
                    downloadProgress.ErrorMessage = "Failed to start download process";
                    progress?.Report(downloadProgress);
                    return false;
                }

                // Read output for progress
                while (!process.StandardOutput.EndOfStream && !cts.Token.IsCancellationRequested)
                {
                    var line = await process.StandardOutput.ReadLineAsync();
                    if (line != null && line.Contains("%"))
                    {
                        // Parse progress from output like "pulling manifest... 45%"
                        var match = System.Text.RegularExpressions.Regex.Match(line, @"(\d+)%");
                        if (match.Success && int.TryParse(match.Groups[1].Value, out int pct))
                        {
                            downloadProgress.BytesDownloaded = pct;
                            downloadProgress.TotalBytes = 100;
                            progress?.Report(downloadProgress);
                            DownloadProgressChanged?.Invoke(this, downloadProgress);
                        }
                    }
                }

                await process.WaitForExitAsync(cts.Token);

                if (process.ExitCode == 0)
                {
                    downloadProgress.Status = ModelDownloadStatus.Completed;
                    downloadProgress.BytesDownloaded = 100;
                    downloadProgress.TotalBytes = 100;
                }
                else
                {
                    downloadProgress.Status = ModelDownloadStatus.Failed;
                    downloadProgress.ErrorMessage = await process.StandardError.ReadToEndAsync();
                }
            }
            catch (OperationCanceledException)
            {
                downloadProgress.Status = ModelDownloadStatus.Cancelled;
            }
            catch (Exception ex)
            {
                downloadProgress.Status = ModelDownloadStatus.Failed;
                downloadProgress.ErrorMessage = ex.Message;
            }
            finally
            {
                _activeDownloads.Remove(modelName);
            }

            progress?.Report(downloadProgress);
            DownloadProgressChanged?.Invoke(this, downloadProgress);
            return downloadProgress.Status == ModelDownloadStatus.Completed;
        }

        public void CancelDownload(string modelName)
        {
            if (_activeDownloads.TryGetValue(modelName, out var cts))
            {
                cts.Cancel();
            }
        }

        public async Task<bool> DeleteModelAsync(string modelName)
        {
            if (!OllamaManager.Instance.IsRunning) return false;

            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = OllamaManager.Instance.GetOllamaPath(),
                    Arguments = $"rm {modelName}",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = System.Diagnostics.Process.Start(psi);
                if (process == null) return false;
                
                await process.WaitForExitAsync();
                return process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        public int GetMaxSafeModelCount()
        {
            return SystemCapabilities.GetSafeModelCount();
        }

        public bool CanDownloadMore()
        {
            var installed = GetInstalledModelsAsync().GetAwaiter().GetResult();
            return installed.Count < GetMaxSafeModelCount();
        }
    }
}
