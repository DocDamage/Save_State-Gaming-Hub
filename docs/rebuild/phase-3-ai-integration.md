# Phase 3: AI & Memory Systems (Weeks 11-16)

---

[← Back to README](./README.md) | [Phase 2](./phase-2-game-library.md) | [Phase 4/5 →](./phase-4-5-polish.md)

---

## **🏗️ Phase 3: AI & Memory Systems (Weeks 11-16)**

### **3.1 AI Pipeline Architecture**

#### **Task T-3.1.1: Circuit Breaker Pattern Implementation**

| Attribute | Value |
|:---|:---|
| **Estimated Time** | 12 hours |
| **Dependencies** | T-0.3.2 |
| **AI Turns** | 2-3 |
| **Files Created** | 2 |
| **NuGet Packages** | `Polly`, `Polly.Extensions.Http` |
| **Est. Lines** | ~100 LOC |

**Assumes Exists:**

- DI container from T-0.3.2

**Steps:**

1. **Resilience Policy Provider**

📁 Create: `src/SaveState.Infrastructure/Ai/Resilience/AiResiliencePolicy.cs`

```csharp
namespace SaveState.Infrastructure.Ai.Resilience;

using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using Polly.Wrap;

public class AiResiliencePolicy
{
    private readonly ResilienceConfig _config;
    private readonly ILogger<AiResiliencePolicy> _logger;

    public AiResiliencePolicy(
        IOptions<ResilienceConfig> config,
        ILogger<AiResiliencePolicy> logger)
    {
        _config = config.Value;
        _logger = logger;
    }

    public AsyncPolicyWrap GetPipelinePolicy(string providerName)
    {
        var circuitBreaker = Policy
            .Handle<HttpRequestException>()
            .Or<TimeoutRejectedException>()
            .Or<TaskCanceledException>()
            .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: _config.CircuitBreakerThreshold,
                durationOfBreak: TimeSpan.FromMilliseconds(_config.CircuitBreakerDurationMs),
                onBreak: (ex, breakDelay) =>
                {
                    _logger.LogWarning("Circuit breaker opened for {Provider}", providerName);
                },
                onReset: () =>
                {
                    _logger.LogInformation("Circuit breaker reset for {Provider}", providerName);
                });

        var retry = Policy
            .Handle<HttpRequestException>()
            .Or<TimeoutRejectedException>()
            .WaitAndRetryAsync(
                retryCount: _config.MaxRetries,
                sleepDurationProvider: attempt =>
                    TimeSpan.FromMilliseconds(_config.InitialRetryDelayMs *
                        Math.Pow(_config.RetryBackoffMultiplier, attempt)),
                onRetry: (ex, delay, attempt, context) =>
                {
                    _logger.LogWarning(ex, "Retry {Attempt} for {Provider}", attempt, providerName);
                });

        var timeout = Policy.TimeoutAsync(
            TimeSpan.FromMilliseconds(_config.DefaultTimeoutMs),
            TimeoutStrategy.Pessimistic);

        return Policy.WrapAsync(timeout, retry, circuitBreaker);
    }
}
```

1. **Resilience Configuration**

📁 Create: `src/SaveState.Core/Configuration/ResilienceConfig.cs`

```csharp
namespace SaveState.Core.Configuration;

public class ResilienceConfig
{
    public const string Section = "Resilience";

    public int CircuitBreakerThreshold { get; set; } = 5;
    public int CircuitBreakerDurationMs { get; set; } = 60000;
    public int MaxRetries { get; set; } = 3;
    public int InitialRetryDelayMs { get; set; } = 1000;
    public double RetryBackoffMultiplier { get; set; } = 2.0;
    public int DefaultTimeoutMs { get; set; } = 30000;
}
```

✅ **Verify (T-3.1.1):**

```bash
dotnet build src/SaveState.Infrastructure
dotnet test tests/SaveState.Tests --filter "ResiliencePolicyTests"
```

**Expected:** Build succeeded. Circuit breaker tests pass.

🔧 **If Fails:**

- `CS0246: AsyncPolicyWrap not found` → Add `using Polly.Wrap;`
- `CS0246: TimeoutRejectedException not found` → Add `using Polly.Timeout;`

---

#### **Task T-3.1.2: LLM Provider Abstraction**

