using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SaveState.Core.Services.Ai.Latency
{
    /// <summary>
    /// Handles streaming of AI responses for perceived faster response time.
    /// </summary>
    public interface IStreamingHandler
    {
        /// <summary>
        /// Stream a response with callbacks for each chunk
        /// </summary>
        Task StreamResponseAsync(
            Func<CancellationToken, IAsyncEnumerable<string>> generator,
            Action<string> onChunk,
            Action<string>? onComplete = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get a response with an immediate preview and full content
        /// </summary>
        Task<StreamedResponse> GetStreamedResponseAsync(
            Func<CancellationToken, IAsyncEnumerable<string>> generator,
            int previewLength = 50,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// A response that was streamed
    /// </summary>
    public class StreamedResponse
    {
        public string Preview { get; set; } = string.Empty;
        public string FullContent { get; set; } = string.Empty;
        public TimeSpan TimeToFirstToken { get; set; }
        public TimeSpan TotalTime { get; set; }
        public int ChunkCount { get; set; }
    }

    /// <summary>
    /// Default streaming handler implementation
    /// </summary>
    public class StreamingHandler : IStreamingHandler
    {
        public async Task StreamResponseAsync(
            Func<CancellationToken, IAsyncEnumerable<string>> generator,
            Action<string> onChunk,
            Action<string>? onComplete = null,
            CancellationToken cancellationToken = default)
        {
            var fullContent = new StringBuilder();

            await foreach (var chunk in generator(cancellationToken))
            {
                if (cancellationToken.IsCancellationRequested) break;
                
                fullContent.Append(chunk);
                onChunk(chunk);
            }

            onComplete?.Invoke(fullContent.ToString());
        }

        public async Task<StreamedResponse> GetStreamedResponseAsync(
            Func<CancellationToken, IAsyncEnumerable<string>> generator,
            int previewLength = 50,
            CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow;
            var fullContent = new StringBuilder();
            var preview = new StringBuilder();
            var chunkCount = 0;
            TimeSpan? timeToFirstToken = null;

            await foreach (var chunk in generator(cancellationToken))
            {
                if (cancellationToken.IsCancellationRequested) break;

                chunkCount++;
                fullContent.Append(chunk);

                if (timeToFirstToken == null)
                {
                    timeToFirstToken = DateTime.UtcNow - startTime;
                }

                if (preview.Length < previewLength)
                {
                    preview.Append(chunk);
                }
            }

            return new StreamedResponse
            {
                Preview = preview.ToString(),
                FullContent = fullContent.ToString(),
                TimeToFirstToken = timeToFirstToken ?? TimeSpan.Zero,
                TotalTime = DateTime.UtcNow - startTime,
                ChunkCount = chunkCount
            };
        }
    }

    /// <summary>
    /// Pre-warms responses for anticipated requests
    /// </summary>
    public interface IResponseWarmer
    {
        /// <summary>
        /// Predict and pre-generate responses based on context
        /// </summary>
        Task WarmForContextAsync(WarmingContext context);

        /// <summary>
        /// Get a pre-warmed response
        /// </summary>
        string? GetWarmedResponse(string key);

        /// <summary>
        /// Register a prediction strategy
        /// </summary>
        void RegisterStrategy(IPredictionStrategy strategy);
    }

    /// <summary>
    /// Context for warming predictions
    /// </summary>
    public class WarmingContext
    {
        public string? CurrentLocation { get; set; }
        public string? CurrentQuest { get; set; }
        public List<string> RecentActions { get; set; } = new();
        public List<string> NearbyNpcs { get; set; } = new();
        public string? CurrentDialoguePartner { get; set; }
        public Dictionary<string, object> GameState { get; set; } = new();
    }

    /// <summary>
    /// Strategy for predicting needed responses
    /// </summary>
    public interface IPredictionStrategy
    {
        string Name { get; }
        IEnumerable<string> PredictPrompts(WarmingContext context);
    }

    /// <summary>
    /// Default response warmer implementation
    /// </summary>
    public class ResponseWarmer : IResponseWarmer
    {
        private readonly ConcurrentDictionary<string, string> _warmedResponses = new();
        private readonly List<IPredictionStrategy> _strategies = new();
        private readonly Func<string, Task<string>>? _generator;

        public ResponseWarmer(Func<string, Task<string>>? generator = null)
        {
            _generator = generator;
            RegisterDefaultStrategies();
        }

        public async Task WarmForContextAsync(WarmingContext context)
        {
            var predictedPrompts = new HashSet<string>();

            foreach (var strategy in _strategies)
            {
                var prompts = strategy.PredictPrompts(context);
                foreach (var prompt in prompts)
                {
                    predictedPrompts.Add(prompt);
                }
            }

            if (_generator == null) return;

            var tasks = predictedPrompts.Select(async prompt =>
            {
                try
                {
                    var response = await _generator(prompt);
                    _warmedResponses[prompt] = response;
                }
                catch { }
            });

            await Task.WhenAll(tasks);
        }

        public string? GetWarmedResponse(string key)
        {
            return _warmedResponses.TryGetValue(key, out var response) ? response : null;
        }

        public void RegisterStrategy(IPredictionStrategy strategy)
        {
            _strategies.Add(strategy);
        }

        private void RegisterDefaultStrategies()
        {
            // Nearby NPC greetings
            RegisterStrategy(new NpcGreetingStrategy());
            
            // Quest continuations
            RegisterStrategy(new QuestContinuationStrategy());
            
            // Location descriptions
            RegisterStrategy(new LocationDescriptionStrategy());
        }
    }

    /// <summary>
    /// Predicts NPC greeting prompts based on nearby NPCs
    /// </summary>
    public class NpcGreetingStrategy : IPredictionStrategy
    {
        public string Name => "NpcGreeting";

        public IEnumerable<string> PredictPrompts(WarmingContext context)
        {
            foreach (var npc in context.NearbyNpcs)
            {
                yield return $"Generate a greeting from {npc}";
                yield return $"What does {npc} say when approached?";
            }
        }
    }

    /// <summary>
    /// Predicts quest continuation prompts
    /// </summary>
    public class QuestContinuationStrategy : IPredictionStrategy
    {
        public string Name => "QuestContinuation";

        public IEnumerable<string> PredictPrompts(WarmingContext context)
        {
            if (!string.IsNullOrEmpty(context.CurrentQuest))
            {
                yield return $"What is the next objective for {context.CurrentQuest}?";
                yield return $"Provide hint for {context.CurrentQuest}";
            }
        }
    }

    /// <summary>
    /// Predicts location description prompts
    /// </summary>
    public class LocationDescriptionStrategy : IPredictionStrategy
    {
        public string Name => "LocationDescription";

        public IEnumerable<string> PredictPrompts(WarmingContext context)
        {
            if (!string.IsNullOrEmpty(context.CurrentLocation))
            {
                yield return $"Describe {context.CurrentLocation}";
                yield return $"What does the player notice at {context.CurrentLocation}?";
            }
        }
    }
}
