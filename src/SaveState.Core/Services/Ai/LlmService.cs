using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using SaveState.Core.Helpers;
using SaveState.Core.Services.GameState;
using SaveState.Core.Services.Ai.Memory;

namespace SaveState.Core.Services.Ai
{
    public enum LlmProvider
    {
        Ollama,       // Local - free
        LMStudio,     // Local - free
        OpenAI,       // Cloud - paid
        Groq,         // Cloud - free tier
        Together,     // Cloud - free tier
        HuggingFace,  // Cloud - free tier
        Offline       // Fallback rule-based
    }

    public class LlmConfig
    {
        public LlmProvider Provider { get; set; } = LlmProvider.Offline;
        public string? ApiKey { get; set; }
        public string? BaseUrl { get; set; }
        public string Model { get; set; } = "llama2";
        public float Temperature { get; set; } = 0.7f;
        public int MaxTokens { get; set; } = 500;
    }

    public class LlmMessage
    {
        public string Role { get; set; } = "user";
        public string Content { get; set; } = string.Empty;
    }

    public class ActiveModel
    {
        public string Name { get; set; } = string.Empty;
        public LlmProvider Provider { get; set; }
        public bool IsReady { get; set; }
        public DateTime LastUsed { get; set; }
    }

    public interface ILlmService
    {
        Task<string> CompleteAsync(string prompt, string? systemPrompt = null);
        Task<string> ChatAsync(List<LlmMessage> messages);
        bool IsAvailable { get; }
        LlmProvider CurrentProvider { get; }
        Task InitializeAsync();
    }

    public class LlmService : ILlmService
    {
        private readonly HttpClient _httpClient;
        private readonly List<LlmConfig> _configs = new();
        private readonly List<ActiveModel> _activeModels = new();
        private LlmConfig _primaryConfig;
        private bool _initialized;
        
        // Advanced AI Architecture integrations
        private IStateInjector? _stateInjector;
        private IMemoryOrchestrator? _memoryOrchestrator;
        private bool _enableStateInjection = false;

        public LlmProvider CurrentProvider => _primaryConfig.Provider;
        public bool IsAvailable => _primaryConfig.Provider != LlmProvider.Offline || true;
        public IReadOnlyList<ActiveModel> ActiveModels => _activeModels;

        public LlmService(LlmConfig? config = null)
        {
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
            _primaryConfig = config ?? new LlmConfig();
        }

        /// <summary>
        /// Configure the Advanced AI Architecture integrations
        /// </summary>
        public void ConfigureAdvancedAi(IStateInjector? stateInjector = null, IMemoryOrchestrator? memoryOrchestrator = null, bool enableStateInjection = true)
        {
            _stateInjector = stateInjector;
            _memoryOrchestrator = memoryOrchestrator;
            _enableStateInjection = enableStateInjection;
        }

        public async Task InitializeAsync()
        {
            if (_initialized) return;
            _initialized = true;

            // Try to auto-start Ollama
            var ollamaStarted = await OllamaManager.Instance.CheckAndStartAsync();
            
            // Detect available providers in order of preference
            await DetectProvidersAsync();

            // Load installed models from Ollama
            if (ollamaStarted)
            {
                var models = await OllamaManager.Instance.GetInstalledModelsAsync();
                foreach (var model in models.Take(SystemCapabilities.GetSafeModelCount()))
                {
                    _activeModels.Add(new ActiveModel
                    {
                        Name = model,
                        Provider = LlmProvider.Ollama,
                        IsReady = true,
                        LastUsed = DateTime.MinValue
                    });
                }
            }
        }

        private async Task DetectProvidersAsync()
        {
            // Check Ollama first (auto-started)
            if (OllamaManager.Instance.IsRunning)
            {
                var models = await OllamaManager.Instance.GetInstalledModelsAsync();
                if (models.Count > 0)
                {
                    _primaryConfig = new LlmConfig
                    {
                        Provider = LlmProvider.Ollama,
                        BaseUrl = "http://localhost:11434/",
                        Model = models[0]
                    };
                    return;
                }
            }

            // Check cloud providers via environment variables
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GROQ_API_KEY")))
            {
                _primaryConfig = new LlmConfig
                {
                    Provider = LlmProvider.Groq,
                    ApiKey = Environment.GetEnvironmentVariable("GROQ_API_KEY"),
                    BaseUrl = "https://api.groq.com/openai/v1/",
                    Model = "llama-3.1-8b-instant"
                };
                _configs.Add(_primaryConfig);
                return;
            }

            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TOGETHER_API_KEY")))
            {
                _primaryConfig = new LlmConfig
                {
                    Provider = LlmProvider.Together,
                    ApiKey = Environment.GetEnvironmentVariable("TOGETHER_API_KEY"),
                    BaseUrl = "https://api.together.xyz/v1/",
                    Model = "meta-llama/Llama-3-8b-chat-hf"
                };
                _configs.Add(_primaryConfig);
                return;
            }

            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("OPENAI_API_KEY")))
            {
                _primaryConfig = new LlmConfig
                {
                    Provider = LlmProvider.OpenAI,
                    ApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY"),
                    BaseUrl = "https://api.openai.com/v1/",
                    Model = "gpt-3.5-turbo"
                };
                _configs.Add(_primaryConfig);
                return;
            }

            // Check LM Studio
            if (await TryConnectLMStudioAsync())
            {
                _primaryConfig = new LlmConfig
                {
                    Provider = LlmProvider.LMStudio,
                    BaseUrl = "http://localhost:1234/v1/",
                    Model = "local-model"
                };
                return;
            }