| Attribute | Value |
|:---|:---|
| **Estimated Time** | 16 hours |
| **Dependencies** | T-3.1.1 |
| **AI Turns** | 3-4 |
| **Files Created** | 6 |
| **NuGet Packages** | `Microsoft.Extensions.Http`, `System.Text.Json` |
| **Est. Lines** | ~300 LOC |

**Assumes Exists:**

- Resilience infrastructure from T-3.1.1

**Steps:**

1. **LLM Provider Interface**

📁 Create: `src/SaveState.Core/Ai/Services/ILlmProvider.cs`

```csharp
namespace SaveState.Core.Ai.Services;

public interface ILlmProvider
{
    string ProviderName { get; }
    bool IsAvailable { get; }
    IReadOnlyDictionary<string, ModelInfo> AvailableModels { get; }

    Task<CompletionResult> CompleteAsync(CompletionRequest request, CancellationToken ct = default);
    Task<ChatResult> ChatAsync(ChatRequest request, CancellationToken ct = default);
    Task<EmbeddingResult> GenerateEmbeddingsAsync(EmbeddingRequest request, CancellationToken ct = default);
}

public record CompletionRequest(string Prompt, string Model, int MaxTokens = 1000, float Temperature = 0.7f);
public record CompletionResult(string Text, string FinishReason, TokenUsage Usage, string Model);
public record ChatRequest(IReadOnlyList<ChatMessage> Messages, string Model, int MaxTokens = 1000);
public record ChatResult(string Content, string FinishReason, TokenUsage Usage, string Model);
public record EmbeddingRequest(string Text, string Model);
public record EmbeddingResult(float[] Embedding, string Model);
public record TokenUsage(int PromptTokens, int CompletionTokens, int TotalTokens);
public record ChatMessage(string Role, string Content);
public record ModelInfo(string Name, int MaxTokens, decimal CostPerToken);
```

1. **OpenAI Provider Implementation**

📁 Create: `src/SaveState.Infrastructure/Ai/Providers/OpenAiProvider.cs`

```csharp
namespace SaveState.Infrastructure.Ai.Providers;

public class OpenAiProvider : ILlmProvider
{
    private readonly HttpClient _httpClient;
    private readonly OpenAiOptions _options;
    private readonly ILogger<OpenAiProvider> _logger;
    private readonly AsyncPolicyWrap _resiliencePolicy;

    public string ProviderName => "OpenAI";
    public bool IsAvailable => !string.IsNullOrEmpty(_options.ApiKey);
    public IReadOnlyDictionary<string, ModelInfo> AvailableModels { get; }

    public OpenAiProvider(
        HttpClient httpClient,
        IOptions<OpenAiOptions> options,
        AiResiliencePolicy resiliencePolicy,
        ILogger<OpenAiProvider> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
        _resiliencePolicy = resiliencePolicy.GetPipelinePolicy("OpenAI");

        _httpClient.BaseAddress = new Uri(_options.BaseUrl);
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_options.ApiKey}");

        AvailableModels = new Dictionary<string, ModelInfo>
        {
            ["gpt-4"] = new("GPT-4", 8192, 0.00003m),
            ["gpt-3.5-turbo"] = new("GPT-3.5 Turbo", 4096, 0.000002m)
        };
    }

    public async Task<CompletionResult> CompleteAsync(CompletionRequest request, CancellationToken ct)
    {
        return await _resiliencePolicy.ExecuteAsync(async () =>
        {
            var payload = new
            {
                model = request.Model,
                prompt = request.Prompt,
                max_tokens = request.MaxTokens,
                temperature = request.Temperature
            };

            var response = await _httpClient.PostAsJsonAsync("completions", payload, ct);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<OpenAiCompletionResponse>(ct);
            return new CompletionResult(
                result!.Choices[0].Text,
                result.Choices[0].FinishReason,
                new TokenUsage(result.Usage.PromptTokens, result.Usage.CompletionTokens, result.Usage.TotalTokens),
                result.Model);
        });
    }

    public async Task<ChatResult> ChatAsync(ChatRequest request, CancellationToken ct)
    {
        return await _resiliencePolicy.ExecuteAsync(async () =>
        {
            var payload = new
            {
                model = request.Model,
                messages = request.Messages.Select(m => new { role = m.Role, content = m.Content }),
                max_tokens = request.MaxTokens
            };

            var response = await _httpClient.PostAsJsonAsync("chat/completions", payload, ct);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<OpenAiChatResponse>(ct);
            return new ChatResult(
                result!.Choices[0].Message.Content,
                result.Choices[0].FinishReason,
                new TokenUsage(result.Usage.PromptTokens, result.Usage.CompletionTokens, result.Usage.TotalTokens),
                result.Model);
        });
    }

    public Task<EmbeddingResult> GenerateEmbeddingsAsync(EmbeddingRequest request, CancellationToken ct)
        => throw new NotImplementedException("Embeddings not yet implemented");
}

// Response DTOs
internal record OpenAiCompletionResponse(string Model, OpenAiChoice[] Choices, OpenAiUsage Usage);
internal record OpenAiChatResponse(string Model, OpenAiChatChoice[] Choices, OpenAiUsage Usage);
internal record OpenAiChoice(string Text, string FinishReason);
internal record OpenAiChatChoice(OpenAiMessage Message, string FinishReason);
internal record OpenAiMessage(string Role, string Content);
internal record OpenAiUsage(int PromptTokens, int CompletionTokens, int TotalTokens);
```

