// Copyright (c) 2026 SaveStateReborn. All rights reserved.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Bogus;
using SaveState.Core.GameLibrary.Entities;

namespace SaveState.Benchmarks;

/// <summary>
/// Benchmarks for search operations and indexing.
/// </summary>
[SimpleJob(RuntimeMoniker.Net90, launchCount: 1, warmupCount: 3, iterationCount: 5)]
[MemoryDiagnoser]
[RankColumn]
public class SearchBenchmarks
{
    private List<Game> _games = null!;
    private Dictionary<string, List<Game>> _searchIndex = null!;
    private string[] _searchTerms = null!;

    [Params(1000, 10000)]
    public int GameCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _games = GenerateTestGames(GameCount);
        _searchIndex = BuildSearchIndex(_games);
        _searchTerms = new[] { "Elden", "Zelda", "Final", "Game", "RPG", "Action" };
    }

    [Benchmark(Baseline = true)]
    public List<Game> Search_Linq_Contains()
    {
        var term = _searchTerms[0];
        return _games
            .Where(g => g.Title.Contains(term, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    [Benchmark]
    public List<Game> Search_Span()
    {
        var term = _searchTerms[0];
        var results = new List<Game>();
        var searchSpan = term.AsSpan();

        foreach (var game in _games)
        {
            if (game.Title.AsSpan().IndexOf(searchSpan, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                results.Add(game);
            }
        }

        return results;
    }

    [Benchmark]
    public List<Game> Search_Parallel()
    {
        var term = _searchTerms[0];
        return _games
            .AsParallel()
            .Where(g => g.Title.Contains(term, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    [Benchmark]
    public List<Game> Search_Indexed()
    {
        var term = _searchTerms[0].ToLowerInvariant();
        return _searchIndex.TryGetValue(term, out var games) ? games : new List<Game>();
    }

    [Benchmark]
    public Dictionary<string, List<Game>> BuildIndex()
    {
        return BuildSearchIndex(_games);
    }

    [Benchmark]
    public List<Game> FuzzySearch_Levenshtein()
    {
        var term = "Eldn";
        return _games
            .Where(g => CalculateLevenshteinDistance(g.Title[..Math.Min(4, g.Title.Length)], term) <= 2)
            .Take(10)
            .ToList();
    }

    [Benchmark]
    public List<Game> MultiTermSearch()
    {
        var terms = new[] { "RPG", "Action" };
        return _games
            .Where(g => terms.All(t =>
                g.Title.Contains(t, StringComparison.OrdinalIgnoreCase) ||
                (g.Tags?.Any(tag => tag.Contains(t, StringComparison.OrdinalIgnoreCase)) ?? false)))
            .ToList();
    }

    [Benchmark]
    public List<Game> SearchWithPagination()
    {
        int page = 1;
        int pageSize = 20;
        var term = _searchTerms[0];

        return _games
            .Where(g => g.Title.Contains(term, StringComparison.OrdinalIgnoreCase))
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
    }

    [Benchmark]
    public Dictionary<string, int> GetSearchFacets()
    {
        var term = _searchTerms[0];
        var matchingGames = _games
            .Where(g => g.Title.Contains(term, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return matchingGames
            .SelectMany(g => g.Tags ?? new List<string>())
            .GroupBy(tag => tag)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    private static Dictionary<string, List<Game>> BuildSearchIndex(List<Game> games)
    {
        var index = new Dictionary<string, List<Game>>(StringComparer.OrdinalIgnoreCase);

        foreach (var game in games)
        {
            // Index by words in title
            var words = game.Title.Split(new[] { ' ', '-', '_', ':' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var word in words)
            {
                var key = word.ToLowerInvariant();
                if (!index.TryGetValue(key, out var list))
                {
                    list = new List<Game>();
                    index[key] = list;
                }
                list.Add(game);
            }

            // Index by tags
            if (game.Tags != null)
            {
                foreach (var tag in game.Tags)
                {
                    var key = tag.ToLowerInvariant();
                    if (!index.TryGetValue(key, out var list))
                    {
                        list = new List<Game>();
                        index[key] = list;
                    }
                    if (!list.Contains(game))
                    {
                        list.Add(game);
                    }
                }
            }
        }

        return index;
    }

    private static int CalculateLevenshteinDistance(string a, string b)
    {
        if (string.IsNullOrEmpty(a)) return b?.Length ?? 0;
        if (string.IsNullOrEmpty(b)) return a.Length;

        var distances = new int[a.Length + 1, b.Length + 1];

        for (int i = 0; i <= a.Length; i++) distances[i, 0] = i;
        for (int j = 0; j <= b.Length; j++) distances[0, j] = j;

        for (int i = 1; i <= a.Length; i++)
        {
            for (int j = 1; j <= b.Length; j++)
            {
                var cost = (b[j - 1] == a[i - 1]) ? 0 : 1;
                distances[i, j] = Math.Min(
                    Math.Min(distances[i - 1, j] + 1, distances[i, j - 1] + 1),
                    distances[i - 1, j - 1] + cost);
            }
        }

        return distances[a.Length, b.Length];
    }

    private static List<Game> GenerateTestGames(int count)
    {
        var faker = new Faker();
        var games = new List<Game>(count);
        var prefixes = new[] { "The Legend of", "Final", "Elder", "Dark", "Hollow", "Super", "Mega", "Ultra" };
        var suffixes = new[] { "Quest", "Fantasy", "Souls", "Knight", "Mario", "Man", "Storm" };

        for (int i = 0; i < count; i++)
        {
            var prefix = faker.PickRandom(prefixes);
            var suffix = faker.PickRandom(suffixes);
            var title = faker.Random.Bool(0.3f)
                ? $"{prefix} {suffix} {faker.Random.Word()}"
                : $"Game {faker.Random.Word()} {i}";

            var game = Game.Create(title, Guid.NewGuid());

            var tagsProperty = typeof(Game).GetProperty("Tags");
            tagsProperty?.SetValue(game, new List<string>
            {
                faker.PickRandom(new[] { "RPG", "Action", "Strategy" }),
                faker.PickRandom(new[] { "Singleplayer", "Multiplayer" })
            });

            games.Add(game);
        }

        return games;
    }
}
