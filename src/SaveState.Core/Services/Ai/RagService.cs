using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace SaveState.Core.Services.Ai
{
    /// <summary>
    /// RAG (Retrieval-Augmented Generation) Service
    /// Implements techniques from: https://github.com/NirDiamant/RAG_Techniques
    /// Follows BMAD methodology from: https://github.com/bmad-code-org/BMAD-METHOD
    /// </summary>
    public class RagDocument
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;  // "cheats", "tips", "lore", "speedrun", "guide"
        public List<string> Tags { get; set; } = new();
        public DateTime AddedAt { get; set; }
        public float[]? Embedding { get; set; }  // Vector embedding for semantic search
    }

    public class RagSearchResult
    {
        public RagDocument Document { get; set; } = new();
        public float Score { get; set; }
        public string MatchedChunk { get; set; } = string.Empty;
    }

    public class RagContext
    {
        public string Query { get; set; } = string.Empty;
        public List<RagSearchResult> Results { get; set; } = new();
        public string CombinedContext { get; set; } = string.Empty;
    }

    public class RagService
    {
        private static RagService? _instance;
        private readonly string _knowledgeBasePath;
        private readonly List<RagDocument> _documents = new();
        private readonly ILlmService? _llmService;
        private readonly Memory.IMemoryOrchestrator? _memoryOrchestrator;

        // Simple in-memory index for keyword search (production would use vector DB)
        private readonly Dictionary<string, List<string>> _invertedIndex = new();

        public static RagService Instance => _instance ??= new RagService();
        public int DocumentCount => _documents.Count;

        public RagService(ILlmService? llmService = null, Memory.IMemoryOrchestrator? memoryOrchestrator = null)
        {
            _llmService = llmService;
            _memoryOrchestrator = memoryOrchestrator;
            _knowledgeBasePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "SaveState2", "data", "knowledge_base");
            if (!Directory.Exists(_knowledgeBasePath)) Directory.CreateDirectory(_knowledgeBasePath);
            LoadDocuments();
            BuildIndex();
        }

        public void AddDocument(string title, string content, string category, string source = "", params string[] tags)
        {
            var doc = new RagDocument
            {
                Title = title,
                Content = content,
                Category = category,
                Source = source,
                Tags = tags.ToList(),
                AddedAt = DateTime.Now
            };

            _documents.Add(doc);
            IndexDocument(doc);
            SaveDocument(doc);
        }

        public void AddCheatCode(string gameName, string cheatName, string cheatCode, string effect)
        {
            AddDocument(
                $"{gameName} - {cheatName}",
                $"Cheat: {cheatName}\nCode: {cheatCode}\nEffect: {effect}",
                "cheats",
                "user_added",
                gameName.ToLower(), "cheat"
            );
        }

        public void AddGameTip(string gameName, string tip, string context = "general")
        {
            AddDocument(
                $"{gameName} Tip",
                tip,
                "tips",
                "user_added",
                gameName.ToLower(), context
            );
        }

        public void AddSpeedrunStrat(string gameName, string stratName, string description)
        {
            AddDocument(
                $"{gameName} - {stratName}",
                description,
                "speedrun",
                "user_added",
                gameName.ToLower(), "speedrun", "strategy"
            );
        }

        // Keyword-based search (fast, simple)
        public List<RagSearchResult> SearchKeyword(string query, int maxResults = 5)
        {
            var queryTerms = Tokenize(query);
            var scores = new Dictionary<string, int>();

            foreach (var term in queryTerms)
            {
                if (_invertedIndex.TryGetValue(term, out var docIds))
                {
                    foreach (var docId in docIds)
                    {
                        scores[docId] = scores.GetValueOrDefault(docId) + 1;
                    }
                }
            }

            return scores
                .OrderByDescending(kvp => kvp.Value)
                .Take(maxResults)
                .Select(kvp =>
                {
                    var doc = _documents.FirstOrDefault(d => d.Id == kvp.Key);
                    return new RagSearchResult
                    {
                        Document = doc ?? new RagDocument(),
                        Score = kvp.Value / (float)queryTerms.Count,
                        MatchedChunk = ExtractRelevantChunk(doc?.Content ?? "", query)
                    };
                })
                .Where(r => r.Document.Id != null)
                .ToList();
        }

        // Category-filtered search
        public List<RagSearchResult> SearchByCategory(string query, string category, int maxResults = 5)
        {
            return SearchKeyword(query, maxResults * 2)
                .Where(r => r.Document.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
                .Take(maxResults)
                .ToList();
        }

        // Build RAG context for LLM
        public RagContext BuildContext(string query, int maxDocuments = 3)
        {
            var results = SearchKeyword(query, maxDocuments);
            
            var combinedContext = string.Join("\n\n", results.Select(r =>
                $"[{r.Document.Category.ToUpper()}] {r.Document.Title}:\n{r.MatchedChunk}"
            ));

            return new RagContext
            {
                Query = query,
                Results = results,
                CombinedContext = combinedContext
            };
        }

        // Generate answer using RAG
        public async Task<string> QueryWithRagAsync(string query, ILlmService llmService)
        {
            var context = BuildContext(query);

            if (context.Results.Count == 0)
            {
                return await llmService.CompleteAsync(query);
            }

            var ragPrompt = $@"Use the following knowledge to answer the question. If the knowledge doesn't help, answer from general knowledge.

KNOWLEDGE:
{context.CombinedContext}

QUESTION: {query}

ANSWER:";

            return await llmService.CompleteAsync(ragPrompt, 
                "You are a helpful gaming assistant with deep knowledge. Be concise and accurate.");
        }

        // Get cheats for a specific game
        public List<RagDocument> GetCheatsForGame(string gameName)
        {
            return _documents
                .Where(d => d.Category == "cheats" && 
                       (d.Tags.Contains(gameName.ToLower()) || d.Title.Contains(gameName, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        // Get tips for a specific game
        public List<RagDocument> GetTipsForGame(string gameName)
        {
            return _documents
                .Where(d => d.Category == "tips" && 
                       (d.Tags.Contains(gameName.ToLower()) || d.Title.Contains(gameName, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        // Import from markdown file
        public void ImportMarkdownFile(string filePath, string category)
        {
            if (!File.Exists(filePath)) return;

            var content = File.ReadAllText(filePath);
            var fileName = Path.GetFileNameWithoutExtension(filePath);

            AddDocument(fileName, content, category, filePath);
        }

        // Import bulk from directory
        public int ImportDirectory(string directoryPath, string category)
        {
            if (!Directory.Exists(directoryPath)) return 0;

            var count = 0;
            foreach (var file in Directory.GetFiles(directoryPath, "*.md"))
            {
                ImportMarkdownFile(file, category);
                count++;
            }
            foreach (var file in Directory.GetFiles(directoryPath, "*.txt"))
            {
                ImportMarkdownFile(file, category);
                count++;
            }

            return count;
        }

        private string ExtractRelevantChunk(string content, string query, int chunkSize = 500)
        {
            if (string.IsNullOrEmpty(content)) return "";
            if (content.Length <= chunkSize) return content;

            // Find best matching section
            var queryTerms = Tokenize(query);
            var sentences = content.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
            
            int bestStart = 0;
            int bestScore = 0;

            for (int i = 0; i < sentences.Length; i++)
            {
                var score = queryTerms.Count(t => sentences[i].Contains(t, StringComparison.OrdinalIgnoreCase));
                if (score > bestScore)
                {
                    bestScore = score;
                    bestStart = i;
                }
            }

            // Get surrounding context
            var start = Math.Max(0, bestStart - 1);
            var end = Math.Min(sentences.Length, bestStart + 3);
            return string.Join(". ", sentences.Skip(start).Take(end - start)) + ".";
        }

        private List<string> Tokenize(string text)
        {
            return text.ToLower()
                .Split(new[] { ' ', ',', '.', '!', '?', '-', '_' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(t => t.Length > 2)
                .Distinct()
                .ToList();
        }

        private void IndexDocument(RagDocument doc)
        {
            var terms = Tokenize(doc.Title + " " + doc.Content);
            foreach (var term in terms)
            {
                if (!_invertedIndex.ContainsKey(term))
                {
                    _invertedIndex[term] = new List<string>();
                }
                if (!_invertedIndex[term].Contains(doc.Id))
                {
                    _invertedIndex[term].Add(doc.Id);
                }
            }

            // Also index tags
            foreach (var tag in doc.Tags)
            {
                var tagLower = tag.ToLower();
                if (!_invertedIndex.ContainsKey(tagLower))
                {
                    _invertedIndex[tagLower] = new List<string>();
                }
                if (!_invertedIndex[tagLower].Contains(doc.Id))
                {
                    _invertedIndex[tagLower].Add(doc.Id);
                }
            }
        }

        private void BuildIndex()
        {
            _invertedIndex.Clear();
            foreach (var doc in _documents)
            {
                IndexDocument(doc);
            }
        }

        private void SaveDocument(RagDocument doc)
        {
            var path = Path.Combine(_knowledgeBasePath, $"{doc.Id}.json");
            var json = JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }

        private void LoadDocuments()
        {
            if (!Directory.Exists(_knowledgeBasePath)) return;

            foreach (var file in Directory.GetFiles(_knowledgeBasePath, "*.json"))
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var doc = JsonSerializer.Deserialize<RagDocument>(json);
                    if (doc != null)
                    {
                        _documents.Add(doc);
                    }
                }
                catch { }
            }
        }

        public bool DeleteDocument(string id)
        {
            var doc = _documents.FirstOrDefault(d => d.Id == id);
            if (doc == null) return false;

            _documents.Remove(doc);
            var path = Path.Combine(_knowledgeBasePath, $"{id}.json");
            if (File.Exists(path)) File.Delete(path);
            BuildIndex(); // Rebuild index
            return true;
        }

        public string GetKnowledgeBasePath() => _knowledgeBasePath;

        public Dictionary<string, int> GetCategoryStats()
        {
            return _documents
                .GroupBy(d => d.Category)
                .ToDictionary(g => g.Key, g => g.Count());
        }
    }
}