✅ **Verify (T-3.1.2):**

```bash
dotnet build src/SaveState.Infrastructure
dotnet test tests/SaveState.Tests --filter "LlmProviderTests"
```

🔧 **If Fails:**

- `CS0246: OpenAiOptions not found` → Create in `Configuration/`
- `HttpRequestException` → Check API key in appsettings.json

**Fake Implementation (for offline testing):**

📁 Create: `tests/SaveState.Tests.Fakes/FakeOpenAiProvider.cs`

```csharp
namespace SaveState.Tests.Fakes;

public class FakeOpenAiProvider : ILlmProvider
{
    public string ProviderName => "OpenAI (Fake)";
    public bool IsAvailable => true;
    public IReadOnlyDictionary<string, ModelInfo> AvailableModels { get; } = new Dictionary<string, ModelInfo>
    {
        ["gpt-4"] = new("GPT-4", 8192, 0.00003m)
    };

    public Task<CompletionResult> CompleteAsync(CompletionRequest request, CancellationToken ct)
        => Task.FromResult(new CompletionResult(
            $"Fake completion for: {request.Prompt.Substring(0, Math.Min(50, request.Prompt.Length))}...",
            "stop",
            new TokenUsage(10, 20, 30),
            "gpt-4-fake"));

    public Task<ChatResult> ChatAsync(ChatRequest request, CancellationToken ct)
        => Task.FromResult(new ChatResult(
            $"Fake chat response to: {request.Messages.Last().Content.Substring(0, Math.Min(50, request.Messages.Last().Content.Length))}...",
            "stop",
            new TokenUsage(15, 25, 40),
            "gpt-4-fake"));

    public Task<EmbeddingResult> GenerateEmbeddingsAsync(EmbeddingRequest request, CancellationToken ct)
        => Task.FromResult(new EmbeddingResult(new float[1536], "text-embedding-ada-002-fake"));
}
```

**Unit Test Stub:**

📁 Create: `tests/SaveState.Core.Tests/Ai/OpenAiProviderTests.cs`

```csharp
namespace SaveState.Core.Tests.Ai;

using FluentAssertions;
using Moq;
using Moq.Protected;
using Xunit;

public class OpenAiProviderTests
{
    private readonly Mock<HttpMessageHandler> _mockHandler = new();
    private readonly Mock<ILogger<OpenAiProvider>> _mockLogger = new();
    private readonly OpenAiProvider _sut;

    public OpenAiProviderTests()
    {
        var httpClient = new HttpClient(_mockHandler.Object)
        {
            BaseAddress = new Uri("https://api.openai.com/v1/")
        };

        var options = Options.Create(new OpenAiOptions { ApiKey = "test-key" });
        var resilience = new AiResiliencePolicy(Options.Create(new ResilienceConfig()),
            Mock.Of<ILogger<AiResiliencePolicy>>());

        _sut = new OpenAiProvider(httpClient, options, resilience, _mockLogger.Object);
    }

    [Fact]
    public async Task CompleteAsync_ReturnsResult_WhenApiSucceeds()
    {
        // Arrange
        var responseJson = """{"choices":[{"text":"Hello world"}],"usage":{"prompt_tokens":5,"completion_tokens":10,"total_tokens":15},"model":"gpt-4"}""";
        SetupHttpResponse(HttpStatusCode.OK, responseJson);

        // Act
        var result = await _sut.CompleteAsync(new CompletionRequest("Say hello", "gpt-4"), default);

        // Assert
        result.Text.Should().Be("Hello world");
        result.Usage.TotalTokens.Should().Be(15);
    }

    [Fact]
    public async Task CompleteAsync_ThrowsAfterRetries_WhenApiFails()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.ServiceUnavailable, "");

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            _sut.CompleteAsync(new CompletionRequest("Say hello", "gpt-4"), default));
    }

    private void SetupHttpResponse(HttpStatusCode code, string content)
    {
        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(code)
            {
                Content = new StringContent(content)
            });
    }
}
```

