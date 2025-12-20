using SaveState.Core.Entities;
using SaveState.Core.Interfaces;
using SaveState.Core.Models;
using Serilog;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace SaveState.Core.Services;

public class OpenAiService : IAiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger = Log.ForContext<OpenAiService>();
    private readonly string _model = "gpt-4o-mini";
    private string? _apiKey;

    public bool IsConfigured => !string.IsNullOrEmpty(_apiKey);

    public OpenAiService(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("OpenAI");
        _httpClient.BaseAddress = new Uri("https://api.openai.com/v1/");
        
        // Try to get API key from environment
        _apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (!string.IsNullOrEmpty(_apiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", _apiKey);
        }
    }

    public async Task<string> ChatAsync(string message, IEnumerable<AiChatMessage>? history = null)
    {
        if (!IsConfigured)
            return "AI is not configured. Please add your OpenAI API key in Settings.";

        try
        {
            var messages = new List<object>
            {
                new { role = "system", content = GetSystemPrompt() }
            };

            // Add history if provided
            if (history != null)
            {
                messages.AddRange(history.Select(h => new { role = h.Role, content = h.Content }));
            }

            messages.Add(new { role = "user", content = message });

            var requestBody = new
            {
                model = _model,
                messages,
                max_tokens = 1000,
                temperature = 0.7
            };

            var response = await SendRequestAsync("chat/completions", requestBody);
            return ExtractContent(response);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "AI chat failed");
            return $"Sorry, I encountered an error: {ex.Message}";
        }
    }

    public async Task<IEnumerable<GameRecommendation>> GetRecommendationsAsync(IEnumerable<Game> libraryGames, int count = 5)
    {
        if (!IsConfigured)
            return Enumerable.Empty<GameRecommendation>();

        try
        {
            var gameList = string.Join(", ", libraryGames.Take(20).Select(g => g.Title));
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
            
            // Parse JSON response
            var jsonStart = response.IndexOf('[');
            var jsonEnd = response.LastIndexOf(']') + 1;
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var json = response.Substring(jsonStart, jsonEnd - jsonStart);
                var recommendations = JsonSerializer.Deserialize<List<GameRecommendation>>(json, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return recommendations ?? Enumerable.Empty<GameRecommendation>();
            }
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to parse recommendations");
        }

        return Enumerable.Empty<GameRecommendation>();
    }

    public async Task<AiAnalysisResult> AnalyzeGameAsync(string gameTitle)
    {
        if (!IsConfigured)
            return new AiAnalysisResult { GameTitle = gameTitle };

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
            
            var jsonStart = response.IndexOf('{');
            var jsonEnd = response.LastIndexOf('}') + 1;
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var json = response.Substring(jsonStart, jsonEnd - jsonStart);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                
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
            _logger.Warning(ex, "Failed to analyze game: {Title}", gameTitle);
        }

        return new AiAnalysisResult { GameTitle = gameTitle };
    }

    public async Task<IEnumerable<string>> FindSimilarGamesAsync(Game game, int count = 5)
    {
        if (!IsConfigured)
            return Enumerable.Empty<string>();

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
        if (!IsConfigured)
            return "AI is not configured. Please add your OpenAI API key in Settings.";

        var prompt = $"""
            Give me 5 helpful tips for playing "{gameTitle}".
            Include beginner advice and some advanced strategies.
            Keep each tip concise (1-2 sentences).
            """;

        return await ChatAsync(prompt);
    }

    private string GetSystemPrompt()
    {
        return """
            You are a friendly gaming assistant for SaveState, a game library management app.
            You help users with:
            - Game recommendations based on their preferences
            - Tips, walkthroughs, and strategies
            - Gaming news and information
            - Technical help with games and emulators
            
            Be concise, helpful, and enthusiastic about gaming!
            When providing structured data, use clean JSON format.
            """;
    }

    private async Task<string> SendRequestAsync(string endpoint, object body)
    {
        var json = JsonSerializer.Serialize(body);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await _httpClient.PostAsync(endpoint, content);
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadAsStringAsync();
    }

    private string ExtractContent(string response)
    {
        using var doc = JsonDocument.Parse(response);
        return doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "";
    }
}
