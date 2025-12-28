using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace SaveState.Core.Services.Ai
{
    public enum OllamaStatus
    {
        NotInstalled,
        Installed,
        Starting,
        Running,
        Stopped,
        Error
    }

    public class OllamaManager : IDisposable
    {
        private readonly ILogger _logger = Log.ForContext<OllamaManager>();
        private Process? _ollamaProcess;
        private readonly HttpClient _httpClient;
        private readonly string _ollamaPath;
        private readonly string _bundledOllamaPath;
        private OllamaStatus _status = OllamaStatus.NotInstalled;
        private CancellationTokenSource? _healthCheckCts;

        public event EventHandler<OllamaStatus>? StatusChanged;
        public OllamaStatus Status => _status;
        public bool IsRunning => _status == OllamaStatus.Running;

        public static OllamaManager? Instance { get; private set; }

        public OllamaManager(IHttpClientFactory httpClientFactory)
        {
            Instance = this;
            _httpClient = httpClientFactory.CreateClient("OllamaManager");
            _bundledOllamaPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "SaveState2", "tools", "ollama");
            _ollamaPath = FindOllamaPath();
        }

        private string FindOllamaPath()
        {
            // Check common installation paths
            var paths = new[]
            {
                // Bundled path first
                Path.Combine(_bundledOllamaPath, "ollama.exe"),
                // Standard Windows install
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Ollama", "ollama.exe"),
                // Program Files
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Ollama", "ollama.exe"),
                // In PATH
                "ollama"
            };

            foreach (var path in paths)
            {
                if (path == "ollama")
                {
                    // Check if in PATH
                    try
                    {
                        var psi = new ProcessStartInfo("where", "ollama")
                        {
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };
                        using var proc = Process.Start(psi);
                        var output = proc?.StandardOutput.ReadToEnd().Trim();
                        proc?.WaitForExit();
                        if (!string.IsNullOrEmpty(output) && File.Exists(output.Split('\n')[0]))
                            return output.Split('\n')[0];
                    }
                    catch (Exception ex)
                    {
                        _logger.Warning(ex, "Failed to find ollama in PATH");
                    }
                }
                else if (File.Exists(path))
                {
                    return path;
                }
            }

            return string.Empty;
        }

        public async Task<bool> CheckAndStartAsync()
        {
            // First check if already running
            if (await IsOllamaRunningAsync())
            {
                SetStatus(OllamaStatus.Running);
                return true;
            }

            // Check if installed
            if (string.IsNullOrEmpty(_ollamaPath) || !File.Exists(_ollamaPath))
            {
                SetStatus(OllamaStatus.NotInstalled);
                return false;
            }

            SetStatus(OllamaStatus.Installed);

            // Start Ollama
            return await StartOllamaAsync();
        }

        public async Task<bool> IsOllamaRunningAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("http://localhost:11434/api/tags");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> StartOllamaAsync()
        {
            if (string.IsNullOrEmpty(_ollamaPath)) return false;

            try
            {
                SetStatus(OllamaStatus.Starting);

                _ollamaProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = _ollamaPath,
                        Arguments = "serve",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    },
                    EnableRaisingEvents = true
                };

                _ollamaProcess.Exited += (s, e) => SetStatus(OllamaStatus.Stopped);
                _ollamaProcess.Start();

                // Wait for Ollama to be ready
                for (int i = 0; i < 30; i++)
                {
                    await Task.Delay(500);
                    if (await IsOllamaRunningAsync())
                    {
                        SetStatus(OllamaStatus.Running);
                        StartHealthCheck();
                        return true;
                    }
                }

                SetStatus(OllamaStatus.Error);
                return false;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to start Ollama");
                SetStatus(OllamaStatus.Error);
                return false;
            }
        }

        public void StopOllama()
        {
            _healthCheckCts?.Cancel();

            try
            {
                if (_ollamaProcess != null && !_ollamaProcess.HasExited)
                {
                    _ollamaProcess.Kill();
                    _ollamaProcess.Dispose();
                    _ollamaProcess = null;
                }
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Failed to stop Ollama process");
            }

            SetStatus(OllamaStatus.Stopped);
        }

        private void StartHealthCheck()
        {
            _healthCheckCts = new CancellationTokenSource();
            _ = Task.Run(async () =>
            {
                while (!_healthCheckCts.Token.IsCancellationRequested)
                {
                    await Task.Delay(10000, _healthCheckCts.Token);
                    if (!await IsOllamaRunningAsync())
                    {
                        SetStatus(OllamaStatus.Stopped);
                        // Try to restart
                        await StartOllamaAsync();
                    }
                }
            }, _healthCheckCts.Token);
        }

        public async Task<List<string>> GetInstalledModelsAsync()
        {
            var models = new List<string>();

            try
            {
                var response = await _httpClient.GetStringAsync("http://localhost:11434/api/tags");
                using var doc = JsonDocument.Parse(response);

                if (doc.RootElement.TryGetProperty("models", out var modelsArray))
                {
                    foreach (var model in modelsArray.EnumerateArray())
                    {
                        if (model.TryGetProperty("name", out var name))
                        {
                            models.Add(name.GetString() ?? "");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Failed to get installed models");
            }

            return models;
        }

        public async Task<bool> IsModelInstalledAsync(string modelName)
        {
            var models = await GetInstalledModelsAsync();
            return models.Exists(m => m.StartsWith(modelName, StringComparison.OrdinalIgnoreCase));
        }

        public string GetOllamaPath() => _ollamaPath;
        public bool IsInstalled => !string.IsNullOrEmpty(_ollamaPath) && File.Exists(_ollamaPath);

        public string GetBundledPath() => _bundledOllamaPath;

        private void SetStatus(OllamaStatus status)
        {
            if (_status != status)
            {
                _status = status;
                StatusChanged?.Invoke(this, status);
            }
        }

        public void Dispose()
        {
            StopOllama();
            _httpClient.Dispose();
            _healthCheckCts?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