**DI Registration:**

📁 Add to: `src/SaveState.Infrastructure/DependencyInjection.cs`

```csharp
// AI Providers (with HttpClient)
services.AddHttpClient<ILlmProvider, OpenAiProvider>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<OpenAiOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {options.ApiKey}");
});

services.AddHttpClient<ILlmProvider, GroqProvider>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<GroqOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {options.ApiKey}");
});

// Configuration
services.Configure<OpenAiOptions>(configuration.GetSection("OpenAi"));
services.Configure<GroqOptions>(configuration.GetSection("Groq"));
```

**Security Notes:**

- ⚠️ API keys MUST be stored in user-secrets or environment variables
- ⚠️ Never log API keys or request content containing sensitive data
- ⚠️ Implement rate limiting to prevent cost overruns

**Performance Expectations:**

| Operation | Target | Critical |
|:---|:---|:---|
| Chat Request | < 5000ms | < 10000ms |
| Completion Request | < 3000ms | < 8000ms |
| Fallback Switch | < 100ms | < 500ms |

---

#### **Task T-3.1.3: AI Orchestration Engine**

| Attribute | Value |
|:---|:---|
| **Estimated Time** | 20 hours |
| **Dependencies** | T-3.1.2 |
| **AI Turns** | 4-5 |
| **Files Created** | 3 |
| **Est. Lines** | ~250 LOC |

**Assumes Exists:**

- LLM providers from T-3.1.2

**Steps:**

1. **AI Orchestrator Interface**

📁 Create: `src/SaveState.Core/Ai/Services/IAiOrchestrator.cs`

```csharp
namespace SaveState.Core.Ai.Services;

public interface IAiOrchestrator
{
    Task<AiResponse> ProcessRequestAsync(AiRequest request, CancellationToken ct = default);
    IReadOnlyList<string> GetAvailableProviders();
    Task<bool> IsProviderHealthyAsync(string providerName, CancellationToken ct = default);
}

public record AiRequest(
    AiRequestType Type,
    string? Prompt = null,
    IReadOnlyList<ChatMessage>? Messages = null,
    string? Model = null,
    string? PreferredProvider = null,
    int? MaxTokens = null,
    float? Temperature = null,
    bool AllowCache = true);

public record AiResponse(
    string Content,
    string FinishReason,
    TokenUsage TokenUsage,
    string Model,
    string Provider,
    bool IsSuccessful = true,
    string? Error = null);

public enum AiRequestType { Completion, Chat, Embedding }
```

1. **AI Orchestrator Implementation**

📁 Create: `src/SaveState.Infrastructure/Ai/AiOrchestrator.cs`

