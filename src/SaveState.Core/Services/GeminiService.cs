using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using SaveState.Core.Entities;
using SaveState.Core.Interfaces;
using SaveState.Core.Models;
using Serilog;

namespace SaveState.Core.Services;

public class GeminiService : IAiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger = Log.ForContext<GeminiService>();
    private readonly string _model = "gemini-pro";
    private string? _apiKey;

    public bool IsConfigured => !string.IsNullOrEmpty(_apiKey);

    public GeminiService(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("Gemini");
        _httpClient.BaseAddress = new Uri("https://generativelanguage.googleapis.com/v1beta/");
        
        // Try to get API key from environment variable first, can be extended to settings later
        _apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
    }

    public void SetApiKey(string apiKey)
    {
        _apiKey = apiKey;
    }

    public async Task<string> ChatAsync(string message, IEnumerable<AiChatMessage>? history = null)
    {
        if (!IsConfigured)
            return "Gemini AI is not configured. Please add your Gemini API key in Settings/Environment.";

        try
        {
            var requestBody = BuildRequestBody(message, history);
            var response = await SendRequestAsync($"models/{_model}:generateContent?key={_apiKey}", requestBody);
            return ExtractContent(response);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Gemini chat failed");
            return $"I encountered an error connecting to Gemini: {ex.Message}";
        }
    }

    public async Task<IEnumerable<GameRecommendation>> GetRecommendationsAsync(IEnumerable<Game> libraryGames, int count = 5)
    {
        if (!IsConfigured) return Enumerable.Empty<GameRecommendation>();

        try
        {
            var gameList = string.Join(", ", libraryGames.Take(30).Select(g => g.Title));
            var prompt = $$"""
                Based on this game library: {{gameList}}
                
                Recommend {{count}} games the user might enjoy. For each game provide:
                - title: Game name
                - reason: Why they'd like it (2-3 sentences)
                - genre: Primary genre
                - similarTo: Which library games it's similar to
                
                Respond in JSON array format:
                [{"title":"...", "reason":"...", "genre":"...", "similarTo":["..."]}]
                """;

            var response = await ChatAsync(prompt);
            return ParseJsonArray<GameRecommendation>(response) ?? Enumerable.Empty<GameRecommendation>();
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to get recommendations from Gemini");
            return Enumerable.Empty<GameRecommendation>();
        }
    }

    public async Task<AiAnalysisResult> AnalyzeGameAsync(string gameTitle)
    {
        if (!IsConfigured) return new AiAnalysisResult { GameTitle = gameTitle };

        try
        {
            var prompt = $$"""
                Analyze the video game "{{gameTitle}}" and provide:
                1. A compelling 2-3 sentence description
                2. Primary genres (up to 3)
                3. Relevant tags (up to 5)
                4. Estimated age rating (E, E10+, T, M, AO)
                5. Overall sentiment (-1 to 1, where 1 is very positive)
                
                Respond in JSON format:
                {"description":"...", "genres":["..."], "tags":["..."], "ageRating":"...", "sentiment":0.8}
                """;

            var response = await ChatAsync(prompt);
            var result = ParseJsonObject(response);
            
             if (result.RootElement.ValueKind != JsonValueKind.Undefined)
             {
                 var root = result.RootElement;
                 return new AiAnalysisResult
                 {
                     GameTitle = gameTitle,
                     GeneratedDescription = root.TryGetProperty("description", out var desc) ? desc.GetString() ?? "" : "",
                     SuggestedGenres = root.TryGetProperty("genres", out var genres) 
                         ? genres.EnumerateArray().Select(g => g.GetString() ?? "").ToArray() 
                         : Array.Empty<string>(),
                     SuggestedTags = root.TryGetProperty("tags", out var tags)
                         ? tags.EnumerateArray().Select(t => t.GetString() ?? "").ToArray()
                         : Array.Empty<string>(),
                     AgeRating = root.TryGetProperty("ageRating", out var age) ? age.GetString() ?? "" : "",
                     SentimentScore = root.TryGetProperty("sentiment", out var sent) ? sent.GetDouble() : 0.5
                 };
             }
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to analyze game with Gemini");
        }

        return new AiAnalysisResult { GameTitle = gameTitle };
    }

    public async Task<IEnumerable<string>> FindSimilarGamesAsync(Game game, int count = 5)
    {
        if (!IsConfigured) return Enumerable.Empty<string>();

        var prompt = $"""
            List {count} games similar to "{game.Title}" that a fan would enjoy.
            Consider gameplay, genre, story themes, and atmosphere.
            Respond with just the game names, one per line.
            """;

        var response = await ChatAsync(prompt);
        return response.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim().TrimStart('-', '*', '•', ' '))
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Take(count);
    }

    public async Task<string> GetGameTipsAsync(string gameTitle)
    {
        if (!IsConfigured) return "Gemini AI is not configured.";

        var prompt = $"""
            Give me 5 helpful tips for playing "{gameTitle}".
            Include beginner advice and some advanced strategies.
            Keep each tip concise (1-2 sentences).
            """;

        return await ChatAsync(prompt);
    }

    private object BuildRequestBody(string message, IEnumerable<AiChatMessage>? history)
    {
        var contents = new List<object>();

        // System prompt is roughly emulated in Gemini by adding it as the first user part or model context if supported.
        // For simplicity with v1beta, we'll prepend to history or just rely on the prompt.
        // Adding a 'system' role instruction at start of chat is often effective.
        
        string systemInstruction = """
            You are a friendly gaming assistant for SaveState.
            You help users with game recommendations, tips, and technical help.
            When asked for structured data, return ONLY valid JSON.
            """;
            
        // Gemini structure: { role: "user"|"model", parts: [{ text: "..." }] }

        if (history != null)
        {
            foreach (var h in history)
            {
                string role = h.Role.ToLower() == "user" ? "user" : "model";
                contents.Add(new
                {
                    role = role,
                    parts = new[] { new { text = h.Content } }
                });
            }
        }
        else
        {
             // Add system instruction as part of first message or separate entry if needed.
             // We'll prepend it to the message if no history essentially.
             message = systemInstruction + "\n\n" + message;
        }

        contents.Add(new
        {
            role = "user",
            parts = new[] { new { text = message } }
        });

        return new
        {
            contents,
            generationConfig = new
            {
                temperature = 0.7,
                maxOutputTokens = 1000
            }
        };
    }

    private async Task<string> SendRequestAsync(string endpoint, object body)
    {
        var json = JsonSerializer.Serialize(body);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await _httpClient.PostAsync(endpoint, content);
        
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Gemini API Error: {response.StatusCode} - {error}");
        }
        
        return await response.Content.ReadAsStringAsync();
    }

    private string ExtractContent(string jsonResponse)
    {
        try 
        {
            using var doc = JsonDocument.Parse(jsonResponse);
            var root = doc.RootElement;
            
            // Navigate to candidates[0].content.parts[0].text
            if (root.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
            {
                var firstCandidate = candidates[0];
                if (firstCandidate.TryGetProperty("content", out var content) && 
                    content.TryGetProperty("parts", out var parts) && 
                    parts.GetArrayLength() > 0)
                {
                    return parts[0].GetProperty("text").GetString() ?? "";
                }
            }
            return "";
        }
        catch (JsonException)
        {
            return "";
        }
    }
    
    // Helpers for JSON parsing from Markdown code blocks often returned by LLMs
    private JsonDocument ParseJsonObject(string content)
    {
        content = CleanJsonString(content);
        return JsonDocument.Parse(content);
    }

    private List<T>? ParseJsonArray<T>(string content)
    {
        content = CleanJsonString(content);
        return JsonSerializer.Deserialize<List<T>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    private string CleanJsonString(string content)
    {
        // Remove markdown code blocks if present
        if (content.Contains("```json"))
        {
            var start = content.IndexOf("```json") + 7;
            var end = content.LastIndexOf("```");
            if (end > start)
            {
                content = content.Substring(start, end - start);
            }
        }
        else if (content.Contains("```"))
        {
             var start = content.IndexOf("```") + 3;
            var end = content.LastIndexOf("```");
             if (end > start)
            {
                content = content.Substring(start, end - start);
            }
        }
        return content.Trim();
    }
}
