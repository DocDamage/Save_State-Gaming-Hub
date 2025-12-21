using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SaveState.Core.Services.Ai.Latency
{
    /// <summary>
    /// Manages AI response latency to ensure immersion is never broken.
    /// Provides async generation, fallbacks, streaming, and caching.
    /// 
    /// Key principle: One lag spike kills immersion faster than bad writing.
    /// </summary>
    public interface ILatencyManager
    {
        /// <summary>
        /// Get a response with latency management
        /// </summary>
        Task<LatencyManagedResponse> GetResponseAsync(LatencyRequest request);

        /// <summary>
        /// Pre-warm cache for anticipated requests
        /// </summary>
        Task PrewarmAsync(IEnumerable<string> anticipatedPrompts);

        /// <summary>
        /// Get cached response if available
        /// </summary>
        string? GetCachedResponse(string promptKey);

        /// <summary>
        /// Register a fallback response pool
        /// </summary>
        void RegisterFallbackPool(string category, IEnumerable<string> responses);

        /// <summary>
        /// Get latency statistics
        /// </summary>
        LatencyStatistics GetStatistics();

        /// <summary>
        /// Set maximum allowed latency before fallback
        /// </summary>
        void SetMaxLatency(TimeSpan maxLatency);
    }

    /// <summary>
    /// A request with latency requirements
    /// </summary>
    public class LatencyRequest
    {
        public string Prompt { get; set; } = string.Empty;
        public string Category { get; set; } = "general";
        public TimeSpan? MaxWait { get; set; }
        public bool AllowFallback { get; set; } = true;
        public bool AllowStreaming { get; set; } = true;
        public bool AllowCached { get; set; } = true;
        public LatencyPriority Priority { get; set; } = LatencyPriority.Normal;
        public Func<string, Task<string>>? Generator { get; set; }
    }

    /// <summary>
    /// Priority levels for latency handling
    /// </summary>
    public enum LatencyPriority
    {
        Background = 0,    // Can wait indefinitely
        Low = 1,           // Can wait up to 2s
        Normal = 2,        // Target 500ms
        High = 3,          // Target 200ms
        Critical = 4       // Target 50ms, use cache/fallback
    }

    /// <summary>
    /// Response from latency manager
    /// </summary>
    public class LatencyManagedResponse
    {
        public string Content { get; set; } = string.Empty;
        public ResponseSource Source { get; set; }
        public TimeSpan Latency { get; set; }
        public bool WasStreamed { get; set; }
        public string? FollowUpContent { get; set; }
        public bool HasFollowUp { get; set; }
        
        /// <summary>
        /// Callback for when follow-up content is ready
        /// </summary>
        public Func<Task<string>>? FollowUpGenerator { get; set; }
    }

    /// <summary>
    /// Where the response came from
    /// </summary>
    public enum ResponseSource
    {
        Generated,          // Fresh from LLM
        Cached,             // From response cache
        Fallback,           // From fallback pool
        Streamed,           // Partially streamed
        Prewarmed           // From prewarmed cache
    }

    /// <summary>
    /// Latency statistics
    /// </summary>
    public class LatencyStatistics
    {
        public long TotalRequests { get; set; }
        public long CacheHits { get; set; }
        public long FallbacksUsed { get; set; }
        public TimeSpan AverageLatency { get; set; }
        public TimeSpan P95Latency { get; set; }
        public TimeSpan P99Latency { get; set; }
        public double CacheHitRate => TotalRequests > 0 
            ? (double)CacheHits / TotalRequests * 100 : 0;
    }

    /// <summary>
    /// Default implementation of latency manager
    /// </summary>
    public class LatencyManager : ILatencyManager
    {
        private readonly ConcurrentDictionary<string, CachedResponse> _cache = new();
        private readonly ConcurrentDictionary<string, List<string>> _fallbackPools = new();
        private readonly ConcurrentDictionary<string, int> _fallbackIndices = new();
        private readonly Random _random = new();
        
        private TimeSpan _maxLatency = TimeSpan.FromMilliseconds(500);
        private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(10);
        
        // Statistics
        private long _totalRequests = 0;
        private long _cacheHits = 0;
        private long _fallbacksUsed = 0;
        private readonly ConcurrentBag<TimeSpan> _latencies = new();

        public LatencyManager()
        {
            RegisterDefaultFallbackPools();
        }

        public async Task<LatencyManagedResponse> GetResponseAsync(LatencyRequest request)
        {
            var startTime = DateTime.UtcNow;
            Interlocked.Increment(ref _totalRequests);

            var maxWait = request.MaxWait ?? GetMaxWaitForPriority(request.Priority);

            // 1. Try cache first
            if (request.AllowCached)
            {
                var cached = GetCachedResponse(request.Prompt);
                if (cached != null)
                {
                    Interlocked.Increment(ref _cacheHits);
                    var latency = DateTime.UtcNow - startTime;
                    _latencies.Add(latency);
                    
                    return new LatencyManagedResponse
                    {
                        Content = cached,
                        Source = ResponseSource.Cached,
                        Latency = latency
                    };
                }
            }

            // 2. For critical priority, use fallback immediately with async follow-up
            if (request.Priority == LatencyPriority.Critical && request.AllowFallback)
            {
                var fallback = GetFallbackResponse(request.Category);
                Interlocked.Increment(ref _fallbacksUsed);
                var latency = DateTime.UtcNow - startTime;
                _latencies.Add(latency);

                return new LatencyManagedResponse
                {
                    Content = fallback,
                    Source = ResponseSource.Fallback,
                    Latency = latency,
                    HasFollowUp = request.Generator != null,
                    FollowUpGenerator = async () =>
                    {
                        if (request.Generator != null)
                        {
                            var generated = await request.Generator(request.Prompt);
                            CacheResponse(request.Prompt, generated);
                            return generated;
                        }
                        return string.Empty;
                    }
                };
            }

            // 3. Try to generate with timeout
            if (request.Generator != null)
            {
                using var cts = new CancellationTokenSource(maxWait);
                
                try
                {
                    var generateTask = request.Generator(request.Prompt);
                    var completedTask = await Task.WhenAny(
                        generateTask,
                        Task.Delay(maxWait, cts.Token)
                    );

                    if (completedTask == generateTask)
                    {
                        var generated = await generateTask;
                        CacheResponse(request.Prompt, generated);
                        var latency = DateTime.UtcNow - startTime;
                        _latencies.Add(latency);

                        return new LatencyManagedResponse
                        {
                            Content = generated,
                            Source = ResponseSource.Generated,
                            Latency = latency
                        };
                    }
                    else if (request.AllowFallback)
                    {
                        // Timeout - use fallback with async follow-up
                        var fallback = GetFallbackResponse(request.Category);
                        Interlocked.Increment(ref _fallbacksUsed);
                        var latency = DateTime.UtcNow - startTime;
                        _latencies.Add(latency);

                        // Continue generating in background
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                var generated = await generateTask;
                                CacheResponse(request.Prompt, generated);
                            }
                            catch { }
                        });

                        return new LatencyManagedResponse
                        {
                            Content = fallback,
                            Source = ResponseSource.Fallback,
                            Latency = latency,
                            HasFollowUp = true,
                            FollowUpContent = null
                        };
                    }
                }
                catch (OperationCanceledException)
                {
                    if (request.AllowFallback)
                    {
                        var fallback = GetFallbackResponse(request.Category);
                        Interlocked.Increment(ref _fallbacksUsed);
                        return new LatencyManagedResponse
                        {
                            Content = fallback,
                            Source = ResponseSource.Fallback,
                            Latency = DateTime.UtcNow - startTime
                        };
                    }
                }
            }

            // 4. Final fallback
            if (request.AllowFallback)
            {
                var fallback = GetFallbackResponse(request.Category);
                Interlocked.Increment(ref _fallbacksUsed);
                return new LatencyManagedResponse
                {
                    Content = fallback,
                    Source = ResponseSource.Fallback,
                    Latency = DateTime.UtcNow - startTime
                };
            }

            return new LatencyManagedResponse
            {
                Content = "...",
                Source = ResponseSource.Fallback,
                Latency = DateTime.UtcNow - startTime
            };
        }

        public async Task PrewarmAsync(IEnumerable<string> anticipatedPrompts)
        {
            var tasks = anticipatedPrompts.Select(async prompt =>
            {
                // Pre-compute a cache key placeholder
                CacheResponse(prompt, $"[prewarming:{prompt}]");
            });

            await Task.WhenAll(tasks);
        }

        public string? GetCachedResponse(string promptKey)
        {
            var key = GenerateCacheKey(promptKey);
            if (_cache.TryGetValue(key, out var cached))
            {
                if (DateTime.UtcNow - cached.CachedAt < _cacheExpiry)
                {
                    return cached.Response;
                }
                _cache.TryRemove(key, out _);
            }
            return null;
        }

        public void RegisterFallbackPool(string category, IEnumerable<string> responses)
        {
            _fallbackPools[category] = responses.ToList();
            _fallbackIndices[category] = 0;
        }

        public LatencyStatistics GetStatistics()
        {
            var latencyList = _latencies.ToArray();
            var avgLatency = latencyList.Any() 
                ? TimeSpan.FromTicks((long)latencyList.Average(l => l.Ticks))
                : TimeSpan.Zero;

            var sortedLatencies = latencyList.OrderBy(l => l).ToArray();
            var p95 = sortedLatencies.Length > 0 
                ? sortedLatencies[(int)(sortedLatencies.Length * 0.95)] 
                : TimeSpan.Zero;
            var p99 = sortedLatencies.Length > 0 
                ? sortedLatencies[(int)(sortedLatencies.Length * 0.99)] 
                : TimeSpan.Zero;

            return new LatencyStatistics
            {
                TotalRequests = _totalRequests,
                CacheHits = _cacheHits,
                FallbacksUsed = _fallbacksUsed,
                AverageLatency = avgLatency,
                P95Latency = p95,
                P99Latency = p99
            };
        }

        public void SetMaxLatency(TimeSpan maxLatency)
        {
            _maxLatency = maxLatency;
        }

        private TimeSpan GetMaxWaitForPriority(LatencyPriority priority)
        {
            return priority switch
            {
                LatencyPriority.Critical => TimeSpan.FromMilliseconds(50),
                LatencyPriority.High => TimeSpan.FromMilliseconds(200),
                LatencyPriority.Normal => TimeSpan.FromMilliseconds(500),
                LatencyPriority.Low => TimeSpan.FromSeconds(2),
                LatencyPriority.Background => TimeSpan.FromSeconds(30),
                _ => _maxLatency
            };
        }

        private string GetFallbackResponse(string category)
        {
            if (_fallbackPools.TryGetValue(category, out var pool) && pool.Any())
            {
                // Round-robin with some randomness
                var index = _fallbackIndices.AddOrUpdate(category, 0, (_, i) => (i + 1) % pool.Count);
                return pool[index];
            }

            if (_fallbackPools.TryGetValue("general", out var generalPool) && generalPool.Any())
            {
                return generalPool[_random.Next(generalPool.Count)];
            }

            return "...";
        }

        private void CacheResponse(string prompt, string response)
        {
            var key = GenerateCacheKey(prompt);
            _cache[key] = new CachedResponse
            {
                Response = response,
                CachedAt = DateTime.UtcNow
            };
        }

        private string GenerateCacheKey(string prompt)
        {
            return prompt.Length > 100 
                ? prompt[..100].GetHashCode().ToString() 
                : prompt.GetHashCode().ToString();
        }

        private void RegisterDefaultFallbackPools()
        {
            // General fallbacks
            RegisterFallbackPool("general", new[]
            {
                "Let me think about that...",
                "Hmm, interesting...",
                "One moment...",
                "I'm considering the possibilities...",
                "That's a good question..."
            });

            // Combat banter
            RegisterFallbackPool("combat", new[]
            {
                "The battle rages on!",
                "Steel clashes against steel!",
                "You strike with precision!",
                "Your opponent staggers!",
                "The fight intensifies!",
                "You press the attack!",
                "Victory draws near!",
                "You dodge the incoming strike!"
            });

            // NPC dialogue
            RegisterFallbackPool("npc_dialogue", new[]
            {
                "Greetings, traveler.",
                "What brings you here?",
                "How may I help you?",
                "Ah, I know you...",
                "Times are strange indeed.",
                "I've heard tales of your deeds."
            });

            // Quest updates
            RegisterFallbackPool("quest", new[]
            {
                "Your journey continues...",
                "The path ahead becomes clearer.",
                "New challenges await.",
                "Your progress has been noted.",
                "The quest unfolds..."
            });

            // Environmental narration
            RegisterFallbackPool("environment", new[]
            {
                "The air grows still.",
                "Shadows dance in the corners.",
                "An ancient presence lingers here.",
                "The atmosphere shifts subtly.",
                "Something stirs in the distance."
            });

            // Discovery moments
            RegisterFallbackPool("discovery", new[]
            {
                "You've uncovered something interesting!",
                "A new revelation emerges.",
                "Knowledge comes to light.",
                "The pieces begin to fit together.",
                "A secret reveals itself."
            });
        }

        private class CachedResponse
        {
            public string Response { get; set; } = string.Empty;
            public DateTime CachedAt { get; set; }
        }
    }
}