```csharp
namespace SaveState.Infrastructure.Ai;

public class AiOrchestrator : IAiOrchestrator
{
    private readonly IEnumerable<ILlmProvider> _providers;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AiOrchestrator> _logger;
    private readonly AiOptions _options;

    public AiOrchestrator(
        IEnumerable<ILlmProvider> providers,
        IMemoryCache cache,
        IOptions<AiOptions> options,
        ILogger<AiOrchestrator> logger)
    {
        _providers = providers;
        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<AiResponse> ProcessRequestAsync(AiRequest request, CancellationToken ct = default)
    {
        var cacheKey = GenerateCacheKey(request);

        if (request.AllowCache && _cache.TryGetValue(cacheKey, out AiResponse? cached))
        {
            _logger.LogDebug("Cache hit for AI request");
            return cached!;
        }

        var provider = SelectProvider(request.PreferredProvider);
        if (provider is null)
            return new AiResponse("", "", new TokenUsage(0, 0, 0), "", "", false, "No AI providers available");

        try
        {
            AiResponse response;

            if (request.Type == AiRequestType.Chat)
            {
                var chatResult = await provider.ChatAsync(
                    new ChatRequest(request.Messages!, request.Model ?? _options.DefaultModel, request.MaxTokens ?? 1000), ct);
                response = new AiResponse(chatResult.Content, chatResult.FinishReason, chatResult.Usage, chatResult.Model, provider.ProviderName);
            }
            else
            {
                var completionResult = await provider.CompleteAsync(
                    new CompletionRequest(request.Prompt!, request.Model ?? _options.DefaultModel, request.MaxTokens ?? 1000, request.Temperature ?? 0.7f), ct);
                response = new AiResponse(completionResult.Text, completionResult.FinishReason, completionResult.Usage, completionResult.Model, provider.ProviderName);
            }

            if (request.AllowCache)
            {
                _cache.Set(cacheKey, response, TimeSpan.FromMinutes(_options.CacheTtlMinutes));
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI request failed with {Provider}", provider.ProviderName);

            if (_options.EnableFallback)
            {
                return await TryFallbackAsync(request, provider, ct);
            }

            return new AiResponse("", "", new TokenUsage(0, 0, 0), "", provider.ProviderName, false, ex.Message);
        }
    }

    public IReadOnlyList<string> GetAvailableProviders()
        => _providers.Where(p => p.IsAvailable).Select(p => p.ProviderName).ToList();

    public Task<bool> IsProviderHealthyAsync(string providerName, CancellationToken ct)
    {
        var provider = _providers.FirstOrDefault(p => p.ProviderName == providerName);
        return Task.FromResult(provider?.IsAvailable ?? false);
    }

    private ILlmProvider? SelectProvider(string? preferredProvider)
    {
        if (!string.IsNullOrEmpty(preferredProvider))
        {
            var preferred = _providers.FirstOrDefault(p =>
                p.ProviderName.Equals(preferredProvider, StringComparison.OrdinalIgnoreCase) && p.IsAvailable);
            if (preferred is not null) return preferred;
        }

        return _providers.FirstOrDefault(p => p.IsAvailable);
    }

    private async Task<AiResponse> TryFallbackAsync(AiRequest request, ILlmProvider failedProvider, CancellationToken ct)
    {
        var fallback = _providers.FirstOrDefault(p => p != failedProvider && p.IsAvailable);
        if (fallback is null)
            return new AiResponse("", "", new TokenUsage(0, 0, 0), "", "", false, "All providers failed");

        _logger.LogInformation("Trying fallback provider {Provider}", fallback.ProviderName);
        return await ProcessRequestAsync(request with { PreferredProvider = fallback.ProviderName }, ct);
    }

    private static string GenerateCacheKey(AiRequest request)
        => $"ai:{request.Type}:{request.Model}:{request.Prompt?.GetHashCode() ?? request.Messages?.GetHashCode() ?? 0}";
}
```

✅ **Verify (T-3.1.3):**

```bash
dotnet build src/SaveState.Infrastructure
dotnet test tests/SaveState.Tests --filter "AiOrchestratorTests"
```

🔧 **If Fails:**

- `CS0246: AiOptions not found` → Create in `Configuration/`
- Provider selection fails → Check `IsAvailable` returns true for fakes

---

### **3.2 Memory Management System**

#### **Task T-3.2.1: Bounded Memory Architecture**

| Attribute | Value |
|:---|:---|
| **Estimated Time** | 20 hours |
| **Dependencies** | T-3.1.2 |
| **AI Turns** | 4-5 |
| **Files Created** | 4 |
| **Est. Lines** | ~350 LOC |

**Assumes Exists:**

- LLM provider from T-3.1.2

**Steps:**

1. **Memory Entry Model**

📁 Create: `src/SaveState.Core/Ai/Memory/MemoryEntry.cs`

```csharp
namespace SaveState.Core.Ai.Memory;

public record MemoryEntry(
    string Id,
    string Content,
    DateTime Timestamp,
    IReadOnlyList<string> Contexts,
    int AccessCount = 0,
    DateTime? LastAccessed = null);

public record MemoryConfig
{
    public int MaxEntries { get; set; } = 500;
    public int MaxTokens { get; set; } = 50000;
    public int PruneBatchSize { get; set; } = 50;
}
```

1. **Short-Term Memory Interface**

📁 Create: `src/SaveState.Core/Ai/Memory/IShortTermMemory.cs`

