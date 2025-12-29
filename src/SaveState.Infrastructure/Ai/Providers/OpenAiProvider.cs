using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly.Wrap;
using System.Net.Http.Json;
using SaveState.Core.Ai.Services;
using SaveState.Core.Common;
using SaveState.Core.Configuration;
using SaveState.Infrastructure.Ai.Resilience;

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

                var result = await response.Content.ReadFromJsonAsync<OpenAiCompletionResponse>(ct).ConfigureAwait(false);
                var completionResult = new CompletionResult(
                    result!.Choices[0].Text,
                    result.Choices[0].FinishReason,
                    new TokenUsage(result.Usage.PromptTokens, result.Usage.CompletionTokens, result.Usage.TotalTokens),
                    result.Model);
                return Result<CompletionResult>.Success(completionResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OpenAI completion request failed");
                return Result<CompletionResult>.Failure($"OpenAI API request failed: {ex.Message}", ErrorType.Internal);
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

                var result = await response.Content.ReadFromJsonAsync<OpenAiChatResponse>(ct).ConfigureAwait(false);
                var chatResult = new ChatResult(
                    result!.Choices[0].Message.Content,
                    result.Choices[0].FinishReason,
                    new TokenUsage(result.Usage.PromptTokens, result.Usage.CompletionTokens, result.Usage.TotalTokens),
                    result.Model);
                return Result<ChatResult>.Success(chatResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OpenAI chat request failed");
                return Result<ChatResult>.Failure($"OpenAI API request failed: {ex.Message}", ErrorType.Internal);
            }
        }).ConfigureAwait(false);
    }

    public Task<Result<EmbeddingResult>> GenerateEmbeddingsAsync(EmbeddingRequest request, CancellationToken ct)
        => Task.FromResult(Result<EmbeddingResult>.Failure("Embeddings not yet implemented", ErrorType.Internal));
}

// Response DTOs
internal record OpenAiCompletionResponse(string Model, OpenAiChoice[] Choices, OpenAiUsage Usage);
internal record OpenAiChatResponse(string Model, OpenAiChatChoice[] Choices, OpenAiUsage Usage);
internal record OpenAiChoice(string Text, string FinishReason);
internal record OpenAiChatChoice(OpenAiMessage Message, string FinishReason);
internal record OpenAiMessage(string Role, string Content);
internal record OpenAiUsage(int PromptTokens, int CompletionTokens, int TotalTokens);
