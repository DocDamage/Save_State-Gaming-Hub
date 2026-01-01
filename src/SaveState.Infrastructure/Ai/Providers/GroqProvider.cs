using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly.Wrap;
using System.Net.Http.Json;
using SaveState.Core.Ai.Services;
using SaveState.Core.Common;
using SaveState.Core.Configuration;
using SaveState.Infrastructure.Ai.Resilience;

namespace SaveState.Infrastructure.Ai.Providers;

public class GroqProvider : ILlmProvider
{
    private readonly HttpClient _httpClient;
    private readonly GroqOptions _options;
    private readonly ILogger<GroqProvider> _logger;
    private readonly AsyncPolicyWrap _resiliencePolicy;

    public string ProviderName => "Groq";
    public bool IsAvailable => !string.IsNullOrEmpty(_options.ApiKey);
    public IReadOnlyDictionary<string, ModelInfo> AvailableModels { get; }

    public GroqProvider(
        HttpClient httpClient,
        IOptions<GroqOptions> options,
        IAiResiliencePolicy resiliencePolicy,
        ILogger<GroqProvider> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
        _resiliencePolicy = resiliencePolicy.GetPipelinePolicy("Groq");

        _httpClient.BaseAddress = new Uri(_options.BaseUrl);
        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _options.ApiKey);

        AvailableModels = new Dictionary<string, ModelInfo>
        {
            ["mixtral-8x7b-32768"] = new("Mixtral 8x7B", 32768, 0.00000027m),
            ["llama2-70b-4096"] = new("Llama 2 70B", 4096, 0.0000007m),
            ["gemma-7b-it"] = new("Gemma 7B IT", 8192, 0.00000007m)
        };
    }

    public async Task<Result<CompletionResult>> CompleteAsync(CompletionRequest request, CancellationToken ct)
    {
        return await _resiliencePolicy.ExecuteAsync(async () =>
        {
            try
            {
                var payload = new
                {
                    model = request.Model,
                    prompt = request.Prompt,
                    max_tokens = request.MaxTokens,
                    temperature = request.Temperature
                };

                var response = await _httpClient.PostAsJsonAsync("completions", payload, ct).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<GroqCompletionResponse>(ct).ConfigureAwait(false);
                var completionResult = new CompletionResult(
                    result!.Choices[0].Text,
                    result.Choices[0].FinishReason,
                    new TokenUsage(result.Usage.PromptTokens, result.Usage.CompletionTokens, result.Usage.TotalTokens),
                    result.Model);
                return Result<CompletionResult>.Success(completionResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Groq completion request failed");
                return Result<CompletionResult>.Failure($"Groq API request failed: {ex.Message}", ErrorType.Internal);
            }
        }).ConfigureAwait(false);
    }

    public async Task<Result<ChatResult>> ChatAsync(ChatRequest request, CancellationToken ct)
    {
        return await _resiliencePolicy.ExecuteAsync(async () =>
        {
            try
            {
                var payload = new
                {
                    model = request.Model,
                    messages = request.Messages.Select(m => new { role = m.Role, content = m.Content }),
                    max_tokens = request.MaxTokens
                };

                var response = await _httpClient.PostAsJsonAsync("chat/completions", payload, ct).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<GroqChatResponse>(ct).ConfigureAwait(false);
                var chatResult = new ChatResult(
                    result!.Choices[0].Message.Content,
                    result.Choices[0].FinishReason,
                    new TokenUsage(result.Usage.PromptTokens, result.Usage.CompletionTokens, result.Usage.TotalTokens),
                    result.Model);
                return Result<ChatResult>.Success(chatResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Groq chat request failed");
                return Result<ChatResult>.Failure($"Groq API request failed: {ex.Message}", ErrorType.Internal);
            }
        }).ConfigureAwait(false);
    }

    public Task<Result<EmbeddingResult>> GenerateEmbeddingsAsync(EmbeddingRequest request, CancellationToken ct)
        => Task.FromResult(Result<EmbeddingResult>.Failure("Embeddings not supported by Groq provider", ErrorType.Internal));
}

// Response DTOs
internal record GroqCompletionResponse(string Model, GroqChoice[] Choices, GroqUsage Usage);
internal record GroqChatResponse(string Model, GroqChatChoice[] Choices, GroqUsage Usage);
internal record GroqChoice(string Text, string FinishReason);
internal record GroqChatChoice(GroqMessage Message, string FinishReason);
internal record GroqMessage(string Role, string Content);
internal record GroqUsage(int PromptTokens, int CompletionTokens, int TotalTokens);