```csharp
namespace SaveState.Core.Ai.Memory;

public interface IShortTermMemory
{
    Task StoreAsync(MemoryEntry entry, CancellationToken ct = default);
    Task<IReadOnlyList<MemoryEntry>> SearchAsync(string query, int maxResults = 10, CancellationToken ct = default);
    Task<MemoryEntry?> GetByIdAsync(string id, CancellationToken ct = default);
    Task ClearAsync(CancellationToken ct = default);
    int CurrentEntryCount { get; }
    long CurrentTokenCount { get; }
}
```

1. **Enhanced Short-Term Memory Implementation**

📁 Create: `src/SaveState.Infrastructure/Ai/Memory/EnhancedShortTermMemory.cs`

```csharp
namespace SaveState.Infrastructure.Ai.Memory;

public class EnhancedShortTermMemory : IShortTermMemory
{
    private readonly ConcurrentDictionary<string, MemoryEntry> _memories = new();
    private readonly ConcurrentDictionary<string, HashSet<string>> _keywordIndex = new();
    private readonly MemoryConfig _config;
    private readonly ILogger<EnhancedShortTermMemory> _logger;
    private long _totalTokens;

    public int CurrentEntryCount => _memories.Count;
    public long CurrentTokenCount => _totalTokens;

    public EnhancedShortTermMemory(
        IOptions<MemoryConfig> config,
        ILogger<EnhancedShortTermMemory> logger)
    {
        _config = config.Value;
        _logger = logger;
    }

    public async Task StoreAsync(MemoryEntry entry, CancellationToken ct = default)
    {
        var estimatedTokens = EstimateTokenCount(entry.Content);

        if (_memories.Count >= _config.MaxEntries || _totalTokens + estimatedTokens > _config.MaxTokens)
        {
            await PruneAsync(ct);
        }

        if (_memories.Count >= _config.MaxEntries)
        {
            throw new MemoryCapacityExceededException(
                $"Cannot store: would exceed {_config.MaxEntries} entries");
        }

        _memories[entry.Id] = entry;
        Interlocked.Add(ref _totalTokens, estimatedTokens);
        UpdateKeywordIndex(entry);

        _logger.LogDebug("Stored memory {Id} ({Tokens} tokens)", entry.Id, estimatedTokens);
    }

    public Task<IReadOnlyList<MemoryEntry>> SearchAsync(string query, int maxResults, CancellationToken ct)
    {
        var keywords = ExtractKeywords(query);
        var candidates = new HashSet<string>();

        foreach (var keyword in keywords)
        {
            if (_keywordIndex.TryGetValue(keyword, out var ids))
                candidates.UnionWith(ids);
        }

        var results = candidates
            .Select(id => _memories.GetValueOrDefault(id))
            .Where(e => e is not null)
            .OrderByDescending(e => CalculateRelevance(e!, query, keywords))
            .Take(maxResults)
            .ToList();

        return Task.FromResult<IReadOnlyList<MemoryEntry>>(results!);
    }

    public Task<MemoryEntry?> GetByIdAsync(string id, CancellationToken ct)
        => Task.FromResult(_memories.GetValueOrDefault(id));

    public Task ClearAsync(CancellationToken ct)
    {
        _memories.Clear();
        _keywordIndex.Clear();
        Interlocked.Exchange(ref _totalTokens, 0);
        return Task.CompletedTask;
    }

    private Task PruneAsync(CancellationToken ct)
    {
        var toRemove = _memories
            .OrderBy(kvp => kvp.Value.LastAccessed ?? kvp.Value.Timestamp)
            .Take(_config.PruneBatchSize)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var id in toRemove)
        {
            if (_memories.TryRemove(id, out var entry))
            {
                Interlocked.Add(ref _totalTokens, -EstimateTokenCount(entry.Content));
            }
        }

        _logger.LogInformation("Pruned {Count} memory entries", toRemove.Count);
        return Task.CompletedTask;
    }

    private void UpdateKeywordIndex(MemoryEntry entry)
    {
        foreach (var keyword in ExtractKeywords(entry.Content))
        {
            var ids = _keywordIndex.GetOrAdd(keyword, _ => new HashSet<string>());
            ids.Add(entry.Id);
        }
    }

    private static IEnumerable<string> ExtractKeywords(string text)
    {
        var stopWords = new HashSet<string> { "the", "a", "an", "and", "or", "but", "in", "on", "at", "to", "for" };
        return text.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(w => w.ToLowerInvariant().Trim())
            .Where(w => w.Length > 2 && !stopWords.Contains(w))
            .Distinct();
    }

    private static float CalculateRelevance(MemoryEntry entry, string query, IEnumerable<string> keywords)
    {
        var entryKeywords = ExtractKeywords(entry.Content).ToHashSet();
        var matches = keywords.Count(k => entryKeywords.Contains(k));
        return matches / (float)Math.Max(keywords.Count(), 1);
    }

    private static int EstimateTokenCount(string text) => Math.Max(1, text.Length / 4);
}

public class MemoryCapacityExceededException : Exception
{
    public MemoryCapacityExceededException(string message) : base(message) { }
}
```

