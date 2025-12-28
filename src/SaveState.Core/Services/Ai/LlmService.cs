using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using SaveState.Core.Helpers;
using SaveState.Core.Interfaces;
using SaveState.Core.Services.GameState;
using SaveState.Core.Services.Ai.Memory;
using SaveState.Core.Services.Ai.Adapters;
using Serilog;

namespace SaveState.Core.Services.Ai
{
    // ... (keep LlmProvider enum and LlmConfig/ActiveModel classes)
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
        Task<string> ChatWithModelAsync(string modelName, List<LlmMessage> messages);
        bool IsAvailable { get; }
        LlmProvider CurrentProvider { get; }
        void AddProvider(LlmConfig config);
        void SetPrimaryProvider(LlmProvider provider);
        Task InitializeAsync();
        void ConfigureAdvancedAi(IStateInjector? stateInjector = null, IMemoryOrchestrator? memoryOrchestrator = null, bool enableStateInjection = true);
        IEnumerable<LlmProvider> GetAvailableProviders();
    }

    public class LlmService : ILlmService
    {
        private readonly ILogger _logger = Log.ForContext<LlmService>();
        private readonly IAppConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly OllamaManager _ollamaManager;
        private readonly List<LlmConfig> _configs = new();
        private readonly List<ActiveModel> _activeModels = new();
        private readonly Dictionary<LlmProvider, ILlmProvider> _adapters = new();
        private LlmConfig _primaryConfig;
        private bool _initialized;

        // Advanced AI Architecture integrations
        private IStateInjector? _stateInjector;
        private IMemoryOrchestrator? _memoryOrchestrator;
        private bool _enableStateInjection = false;

        public LlmProvider CurrentProvider => _primaryConfig.Provider;
        public bool IsAvailable => _adapters.ContainsKey(_primaryConfig.Provider) && _adapters[_primaryConfig.Provider].IsAvailable;
        public IReadOnlyList<ActiveModel> ActiveModels => _activeModels;

        public LlmService(IAppConfiguration config, IHttpClientFactory httpClientFactory, OllamaManager ollamaManager, LlmConfig? llmConfig = null)
        {
            _config = config;
            _httpClientFactory = httpClientFactory;
            _ollamaManager = ollamaManager;
            _primaryConfig = llmConfig ?? new LlmConfig();
        }

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

            // 1. Initialize Adapters
            InitializeAdapters();

            // 2. Try to auto-start Ollama
            var ollamaStarted = await _ollamaManager.CheckAndStartAsync();

            // 3. Detect available providers
            // (If no primary config was provided, or if we want to auto-discover better ones)
            if (_primaryConfig.Provider == LlmProvider.Offline)
            {
               await DetectBestProviderAsync();
            }

            // 4. Load installed models from Ollama
            if (ollamaStarted)
            {
                var models = await _ollamaManager.GetInstalledModelsAsync();
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

        private void InitializeAdapters()
        {
            _adapters[LlmProvider.Offline] = new OfflineProvider();

            // Default Ollama
            _adapters[LlmProvider.Ollama] = new OllamaProvider(_httpClientFactory.CreateClient("LlmProvider"), _ollamaManager);

            // Generic setup for others - they will be configured if/when configs are added
            // Currently assuming one instance per provider type for simplicity.
            // In a more complex setup, we might need a factory.
        }

        private void EnsureAdapterConfigured(LlmConfig config)
        {
            if (config.Provider == LlmProvider.OpenAI ||
                config.Provider == LlmProvider.Groq ||
                config.Provider == LlmProvider.Together ||
                config.Provider == LlmProvider.LMStudio)
            {
                // Re-create adapter with specific config keys
                 _adapters[config.Provider] = new OpenAiCompatibleProvider(
                     config.Provider,
                     config.ApiKey ?? "",
                     config.BaseUrl ?? GetDefaultBaseUrl(config.Provider),
                     _httpClientFactory.CreateClient("LlmProvider")
                 );
            }
        }

        private string GetDefaultBaseUrl(LlmProvider provider) => provider switch
        {
            LlmProvider.OpenAI => _config.GetApiEndpoint("OpenAI", "https://api.openai.com/v1/"),
            LlmProvider.Groq => _config.GetApiEndpoint("Groq", "https://api.groq.com/openai/v1/"),
            LlmProvider.Together => _config.GetApiEndpoint("Together", "https://api.together.xyz/v1/"),
            LlmProvider.LMStudio => _config.GetApiEndpoint("LMStudio", "http://localhost:1234/v1/"),
            _ => ""
        };

        private async Task DetectBestProviderAsync()
        {
            // Check Ollama
            if (_ollamaManager.IsRunning)
            {
                var models = await _ollamaManager.GetInstalledModelsAsync();
                if (models.Count > 0)
                {
                    _primaryConfig = new LlmConfig
                    {
                        Provider = LlmProvider.Ollama,
                        BaseUrl = _config.GetApiEndpoint("Ollama", "http://localhost:11434/"),
                        Model = models[0]
                    };
                    return;
                }
            }

            // Check Environment Variables
            if (CheckEnvProvider("GROQ_API_KEY", LlmProvider.Groq, "llama-3.1-8b-instant")) return;
            if (CheckEnvProvider("TOGETHER_API_KEY", LlmProvider.Together, "meta-llama/Llama-3-8b-chat-hf")) return;
            if (CheckEnvProvider("OPENAI_API_KEY", LlmProvider.OpenAI, "gpt-3.5-turbo")) return;

            // Check LM Studio
            var lmStudio = new OpenAiCompatibleProvider(LlmProvider.LMStudio, "", "http://localhost:1234/v1/", _httpClientFactory.CreateClient("LlmProvider"));
            if (await lmStudio.HealthCheckAsync())
            {
                 _primaryConfig = new LlmConfig
                {
                    Provider = LlmProvider.LMStudio,
                    BaseUrl = "http://localhost:1234/v1/",
                    Model = "local-model"
                };
                EnsureAdapterConfigured(_primaryConfig);
                return;
            }
        }

        private bool CheckEnvProvider(string envVar, LlmProvider provider, string defaultModel)
        {
            var key = Environment.GetEnvironmentVariable(envVar);
            if (!string.IsNullOrEmpty(key))
            {
                _primaryConfig = new LlmConfig
                {
                    Provider = provider,
                    ApiKey = key,
                    Model = defaultModel
                };
                EnsureAdapterConfigured(_primaryConfig);
                return true;
            }
            return false;
        }

        public void AddProvider(LlmConfig config)
        {
            _configs.Add(config);
            EnsureAdapterConfigured(config);
            if (_primaryConfig.Provider == LlmProvider.Offline)
            {
                _primaryConfig = config;
            }
        }

        public void SetPrimaryProvider(LlmProvider provider)
        {
            var config = _configs.FirstOrDefault(c => c.Provider == provider);
            if (config != null)
            {
                _primaryConfig = config;
                EnsureAdapterConfigured(config);
            }
        }

        public async Task<string> CompleteAsync(string prompt, string? systemPrompt = null)
        {
            if (!_initialized) await InitializeAsync();

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

            if (_memoryOrchestrator != null)
            {
                await _memoryOrchestrator.RecordInteraction(prompt, response, "llm_completion");
            }

            return response;
        }

        public async Task<string> ChatAsync(List<LlmMessage> messages)
        {
            if (!_initialized) await InitializeAsync();

            EnsureAdapterConfigured(_primaryConfig);

            if (!_adapters.ContainsKey(_primaryConfig.Provider))
            {
                return await _adapters[LlmProvider.Offline].ChatAsync("", messages, 0, 0);
            }

            try
            {
                return await _adapters[_primaryConfig.Provider].ChatAsync(
                    _primaryConfig.Model,
                    messages,
                    _primaryConfig.Temperature,
                    _primaryConfig.MaxTokens);
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Provider {Provider} failed, falling back to offline", _primaryConfig.Provider);
                // Fallback to offline
                return await _adapters[LlmProvider.Offline].ChatAsync("", messages, 0, 0);
            }
        }

        public async Task<string> ChatWithModelAsync(string modelName, List<LlmMessage> messages)
        {
            if (!_initialized) await InitializeAsync();

            // Temporary config switch
            var originalModel = _primaryConfig.Model;
            _primaryConfig.Model = modelName;

            // Check if model belongs to a differnt provider?
            // For now assume current provider supports it or it's cross-provider logic
            // The method logic in original file was a bit loose.

            try
            {
                return await ChatAsync(messages);
            }
            finally
            {
                _primaryConfig.Model = originalModel;
            }
        }

        public string GetProviderStatus()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Primary Provider: {_primaryConfig.Provider}");
            sb.AppendLine($"Model: {_primaryConfig.Model}");
            sb.AppendLine($"Active Models: {_activeModels.Count}");
            sb.AppendLine($"Adapter Ready: {_adapters.ContainsKey(_primaryConfig.Provider)}");
            return sb.ToString();
        }

        public IEnumerable<LlmProvider> GetAvailableProviders()
        {
             return _adapters.Keys.ToList();
        }
    }
}
