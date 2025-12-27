using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using SaveState.Core.Helpers;

namespace SaveState.Core.Services.Ai.Adapters
{
    public abstract class BaseHttpLlmProvider : ILlmProvider
    {
        protected readonly HttpClient HttpClient;
        protected string BaseUrl { get; set; } = string.Empty;
        protected string ApiKey { get; set; } = string.Empty;

        public abstract LlmProvider ProviderType { get; }
        public virtual string ProviderName => ProviderType.ToString();
        public virtual bool IsAvailable => !string.IsNullOrEmpty(BaseUrl);

        protected BaseHttpLlmProvider()
        {
            HttpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        }

        public abstract Task<string> ChatAsync(string model, List<LlmMessage> messages, float temperature, int maxTokens);

        public virtual async Task<bool> HealthCheckAsync()
        {
            try
            {
                // Simple connectivity check
                var response = await HttpClient.GetAsync(BaseUrl);
                return response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NotFound; // 404 usually means server exists
            }
            catch
            {
                return false;
            }
        }
    }

    public class OpenAiCompatibleProvider : BaseHttpLlmProvider
    {
        private readonly LlmProvider _type;
        
        public override LlmProvider ProviderType => _type;

        public OpenAiCompatibleProvider(LlmProvider type, string apiKey, string baseUrl)
        {
            _type = type;
            ApiKey = apiKey;
            BaseUrl = baseUrl;
            if (!BaseUrl.EndsWith("/")) BaseUrl += "/";
        }

        public override async Task<string> ChatAsync(string model, List<LlmMessage> messages, float temperature, int maxTokens)
        {
            var request = new
            {
                model = model,
                messages = messages.Select(m => new { role = m.Role, content = m.Content }),
                temperature = temperature,
                max_tokens = maxTokens
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var requestMsg = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}chat/completions")
            {
                Content = content
            };

            if (!string.IsNullOrEmpty(ApiKey))
            {
                requestMsg.Headers.Add("Authorization", $"Bearer {ApiKey}");
            }

            try
            {
                var response = await HttpClient.SendAsync(requestMsg);
                
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new LlmProviderException(ProviderType, $"API Error {response.StatusCode}: {error}", response.StatusCode == System.Net.HttpStatusCode.TooManyRequests);
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseJson);
                
                // Handle different response structures if necessary, but standard OpenAI format is:
                return doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString() ?? "";
            }
            catch (HttpRequestException ex)
            {
                throw new LlmProviderException(ProviderType, "Network error: " + ex.Message, true, ex);
            }
        }
    }

    public class OllamaProvider : BaseHttpLlmProvider
    {
        public override LlmProvider ProviderType => LlmProvider.Ollama;

        public OllamaProvider(string baseUrl = "http://localhost:11434/")
        {
            BaseUrl = baseUrl;
            if (!BaseUrl.EndsWith("/")) BaseUrl += "/";
        }

        public override async Task<string> ChatAsync(string model, List<LlmMessage> messages, float temperature, int maxTokens)
        {
             var request = new
            {
                model = model,
                messages = messages.Select(m => new { role = m.Role, content = m.Content }),
                stream = false,
                options = new { temperature = temperature, num_predict = maxTokens }
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await HttpClient.PostAsync($"{BaseUrl}api/chat", content);
                
                if (!response.IsSuccessStatusCode)
                {
                    throw new LlmProviderException(ProviderType, $"Ollama Error {response.StatusCode}");
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseJson);
                
                return doc.RootElement
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString() ?? "";
            }
            catch (HttpRequestException ex)
            {
                throw new LlmProviderException(ProviderType, "Ollama connection failed", true, ex);
            }
        }

        public override Task<bool> HealthCheckAsync()
        {
            return Task.FromResult(OllamaManager.Instance.IsRunning);
        }
    }

    public class OfflineProvider : ILlmProvider
    {
        public LlmProvider ProviderType => LlmProvider.Offline;
        public string ProviderName => "Offline Fallback";
        public bool IsAvailable => true;

        public Task<string> ChatAsync(string model, List<LlmMessage> messages, float temperature, int maxTokens)
        {
            var lastMsg = messages.LastOrDefault()?.Content ?? "";
            var rand = new Random(lastMsg.GetHashCode());
            
            string response;
            if (lastMsg.Contains("commentary", StringComparison.OrdinalIgnoreCase))
                response = "The action is intense!";
            else if (lastMsg.Contains("quest", StringComparison.OrdinalIgnoreCase))
                response = "A new journey awaits.";
            else
                response = "Offline mode: Unable to generate complex response.";

            return Task.FromResult(response);
        }

        public Task<bool> HealthCheckAsync() => Task.FromResult(true);
    }
}