            // Fallback to offline
            _primaryConfig = new LlmConfig { Provider = LlmProvider.Offline };
        }

        private async Task<bool> TryConnectLMStudioAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("http://localhost:1234/v1/models");
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public void AddProvider(LlmConfig config)
        {
            _configs.Add(config);
            if (_primaryConfig.Provider == LlmProvider.Offline)
            {
                _primaryConfig = config;
            }
        }

        public void SetPrimaryProvider(LlmProvider provider)
        {
            var config = _configs.FirstOrDefault(c => c.Provider == provider);
            if (config != null) _primaryConfig = config;
        }

        public async Task<string> CompleteAsync(string prompt, string? systemPrompt = null)
        {
            if (!_initialized) await InitializeAsync();

            // Apply state injection if configured
            var finalPrompt = prompt;
            if (_enableStateInjection && _stateInjector != null)
            {
                finalPrompt = _stateInjector.Inject(prompt, null);
            }

            var messages = new List<LlmMessage>();
            if (!string.IsNullOrEmpty(systemPrompt))
            {
                messages.Add(new LlmMessage { Role = "system", Content = systemPrompt });
            }
            messages.Add(new LlmMessage { Role = "user", Content = finalPrompt });
            
            var response = await ChatAsync(messages);

            // Record interaction in memory if configured
            if (_memoryOrchestrator != null)
            {
                await _memoryOrchestrator.RecordInteraction(prompt, response, "llm_completion");
            }

            return response;
        }

        public async Task<string> ChatAsync(List<LlmMessage> messages)
        {
            if (!_initialized) await InitializeAsync();

            if (_primaryConfig.Provider == LlmProvider.Offline)
            {
                return OfflineFallback(messages.LastOrDefault()?.Content ?? "");
            }

            try
            {
                return _primaryConfig.Provider switch
                {
                    LlmProvider.Ollama => await CallOllamaAsync(messages),
                    _ => await CallOpenAICompatibleAsync(messages)
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LLM Error: {ex.Message}");
                return OfflineFallback(messages.LastOrDefault()?.Content ?? "");
            }
        }

        public async Task<string> ChatWithModelAsync(string modelName, List<LlmMessage> messages)
        {
            // Use a specific model from active models
            var model = _activeModels.FirstOrDefault(m => m.Name.StartsWith(modelName, StringComparison.OrdinalIgnoreCase));
            if (model == null) return await ChatAsync(messages);

            var originalModel = _primaryConfig.Model;
            _primaryConfig.Model = model.Name;
            model.LastUsed = DateTime.Now;

            try
            {
                return await ChatAsync(messages);
            }
            finally
            {
                _primaryConfig.Model = originalModel;
            }
        }

        private async Task<string> CallOpenAICompatibleAsync(List<LlmMessage> messages)
        {
            var request = new
            {
                model = _primaryConfig.Model,
                messages = messages.Select(m => new { role = m.Role, content = m.Content }),
                temperature = _primaryConfig.Temperature,
                max_tokens = _primaryConfig.MaxTokens
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            if (!string.IsNullOrEmpty(_primaryConfig.ApiKey))
            {
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_primaryConfig.ApiKey}");
            }

            var response = await _httpClient.PostAsync($"{_primaryConfig.BaseUrl}chat/completions", content);
            var responseJson = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(responseJson);
            return doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "";
        }

        private async Task<string> CallOllamaAsync(List<LlmMessage> messages)
        {
            // Use chat API for multi-turn
            var request = new
            {
                model = _primaryConfig.Model,
                messages = messages.Select(m => new { role = m.Role, content = m.Content }),
                stream = false
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_primaryConfig.BaseUrl}api/chat", content);
            var responseJson = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(responseJson);
            return doc.RootElement
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "";
        }

        private string OfflineFallback(string input)
        {
            var rand = new Random(input.GetHashCode());
            
            if (input.Contains("commentary") || input.Contains("comment"))
            {
                var lines = new[]
                {
                    "What an incredible play!",
                    "The tension is palpable!",
                    "A masterful display of skill!",
                    "Things are heating up!",
                    "Absolutely phenomenal!",
                };
                return lines[rand.Next(lines.Length)];
            }
            
            if (input.Contains("fusion") || input.Contains("combine"))
            {
                return "A powerful fusion has been created!";
            }

            if (input.Contains("dream") || input.Contains("level"))
            {
                return "A surreal landscape emerges from your memories...";
            }

            if (input.Contains("predict") || input.Contains("winner"))
            {
                return "This will be a close match - may the best fighter win!";
            }

            if (input.Contains("tip") || input.Contains("suggest"))
            {
                return "Keep practicing and learn from each attempt!";
            }

            return "Processing complete.";
        }

        public void SetConfig(LlmConfig config)
        {
            _primaryConfig = config;
        }

        public LlmConfig GetConfig() => _primaryConfig;

        public List<LlmProvider> GetAvailableProviders()
        {
            var providers = new List<LlmProvider> { LlmProvider.Offline };

            if (OllamaManager.Instance.IsRunning || OllamaManager.Instance.IsInstalled)
                providers.Insert(0, LlmProvider.Ollama);

            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GROQ_API_KEY")))
                providers.Add(LlmProvider.Groq);

            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TOGETHER_API_KEY")))
                providers.Add(LlmProvider.Together);

            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("OPENAI_API_KEY")))
                providers.Add(LlmProvider.OpenAI);

            return providers;
        }

        public string GetProviderStatus()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Primary Provider: {_primaryConfig.Provider}");
            sb.AppendLine($"Model: {_primaryConfig.Model}");
            sb.AppendLine($"Active Models: {_activeModels.Count}");
            
            if (OllamaManager.Instance.Status != OllamaStatus.NotInstalled)
                sb.AppendLine($"Ollama: {OllamaManager.Instance.Status}");

            return sb.ToString();
        }
    }
}
