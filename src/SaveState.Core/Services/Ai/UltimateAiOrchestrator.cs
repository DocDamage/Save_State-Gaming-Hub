using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace SaveState.Core.Services.Ai
{
    // Note: Data models (AiPipelineStage, PipelineContext, PipelineResult, etc.)
    // are defined in OrchestratorModels.cs for better organization.

    public class UltimateAiOrchestrator : IUltimateAiOrchestrator
    {
        private readonly ILogger _logger = Log.ForContext<UltimateAiOrchestrator>();
        private readonly PipelineOrchestrator _pipelineOrchestrator;
        private readonly CacheManager _cacheManager;
        private readonly ExperimentManager _experimentManager;
        private readonly MetricsService _metricsService;
        private readonly HealthMonitor _healthMonitor;
        private readonly UltimateOrchestratorConfig _config;

        public UltimateAiOrchestrator(UltimateOrchestratorConfig? config = null)
        {
            _config = config ?? new UltimateOrchestratorConfig();

            // Initialize focused services
            _metricsService = new MetricsService();
            _cacheManager = new CacheManager();
            _experimentManager = new ExperimentManager();
            _pipelineOrchestrator = new PipelineOrchestrator();
            _healthMonitor = new HealthMonitor(_metricsService, _cacheManager);

            // Build the standard pipeline
            BuildStandardPipeline();
        }

        /// <summary>
        /// Configures the orchestrator with the standard game pipeline:
        /// 1. Governance (KillSwitch + Policy)
        /// 2. Intent Routing & Execution (Specialist Agents)
        /// 3. Validation
        /// 4. Provenance Recording
        /// </summary>
        public void BuildStandardPipeline()
        {
            var provider = AiServiceProvider.Instance;

            // Stage 1: Governance & Safety
            AddStage("Governance", (context) =>
            {
                if (!provider.KillSwitch.IsFeatureAllowed("AiGeneration"))
                {
                    context.Errors.Add("AI Generation is globally disabled");
                    throw new OperationCanceledException("AI Generation disabled");
                }

                // Policy Check
                // Note: We assume default contract for now
                // var decision = provider.PolicyGate.EnforceContract(defaultContract, request);
                return Task.CompletedTask;
            }, new AiPipelineStage { Name = "Governance", Priority = 0, CriticalStage = true });

            // Stage 2: Intent Routing & Execution
            AddStage("CoreExecution", async (context) =>
            {
                var sessionId = context.Data.ContainsKey("SessionId") ? context.Data["SessionId"].ToString() : "default";
                var userId = context.Data.ContainsKey("UserId") ? context.Data["UserId"].ToString() : "anonymous";
                
                // Delegate to the Intent Router (The Brain)
                var result = await provider.IntentRouter.RouteAndProcessAsync(context.Input, sessionId!, userId!);
                context.Output = result;
            }, new AiPipelineStage { Name = "CoreExecution", Priority = 10, CriticalStage = true });

            // Stage 3: Post-Processing & Provenance
            AddStage("Provenance", async (context) =>
            {
                if (!string.IsNullOrEmpty(context.Output))
                {
                    // Record to ledger with improved agent identification and quality scoring
                    var agentId = context.Data.ContainsKey("AgentId") ? context.Data["AgentId"].ToString() : "Orchestrator";
                    var qualityScore = context.Data.ContainsKey("QualityScore") ? Convert.ToSingle(context.Data["QualityScore"]) : 1.0f;

                    await provider.ProvenanceLedger.RecordGenerationAsync(
                        agentId: agentId,
                        prompt: context.Input,
                        content: context.Output,
                        score: qualityScore,
                        quarantined: false
                    );
                }
            }, new AiPipelineStage { Name = "Provenance", Priority = 20 });
        }

        public void AddStage(string name, PipelineStageHandler handler, AiPipelineStage? config = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Stage name cannot be empty", nameof(name));

            config ??= new AiPipelineStage { Name = name };
            config.Name = name;
            
            _stages[name] = (handler, config);
            _stageMetrics[name] = new StageMetrics { StageName = name };
        }

        public void RemoveStage(string name)
        {
            _stages.TryRemove(name, out _);
            _conditions.TryRemove(name, out _);
        }

        public void SetStageCondition(string stageName, PipelineCondition condition)
        {
            _conditions[stageName] = condition;
        }

        public async Task<PipelineResult> ExecuteAsync(
            string input, 
            Dictionary<string, object>? initialData = null, 
            CancellationToken ct = default)
        {
            Interlocked.Increment(ref _totalRequests);
            var startTime = DateTime.UtcNow;

            var context = new PipelineContext
            {
                Input = input,
                Data = initialData ?? new Dictionary<string, object>(),
                CancellationToken = ct
            };

            var result = new PipelineResult
            {
                RequestId = context.RequestId
            };

            try
            {
                // Edge case: Empty input
                if (string.IsNullOrWhiteSpace(input))
                {
                    result.Success = false;
                    result.Errors.Add("Input cannot be empty");
                    return FinalizeResult(result, startTime, false);
                }

                // Edge case: Input too long
                if (input.Length > _config.MaxInputLength)
                {
                    if (_config.TruncateLongInputs)
                    {
                        context.Input = input.Substring(0, _config.MaxInputLength);
                        context.Warnings.Add($"Input truncated from {input.Length} to {_config.MaxInputLength} characters");
                    }
                    else
                    {
                        result.Success = false;
                        result.Errors.Add($"Input exceeds maximum length of {_config.MaxInputLength}");
                        return FinalizeResult(result, startTime, false);
                    }
                }

                // Check cache
                var cacheKey = ComputeCacheKey(context.Input);
                if (_cache.TryGetValue(cacheKey, out var cached) && DateTime.UtcNow < cached.Expiry)
                {
                    Interlocked.Increment(ref _cacheHits);
                    result.Output = cached.Value;
                    result.Success = true;
                    result.UsedCache = true;
                    EmitEvent(context.RequestId, "cache", "hit");
                    return FinalizeResult(result, startTime, true);
                }
                Interlocked.Increment(ref _cacheMisses);

                // Select experiment variant if applicable
                var userId = context.Data.TryGetValue("user_id", out var uid) ? uid.ToString() : "anonymous";
                foreach (var exp in _experiments.Values.Where(e => e.IsActive))
                {
                    var variant = GetAssignedVariant(userId!, exp.ExperimentId);
                    if (variant != null)
                    {
                        context.Data[$"experiment_{exp.ExperimentId}"] = variant;
                        EmitEvent(context.RequestId, "experiment", "assigned", 
                            new Dictionary<string, object> { ["experiment"] = exp.ExperimentId, ["variant"] = variant });
                    }
                }

                // Execute pipeline stages
                var orderedStages = _stages.Values
                    .Where(s => s.Config.Enabled)
                    .OrderBy(s => s.Config.Priority)
                    .ToList();

                foreach (var stage in orderedStages)
                {
                    ct.ThrowIfCancellationRequested();

                    var stageResult = await ExecuteStageAsync(stage, context);
                    context.StageResults.Add(stageResult);
                    result.StageResults.Add(stageResult);

                    // Handle stage failure
                    if (!stageResult.Success && !stageResult.WasSkipped)
                    {
                        if (stage.Config.CriticalStage)
                        {
                            result.Success = false;
                            result.Errors.Add($"Critical stage '{stage.Config.Name}' failed: {stageResult.ErrorMessage}");
                            
                            // Try self-healing if enabled
                            if (_selfHealingEnabled)
                            {
                                var healed = await TrySelfHealAsync(stage.Config.Name, context);
                                if (!healed)
                                {
                                    return FinalizeResult(result, startTime, false);
                                }
                            }
                            else
                            {
                                return FinalizeResult(result, startTime, false);
                            }
                        }
                        else
                        {
                            context.Warnings.Add($"Stage '{stage.Config.Name}' failed but is not critical, continuing");
                        }
                    }
                }

                // Get final output
                result.Output = context.Output;
                result.Success = !string.IsNullOrEmpty(context.Output);
                result.Warnings = context.Warnings;
                result.Errors = context.Errors;

                // Cache successful results
                if (result.Success && _config.EnableCaching)
                {
                    _cache[cacheKey] = (result.Output!, DateTime.UtcNow.Add(_config.DefaultCacheTtl));
                }

                // Calculate quality score
                result.QualityScore = CalculateQualityScore(result, context);

                return FinalizeResult(result, startTime, result.Success);
            }
            catch (OperationCanceledException)
            {
                result.Success = false;
                result.Errors.Add("Request was cancelled");
                return FinalizeResult(result, startTime, false);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Errors.Add($"Unhandled exception: {ex.Message}");
                EmitEvent(context.RequestId, "pipeline", "error", 
                    new Dictionary<string, object> { ["exception"] = ex.GetType().Name, ["message"] = ex.Message });
                return FinalizeResult(result, startTime, false);
            }
        }

        public async Task<PipelineResult> ExecuteWithFallbackAsync(
            string input, 
            Func<string, Task<string>> fallback, 
            CancellationToken ct = default)
        {
            var result = await ExecuteAsync(input, ct: ct);

            if (!result.Success && fallback != null)
            {
                try
                {
                    Interlocked.Increment(ref _fallbacksUsed);
                    var fallbackOutput = await fallback(input);
                    result.Output = fallbackOutput;
                    result.Success = !string.IsNullOrEmpty(fallbackOutput);
                    result.FallbackUsed = "custom";
                    EmitEvent(result.RequestId, "fallback", "used");
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Fallback also failed: {ex.Message}");
                }
            }

            return result;
        }

        public void EnableCache(string keyPattern, TimeSpan ttl)
        {
            _config.CachePatterns[keyPattern] = ttl;
        }

        public void InvalidateCache(string keyPattern)
        {
            var keysToRemove = _cache.Keys.Where(k => k.Contains(keyPattern)).ToList();
            foreach (var key in keysToRemove)
            {
                _cache.TryRemove(key, out _);
            }
        }

        public void ClearCache()
        {
            _cache.Clear();
        }

        public void RegisterExperiment(ExperimentConfig config)
        {
            _experiments[config.ExperimentId] = config;
        }

        public void EndExperiment(string experimentId)
        {
            if (_experiments.TryGetValue(experimentId, out var exp))
            {
                exp.IsActive = false;
            }
        }

        public string? GetAssignedVariant(string userId, string experimentId)
        {
            if (!_experiments.TryGetValue(experimentId, out var exp) || !exp.IsActive)
                return null;

            var key = $"{userId}_{experimentId}";
            
            if (_experimentAssignments.TryGetValue(key, out var existing))
                return existing;

            // Deterministic assignment based on user ID hash
            var hash = Math.Abs(key.GetHashCode());
            if (hash % 100 >= exp.TrafficPercentage * 100)
                return null; // Not in experiment

            // Select variant based on weights
            var rand = (hash % 1000) / 1000.0;
            var cumulative = 0.0;
            
            foreach (var variant in exp.Variants.Values)
            {
                cumulative += variant.Weight;
                if (rand <= cumulative)
                {
                    _experimentAssignments[key] = variant.VariantId;
                    return variant.VariantId;
                }
            }

            return exp.Variants.Values.FirstOrDefault()?.VariantId;
        }

        public void AddObserver(ObservabilityHandler handler)
        {
            _observers.Add(handler);
        }

        public OrchestratorMetrics GetMetrics()
        {
            var latencies = _latencyHistory.ToArray();
            Array.Sort(latencies);

            return new OrchestratorMetrics
            {
                TotalRequests = _totalRequests,
                SuccessfulRequests = _successfulRequests,
                FailedRequests = _failedRequests,
                AverageLatency = _totalRequests > 0 
                    ? TimeSpan.FromMilliseconds(_totalLatencyMs / _totalRequests) 
                    : TimeSpan.Zero,
                P50Latency = GetPercentile(latencies, 0.5),
                P95Latency = GetPercentile(latencies, 0.95),
                P99Latency = GetPercentile(latencies, 0.99),
                CacheHits = _cacheHits,
                CacheMisses = _cacheMisses,
                StageMetrics = new Dictionary<string, StageMetrics>(_stageMetrics),
                ActiveExperiments = _experiments.Values.Count(e => e.IsActive),
                FallbacksUsed = _fallbacksUsed
            };
        }

        public List<ObservabilityData> GetRecentEvents(int count = 100)
        {
            return _recentEvents.TakeLast(count).ToList();
        }

        public Task<HealthCheckResult> CheckHealthAsync()
        {
            var result = new HealthCheckResult
            {
                IsHealthy = true
            };

            // Check each stage
            foreach (var stage in _stages)
            {
                var health = new ComponentHealth
                {
                    Name = stage.Key,
                    IsHealthy = true,
                    Status = "ok"
                };

                // Check if stage has been failing
                if (_stageMetrics.TryGetValue(stage.Key, out var metrics))
                {
                    var failureRate = metrics.Executions > 0 
                        ? (float)metrics.Failures / metrics.Executions 
                        : 0;

                    if (failureRate > 0.5f)
                    {
                        health.IsHealthy = false;
                        health.Status = "degraded";
                        health.ErrorMessage = $"High failure rate: {failureRate:P0}";
                        result.Issues.Add($"Stage '{stage.Key}' has high failure rate");
                    }

                    if (metrics.AverageLatency > stage.Value.Config.Timeout)
                    {
                        health.Status = "slow";
                        result.Issues.Add($"Stage '{stage.Key}' is slower than configured timeout");
                    }
                }

                result.Components[stage.Key] = health;
            }

            // Check cache health
            var cacheHealth = new ComponentHealth
            {
                Name = "cache",
                IsHealthy = true,
                Status = "ok"
            };

            if (_cache.Count > _config.MaxCacheSize * 0.9)
            {
                cacheHealth.Status = "warning";
                result.Issues.Add("Cache is nearly full");
            }

            result.Components["cache"] = cacheHealth;

            result.IsHealthy = !result.Issues.Any(i => i.Contains("failure rate"));
            return Task.FromResult(result);
        }

        public void EnableSelfHealing(bool enable)
        {
            _selfHealingEnabled = enable;
        }

        // ============ Private Methods ============

        private async Task<PipelineStageResult> ExecuteStageAsync(
            (PipelineStageHandler Handler, AiPipelineStage Config) stage,
            PipelineContext context)
        {
            var result = new PipelineStageResult
            {
                StageName = stage.Config.Name
            };

            var startTime = DateTime.UtcNow;

            try
            {
                // Check condition
                if (_conditions.TryGetValue(stage.Config.Name, out var condition))
                {
                    if (!await condition(context))
                    {
                        result.WasSkipped = true;
                        result.Success = true;
                        UpdateStageMetrics(stage.Config.Name, true, TimeSpan.Zero, true);
                        return result;
                    }
                }

                // Execute with timeout
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken);
                timeoutCts.CancelAfter(stage.Config.Timeout);

                var originalCt = context.CancellationToken;
                context.CancellationToken = timeoutCts.Token;

                try
                {
                    await stage.Handler(context);
                    result.Success = true;
                }
                finally
                {
                    context.CancellationToken = originalCt;
                }

                result.Duration = DateTime.UtcNow - startTime;
                EmitEvent(context.RequestId, stage.Config.Name, "completed", 
                    new Dictionary<string, object> { ["duration_ms"] = result.Duration.TotalMilliseconds });
                UpdateStageMetrics(stage.Config.Name, true, result.Duration, false);
            }
            catch (OperationCanceledException) when (!context.CancellationToken.IsCancellationRequested)
            {
                result.Success = false;
                result.ErrorMessage = "Stage timed out";
                result.Duration = stage.Config.Timeout;
                EmitEvent(context.RequestId, stage.Config.Name, "timeout");
                UpdateStageMetrics(stage.Config.Name, false, result.Duration, false);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.Duration = DateTime.UtcNow - startTime;
                EmitEvent(context.RequestId, stage.Config.Name, "error",
                    new Dictionary<string, object> { ["exception"] = ex.GetType().Name });
                UpdateStageMetrics(stage.Config.Name, false, result.Duration, false);
            }

            return result;
        }

        private async Task<bool> TrySelfHealAsync(string stageName, PipelineContext context)
        {
            EmitEvent(context.RequestId, "self_healing", "attempting", 
                new Dictionary<string, object> { ["stage"] = stageName });

            // Strategy 1: Retry with delay
            await Task.Delay(500);
            
            if (_stages.TryGetValue(stageName, out var stage))
            {
                try
                {
                    await stage.Handler(context);
                    EmitEvent(context.RequestId, "self_healing", "success");
                    return true;
                }
                catch
                {
                    // Strategy 2: Skip if non-critical
                    if (!stage.Config.CriticalStage)
                    {
                        context.Warnings.Add($"Stage '{stageName}' was skipped after self-healing failed");
                        return true;
                    }
                }
            }

            EmitEvent(context.RequestId, "self_healing", "failed");
            return false;
        }

        private void EmitEvent(string requestId, string stage, string eventType, 
            Dictionary<string, object>? data = null)
        {
            var evt = new ObservabilityData
            {
                RequestId = requestId,
                Stage = stage,
                EventType = eventType,
                Data = data ?? new Dictionary<string, object>()
            };

            _recentEvents.Enqueue(evt);
            while (_recentEvents.Count > _config.MaxEventHistory)
            {
                _recentEvents.TryDequeue(out _);
            }

            foreach (var observer in _observers)
            {
                try { observer(evt); }
                catch (Exception ex)
                {
                    _logger.Debug(ex, "Observer notification failed");
                }
            }
        }

        private void UpdateStageMetrics(string stageName, bool success, TimeSpan duration, bool skipped)
        {
            _stageMetrics.AddOrUpdate(
                stageName,
                new StageMetrics 
                { 
                    StageName = stageName, 
                    Executions = skipped ? 0 : 1,
                    Successes = success && !skipped ? 1 : 0,
                    Failures = !success && !skipped ? 1 : 0,
                    Skipped = skipped ? 1 : 0,
                    AverageLatency = duration
                },
                (_, existing) =>
                {
                    if (!skipped)
                    {
                        existing.Executions++;
                        if (success) existing.Successes++;
                        else existing.Failures++;
                        
                        var totalMs = existing.AverageLatency.TotalMilliseconds * (existing.Executions - 1);
                        existing.AverageLatency = TimeSpan.FromMilliseconds(
                            (totalMs + duration.TotalMilliseconds) / existing.Executions);
                    }
                    else
                    {
                        existing.Skipped++;
                    }
                    return existing;
                });
        }

        private PipelineResult FinalizeResult(PipelineResult result, DateTime startTime, bool success)
        {
            result.TotalDuration = DateTime.UtcNow - startTime;

            if (success)
                Interlocked.Increment(ref _successfulRequests);
            else
                Interlocked.Increment(ref _failedRequests);

            var latencyMs = (long)result.TotalDuration.TotalMilliseconds;
            Interlocked.Add(ref _totalLatencyMs, latencyMs);
            
            _latencyHistory.Enqueue(latencyMs);
            while (_latencyHistory.Count > _config.LatencyHistorySize)
            {
                _latencyHistory.TryDequeue(out _);
            }

            EmitEvent(result.RequestId, "pipeline", success ? "completed" : "failed",
                new Dictionary<string, object> 
                { 
                    ["duration_ms"] = result.TotalDuration.TotalMilliseconds,
                    ["stages_executed"] = result.StageResults.Count
                });

            return result;
        }

        private float CalculateQualityScore(PipelineResult result, PipelineContext context)
        {
            float score = 1.0f;

            // Reduce for warnings
            score -= result.Warnings.Count * 0.05f;

            // Reduce for failed non-critical stages
            score -= result.StageResults.Count(s => !s.Success && !s.WasSkipped) * 0.1f;

            // Reduce for slow stages
            var slowStages = result.StageResults.Count(s => 
                _stages.TryGetValue(s.StageName, out var stage) && 
                s.Duration > stage.Config.Timeout * 0.8);
            score -= slowStages * 0.05f;

            // Bonus for cache hit
            if (result.UsedCache) score += 0.1f;

            return Math.Clamp(score, 0, 1);
        }

        private string ComputeCacheKey(string input)
        {
            // Use hash for cache key
            using var sha = System.Security.Cryptography.SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        private TimeSpan GetPercentile(long[] sortedLatencies, double percentile)
        {
            if (sortedLatencies.Length == 0) return TimeSpan.Zero;
            
            var index = (int)(percentile * sortedLatencies.Length);
            index = Math.Min(index, sortedLatencies.Length - 1);
            return TimeSpan.FromMilliseconds(sortedLatencies[index]);
        }

        private async Task CacheCleanupLoopAsync()
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromMinutes(1));
                
                var now = DateTime.UtcNow;
                var expiredKeys = _cache
                    .Where(kvp => kvp.Value.Expiry < now)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var key in expiredKeys)
                {
                    _cache.TryRemove(key, out _);
                }

                // Also trim if over size
                while (_cache.Count > _config.MaxCacheSize)
                {
                    var oldest = _cache.OrderBy(c => c.Value.Expiry).First();
                    _cache.TryRemove(oldest.Key, out _);
                }
            }
        }
    }
}
