using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using SaveState.Core.Interfaces;
using Serilog;

namespace SaveState.Core.Services;

/// <summary>
/// Embedding service using Google Gemini text-embedding-004 model
/// </summary>
public class EmbeddingService : IEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger = Log.ForContext<EmbeddingService>();
    private readonly string _model = "text-embedding-004";
    private string? _apiKey;

    // Gemini text-embedding-004 produces 768-dimensional vectors
    public int EmbeddingDimension => 768;
    public bool IsConfigured => !string.IsNullOrEmpty(_apiKey);

    public EmbeddingService(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("Gemini");
        _httpClient.BaseAddress = new Uri("https://generativelanguage.googleapis.com/v1beta/");
        _apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
    }

    public void SetApiKey(string apiKey)
    {
        _apiKey = apiKey;
    }

    public async Task<float[]> GetEmbeddingAsync(string text)
    {
        if (!IsConfigured)
        {
            _logger.Warning("Embedding service not configured - returning zero vector");
            return new float[EmbeddingDimension];
        }

        try
        {
            var requestBody = new
            {
                model = $"models/{_model}",
                content = new
                {
                    parts = new[] { new { text } }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                $"models/{_model}:embedContent?key={_apiKey}", 
                content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.Error("Gemini Embedding API Error: {Status} - {Error}", response.StatusCode, error);
                return new float[EmbeddingDimension];
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            return ParseEmbedding(responseJson);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to get embedding for text");
            return new float[EmbeddingDimension];
        }
    }

    public async Task<List<float[]>> GetEmbeddingsAsync(IEnumerable<string> texts)
    {
        var results = new List<float[]>();
        
        // Gemini doesn't have a batch endpoint, so we process sequentially
        // with some parallelism for performance
        var textList = texts.ToList();
        var semaphore = new SemaphoreSlim(5); // Max 5 concurrent requests
        var tasks = new List<Task<float[]>>();

        foreach (var text in textList)
        {
            await semaphore.WaitAsync();
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    return await GetEmbeddingAsync(text);
                }
                finally
                {
                    semaphore.Release();
                }
            }));
        }

        var embeddings = await Task.WhenAll(tasks);
        return embeddings.ToList();
    }

    private float[] ParseEmbedding(string jsonResponse)
    {
        try
        {
            using var doc = JsonDocument.Parse(jsonResponse);
            var root = doc.RootElement;

            // Navigate to embedding.values array
            if (root.TryGetProperty("embedding", out var embedding) &&
                embedding.TryGetProperty("values", out var values))
            {
                var result = new float[values.GetArrayLength()];
                int i = 0;
                foreach (var val in values.EnumerateArray())
                {
                    result[i++] = val.GetSingle();
                }
                return result;
            }
        }
        catch (JsonException ex)
        {
            _logger.Error(ex, "Failed to parse embedding response");
        }

        return new float[EmbeddingDimension];
    }

    /// <summary>
    /// Serialize embedding to bytes for database storage
    /// </summary>
    public static byte[] SerializeEmbedding(float[] embedding)
    {
        var bytes = new byte[embedding.Length * sizeof(float)];
        Buffer.BlockCopy(embedding, 0, bytes, 0, bytes.Length);
        return bytes;
    }

    /// <summary>
    /// Deserialize embedding from database bytes
    /// </summary>
    public static float[] DeserializeEmbedding(byte[] bytes)
    {
        var floats = new float[bytes.Length / sizeof(float)];
        Buffer.BlockCopy(bytes, 0, floats, 0, bytes.Length);
        return floats;
    }
}
