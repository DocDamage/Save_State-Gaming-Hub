using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Serilog;

namespace SaveState.Core.Services.Ai;

/// <summary>
/// Manages A/B testing experiments for AI responses.
/// Provides functionality to create, manage, and track experiments.
/// </summary>
public class ExperimentManager
{
    private readonly ILogger _logger = Log.ForContext<ExperimentManager>();
    private readonly ConcurrentDictionary<string, ExperimentConfig> _experiments = new();
    private readonly ConcurrentDictionary<string, string> _experimentAssignments = new();
    private readonly Random _random = new();

    /// <summary>
    /// Registers a new experiment configuration.
    /// </summary>
    public void RegisterExperiment(ExperimentConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.Id))
            throw new ArgumentException("Experiment ID cannot be empty", nameof(config.Id));

        if (config.Variants == null || config.Variants.Length == 0)
            throw new ArgumentException("Experiment must have at least one variant", nameof(config.Variants));

        _experiments[config.Id] = config;
        _logger.Information("Registered experiment: {ExperimentId} with {VariantCount} variants",
            config.Id, config.Variants.Length);
    }

    /// <summary>
    /// Ends an experiment and cleans up assignments.
    /// </summary>
    public void EndExperiment(string experimentId)
    {
        if (_experiments.TryRemove(experimentId, out var config))
        {
            // Remove all assignments for this experiment
            var keysToRemove = new List<string>();
            foreach (var kvp in _experimentAssignments)
            {
                if (kvp.Key.StartsWith($"{experimentId}:"))
                {
                    keysToRemove.Add(kvp.Key);
                }
            }

            foreach (var key in keysToRemove)
            {
                _experimentAssignments.TryRemove(key, out _);
            }

            _logger.Information("Ended experiment: {ExperimentId}, removed {AssignmentCount} assignments",
                experimentId, keysToRemove.Count);
        }
    }

    /// <summary>
    /// Gets the assigned variant for a user in a specific experiment.
    /// Uses consistent hashing to ensure users get the same variant across sessions.
    /// </summary>
    public string? GetAssignedVariant(string userId, string experimentId)
    {
        if (!_experiments.TryGetValue(experimentId, out var config))
        {
            _logger.Warning("Experiment not found: {ExperimentId}", experimentId);
            return null;
        }

        var assignmentKey = $"{experimentId}:{userId}";

        // Check if user is already assigned
        if (_experimentAssignments.TryGetValue(assignmentKey, out var assignedVariant))
        {
            return assignedVariant;
        }

        // Assign user to a variant based on experiment configuration
        var variant = AssignVariant(userId, config);
        _experimentAssignments[assignmentKey] = variant;

        _logger.Debug("Assigned user {UserId} to variant {Variant} in experiment {ExperimentId}",
            userId, variant, experimentId);

        return variant;
    }

    /// <summary>
    /// Gets all active experiments.
    /// </summary>
    public IEnumerable<ExperimentConfig> GetActiveExperiments()
    {
        return _experiments.Values;
    }

    /// <summary>
    /// Gets an experiment by ID.
    /// </summary>
    public ExperimentConfig? GetExperiment(string experimentId)
    {
        return _experiments.TryGetValue(experimentId, out var config) ? config : null;
    }

    /// <summary>
    /// Gets assignment statistics for an experiment.
    /// </summary>
    public Dictionary<string, int> GetExperimentStats(string experimentId)
    {
        var stats = new Dictionary<string, int>();

        foreach (var kvp in _experimentAssignments)
        {
            if (kvp.Key.StartsWith($"{experimentId}:"))
            {
                var variant = kvp.Value;
                stats[variant] = stats.GetValueOrDefault(variant, 0) + 1;
            }
        }

        return stats;
    }

    private string AssignVariant(string userId, ExperimentConfig config)
    {
        // Use user ID for consistent assignment (deterministic)
        var hash = Math.Abs(userId.GetHashCode());
        var totalWeight = 0;

        // Calculate total weight
        foreach (var variant in config.Variants)
        {
            totalWeight += variant.Weight;
        }

        // Use hash to determine assignment
        var assignment = hash % totalWeight;

        // Find which variant this assignment falls into
        var currentWeight = 0;
        foreach (var variant in config.Variants)
        {
            currentWeight += variant.Weight;
            if (assignment < currentWeight)
            {
                return variant.Name;
            }
        }

        // Fallback to first variant (shouldn't happen)
        return config.Variants[0].Name;
    }
}