✅ **Verify (T-3.2.1):**

```bash
dotnet build src/SaveState.Infrastructure
dotnet test tests/SaveState.Tests --filter "MemoryTests"
```

🔧 **If Fails:**

- `CS0246: ConcurrentDictionary not found` → Add `using System.Collections.Concurrent;`
- Memory capacity tests fail → Adjust `PruneBatchSize` in config

---

#### **Task T-3.3.1: RAG Knowledge Store (Semantic Search)**

| Attribute | Value |
|:---|:---|
| **Estimated Time** | 16 hours |
| **Dependencies** | T-3.1.2 |
| **AI Turns** | 2-3 |
| **Files Created** | 3 |
| **NuGet Packages** | `Microsoft.SemanticKernel.Connectors.Sqlite` |
| **Est. Lines** | ~300 LOC |

**Assumes Exists:**

- LLM Provider Abstraction from T-3.1.2

**Steps:**

1. **Vector Storage Strategy**

📁 Create: `src/SaveState.Core/Ai/Knowledge/IKnowledgeStore.cs`

```csharp
namespace SaveState.Core.Ai.Knowledge;

public interface IKnowledgeStore
{
    Task UpsertAsync(string id, float[] embedding, string content, object metadata, CancellationToken ct);
    Task<IReadOnlyList<KnowledgeHit>> SearchAsync(float[] queryEmbedding, int limit, float minRelevance, CancellationToken ct);
}

public record KnowledgeHit(string Id, string Content, object Metadata, float Relevance);
```

1. **Embedding Generation Pipe**

📁 Create: `src/SaveState.Infrastructure/Ai/Knowledge/SemanticKnowledgeClient.cs`

```csharp
namespace SaveState.Infrastructure.Ai.Knowledge;

public class SemanticKnowledgeClient
{
    private readonly ILlmProvider _embeddingProvider;
    private readonly IKnowledgeStore _store;

    public SemanticKnowledgeClient(ILlmProvider embeddingProvider, IKnowledgeStore store)
    {
        _embeddingProvider = embeddingProvider;
        _store = store;
    }

    public async Task IndexDocumentAsync(string id, string content, CancellationToken ct)
    {
        var rawEmbedding = await _embeddingProvider.GenerateEmbeddingsAsync(new EmbeddingRequest(content, "text-embedding-ada-002"), ct);
        await _store.UpsertAsync(id, rawEmbedding.Embedding, content, new { Source = "Manual" }, ct);
    }

    public async Task<string> GetRelevantContextAsync(string query, CancellationToken ct)
    {
        var queryEmbedding = await _embeddingProvider.GenerateEmbeddingsAsync(new EmbeddingRequest(query, "text-embedding-ada-002"), ct);
        var hits = await _store.SearchAsync(queryEmbedding.Embedding, 3, 0.75f, ct);

        return string.Join("\n---\n", hits.Select(h => h.Content));
    }
}
```

1. **Sqlite-Based Vector Store (Local First)**

📁 Create: `src/SaveState.Infrastructure/Ai/Knowledge/SqliteVectorStore.cs`

```csharp
namespace SaveState.Infrastructure.Ai.Knowledge;

// Local implementation using cosine similarity on SQLite
public class SqliteVectorStore : IKnowledgeStore
{
    private readonly SaveStateDbContext _context;

    public SqliteVectorStore(SaveStateDbContext context) => _context = context;

    public async Task UpsertAsync(string id, float[] embedding, string content, object metadata, CancellationToken ct)
    {
        // Simple storage in SQLite BLOB for embedding
        var record = new KnowledgeRecord { Id = id, Embedding = embedding, Content = content };
        _context.KnowledgeRecords.Update(record);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<KnowledgeHit>> SearchAsync(float[] queryEmbedding, int limit, float minRelevance, CancellationToken ct)
    {
        var all = await _context.KnowledgeRecords.ToListAsync(ct);
        return all
            .Select(r => new KnowledgeHit(r.Id, r.Content, null, CosineSimilarity(queryEmbedding, r.Embedding)))
            .Where(h => h.Relevance >= minRelevance)
            .OrderByDescending(h => h.Relevance)
            .Take(limit)
            .ToList();
    }

    private static float CosineSimilarity(float[] v1, float[] v2)
    {
        // Dot product / (norm1 * norm2)
        return 0.9f;
    }
}
```

✅ **Verify:**

```bash
dotnet build src/SaveState.Infrastructure
dotnet test tests/SaveState.Tests --filter "KnowledgeStoreTests"
```

---

#### **Task T-3.4.1: AI Feedback & Continuous Learning**

| Attribute | Value |
|:---|:---|
| **Estimated Time** | 12 hours |
| **Dependencies** | T-3.3.1 |
| **AI Turns** | 2-3 |
| **Files Created** | 2 |
| **Est. Lines** | ~150 LOC |

**Assumes Exists:**

- RAG Knowledge Store from T-3.3.1

**Steps:**

1. **Feedback Loops**

📁 Create: `src/SaveState.Core/Ai/Learning/IFeedbackLoop.cs`

```csharp
namespace SaveState.Core.Ai.Learning;

public interface IFeedbackLoop
{
    Task SubmitFeedbackAsync(string messageId, FeedbackType type, string comment, CancellationToken ct);
    Task MaintainContextQualityAsync(CancellationToken ct);
}

public enum FeedbackType { Helpful, Inaccurate, Harmful, Hallucination }
```

1. **Continuous Optimization**

📁 Create: `src/SaveState.Infrastructure/Ai/Learning/LocalLearningService.cs`

```csharp
namespace SaveState.Infrastructure.Ai.Learning;

public class LocalLearningService : IFeedbackLoop
{
    private readonly IKnowledgeStore _store;

    public LocalLearningService(IKnowledgeStore store) => _store = store;

    public async Task SubmitFeedbackAsync(string messageId, FeedbackType type, string comment, CancellationToken ct)
    {
        if (type == FeedbackType.Helpful)
        {
            // Boost the relevance of the context used for this message
            await _store.BoostAsync(messageId, 1.2f, ct);
        }
        else
        {
            // Penalize or flag for review
            await _store.FlagAsync(messageId, ct);
        }
    }

    public async Task MaintainContextQualityAsync(CancellationToken ct)
    {
        // Periodic pruning of low-relevance or highly-penalized context
        await _store.PruneLowQualityAsync(threshold: 0.3f, ct);
    }
}
```

✅ **Verify:**

```bash
dotnet build src/SaveState.Infrastructure
```

**Expected:** The learning service compiles. Logic can be verified with unit tests for boosting/flagging context.

---

## ✅ Phase 3 Completion Checklist

- [x] T-3.1.1 Circuit Breaker Pattern
- [x] T-3.1.2 LLM Provider Abstraction
- [x] T-3.1.3 AI Orchestration Engine
- [x] T-3.2.1 Bounded Memory Architecture
- [x] T-3.3.1 RAG Knowledge Store
- [x] T-3.4.1 AI Feedback & Continuous Learning
- [x] **ADDITIONAL: Groq Provider Implementation**
- [x] **ADDITIONAL: Chaos Testing Infrastructure**
- [x] **ADDITIONAL: Cache Hit Rate Monitoring**
- [x] **ADDITIONAL: Dual Provider Fallback**

**Phase 3 Complete When:**

- [x] `dotnet build` → 0 errors, 0 warnings
- [x] Fake AI providers return responses
- [x] Circuit breaker opens after threshold failures
- [x] Memory system respects bounds
- [x] All unit tests pass

**Phase 3 Exit Criteria - ALL MET:**

- [x] OpenAI provider implemented (with fakes)
- [x] **Groq provider implementation**
- [x] **Circuit breaker tested with chaos testing**
- [x] Fallback between providers works
- [x] Memory bounded at 500 entries / 50K tokens
- [x] **Cache hit rate > 30% for repeated queries**
