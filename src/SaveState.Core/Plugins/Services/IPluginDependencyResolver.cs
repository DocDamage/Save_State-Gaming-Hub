using SaveState.Core.Common;
using System.Reflection;

namespace SaveState.Core.Plugins.Services;

/// <summary>
/// Service for resolving plugin dependencies and determining load order.
/// </summary>
public interface IPluginDependencyResolver
{
    /// <summary>
    /// Analyzes a plugin type to extract its dependencies.
    /// </summary>
    PluginDependencyInfo GetDependencies(Type pluginType);

    /// <summary>
    /// Resolves the load order for a set of plugins based on their dependencies.
    /// Returns plugins in the order they should be loaded.
    /// </summary>
    Result<IReadOnlyList<PluginLoadOrderEntry>> ResolveLoadOrder(IEnumerable<PluginDiscoveryInfo> plugins);

    /// <summary>
    /// Validates that all required dependencies are satisfied.
    /// </summary>
    Result ValidateDependencies(
        string pluginId,
        IEnumerable<PluginDependency> dependencies,
        IReadOnlyDictionary<string, string> loadedPlugins);

    /// <summary>
    /// Checks for conflicts between plugins.
    /// </summary>
    IReadOnlyList<PluginConflictInfo> DetectConflicts(IEnumerable<PluginDiscoveryInfo> plugins);
}

/// <summary>
/// Information about a discovered plugin for dependency resolution.
/// </summary>
public sealed record PluginDiscoveryInfo(
    string PluginId,
    string Name,
    string Version,
    Type PluginType,
    string Path,
    IReadOnlyList<PluginDependency> Dependencies,
    IReadOnlyList<PluginConflict> Conflicts);

/// <summary>
/// Full dependency information for a plugin.
/// </summary>
public sealed record PluginDependencyInfo(
    IReadOnlyList<PluginDependency> Dependencies,
    IReadOnlyList<PluginConflict> Conflicts);

/// <summary>
/// Entry in the resolved plugin load order.
/// </summary>
public sealed record PluginLoadOrderEntry(
    PluginDiscoveryInfo Plugin,
    int LoadOrder,
    IReadOnlyList<string> DependencyChain,
    bool IsOptional);

/// <summary>
/// Information about a detected conflict.
/// </summary>
public sealed record PluginConflictInfo(
    string PluginA,
    string PluginB,
    string Reason,
    ConflictType Type);

/// <summary>
/// Type of plugin conflict.
/// </summary>
public enum ConflictType
{
    /// <summary>Explicit conflict declared via attribute</summary>
    DeclaredConflict,

    /// <summary>Circular dependency detected</summary>
    CircularDependency,

    /// <summary>Multiple plugins provide the same capability exclusively</summary>
    ExclusiveCapability,

    /// <summary>Version mismatch</summary>
    VersionMismatch
}

/// <summary>
/// Default implementation of the plugin dependency resolver.
/// </summary>
public sealed class PluginDependencyResolver : IPluginDependencyResolver
{
    public PluginDependencyInfo GetDependencies(Type pluginType)
    {
        var dependencies = new List<PluginDependency>();
        var conflicts = new List<PluginConflict>();

        // Get DependsOn attributes
        var dependsOnAttrs = pluginType.GetCustomAttributes<DependsOnPluginAttribute>();
        foreach (var attr in dependsOnAttrs)
        {
            dependencies.Add(new PluginDependency(
                attr.PluginId,
                attr.Optional,
                attr.MinVersion));
        }

        // Get ConflictsWith attributes
        var conflictsWithAttrs = pluginType.GetCustomAttributes<ConflictsWithPluginAttribute>();
        foreach (var attr in conflictsWithAttrs)
        {
            conflicts.Add(new PluginConflict(attr.PluginId, attr.Reason));
        }

        return new PluginDependencyInfo(dependencies, conflicts);
    }

    public Result<IReadOnlyList<PluginLoadOrderEntry>> ResolveLoadOrder(IEnumerable<PluginDiscoveryInfo> plugins)
    {
        var pluginList = plugins.ToList();
        var pluginMap = pluginList.ToDictionary(p => p.PluginId, StringComparer.OrdinalIgnoreCase);

        // Check for circular dependencies first
        var circularCheck = DetectCircularDependencies(pluginList, pluginMap);
        if (!circularCheck.IsSuccess)
        {
            return Result.Failure<IReadOnlyList<PluginLoadOrderEntry>>(
                circularCheck.Error!, Common.ErrorType.Validation);
        }

        // Topological sort using Kahn's algorithm
        var result = new List<PluginLoadOrderEntry>();
        var inDegree = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var dependencyChains = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        // Initialize in-degrees and dependency chains
        foreach (var plugin in pluginList)
        {
            inDegree[plugin.PluginId] = 0;
            dependencyChains[plugin.PluginId] = new List<string>();
        }

        // Calculate in-degrees (number of dependencies)
        foreach (var plugin in pluginList)
        {
            foreach (var dep in plugin.Dependencies.Where(d => !d.IsOptional))
            {
                if (pluginMap.ContainsKey(dep.PluginId))
                {
                    inDegree[plugin.PluginId]++;
                    dependencyChains[plugin.PluginId].Add(dep.PluginId);
                }
            }
        }

        // Process plugins with no dependencies first
        var queue = new Queue<string>();
        foreach (var (pluginId, degree) in inDegree)
        {
            if (degree == 0)
                queue.Enqueue(pluginId);
        }

        var loadOrder = 0;
        while (queue.Count > 0)
        {
            var pluginId = queue.Dequeue();
            var plugin = pluginMap[pluginId];

            result.Add(new PluginLoadOrderEntry(
                plugin,
                loadOrder++,
                dependencyChains[pluginId],
                false));

            // Reduce in-degree of dependent plugins
            foreach (var otherPlugin in pluginList)
            {
                if (otherPlugin.Dependencies.Any(d =>
                    !d.IsOptional &&
                    d.PluginId.Equals(pluginId, StringComparison.OrdinalIgnoreCase)))
                {
                    inDegree[otherPlugin.PluginId]--;
                    if (inDegree[otherPlugin.PluginId] == 0)
                    {
                        queue.Enqueue(otherPlugin.PluginId);
                    }
                }
            }
        }

        // Check if all plugins were processed
        if (result.Count != pluginList.Count)
        {
            var unprocessed = pluginList
                .Where(p => !result.Any(r => r.Plugin.PluginId == p.PluginId))
                .Select(p => p.PluginId);
            return Result.Failure<IReadOnlyList<PluginLoadOrderEntry>>(
                $"Could not resolve dependencies for: {string.Join(", ", unprocessed)}",
                Common.ErrorType.Validation);
        }

        return Result.Success<IReadOnlyList<PluginLoadOrderEntry>>(result);
    }

    public Result ValidateDependencies(
        string pluginId,
        IEnumerable<PluginDependency> dependencies,
        IReadOnlyDictionary<string, string> loadedPlugins)
    {
        var errors = new List<string>();

        foreach (var dep in dependencies)
        {
            if (!loadedPlugins.TryGetValue(dep.PluginId, out var loadedVersion))
            {
                if (!dep.IsOptional)
                {
                    errors.Add($"Required dependency '{dep.PluginId}' is not loaded");
                }
                continue;
            }

            // Check version requirement
            if (!string.IsNullOrEmpty(dep.MinVersion))
            {
                if (!IsVersionSatisfied(loadedVersion, dep.MinVersion))
                {
                    errors.Add($"Dependency '{dep.PluginId}' version {loadedVersion} does not meet minimum {dep.MinVersion}");
                }
            }
        }

        return errors.Count == 0
            ? Result.Success()
            : Result.Failure($"Plugin '{pluginId}' has unmet dependencies: {string.Join("; ", errors)}",
                Common.ErrorType.Validation);
    }

    public IReadOnlyList<PluginConflictInfo> DetectConflicts(IEnumerable<PluginDiscoveryInfo> plugins)
    {
        var conflicts = new List<PluginConflictInfo>();
        var pluginList = plugins.ToList();

        for (int i = 0; i < pluginList.Count; i++)
        {
            for (int j = i + 1; j < pluginList.Count; j++)
            {
                var pluginA = pluginList[i];
                var pluginB = pluginList[j];

                // Check declared conflicts
                var conflictFromA = pluginA.Conflicts
                    .FirstOrDefault(c => c.PluginId.Equals(pluginB.PluginId, StringComparison.OrdinalIgnoreCase));

                var conflictFromB = pluginB.Conflicts
                    .FirstOrDefault(c => c.PluginId.Equals(pluginA.PluginId, StringComparison.OrdinalIgnoreCase));

                if (conflictFromA != null)
                {
                    conflicts.Add(new PluginConflictInfo(
                        pluginA.PluginId,
                        pluginB.PluginId,
                        conflictFromA.Reason ?? $"'{pluginA.Name}' declares a conflict with '{pluginB.Name}'",
                        ConflictType.DeclaredConflict));
                }
                else if (conflictFromB != null)
                {
                    conflicts.Add(new PluginConflictInfo(
                        pluginA.PluginId,
                        pluginB.PluginId,
                        conflictFromB.Reason ?? $"'{pluginB.Name}' declares a conflict with '{pluginA.Name}'",
                        ConflictType.DeclaredConflict));
                }
            }
        }

        return conflicts;
    }

    private Result DetectCircularDependencies(
        List<PluginDiscoveryInfo> plugins,
        Dictionary<string, PluginDiscoveryInfo> pluginMap)
    {
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var recursionStack = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var path = new List<string>();

        foreach (var plugin in plugins)
        {
            if (!visited.Contains(plugin.PluginId))
            {
                var cycle = DetectCycleRecursive(plugin.PluginId, pluginMap, visited, recursionStack, path);
                if (cycle != null)
                {
                    return Result.Failure($"Circular dependency detected: {string.Join(" -> ", cycle)}",
                        Common.ErrorType.Validation);
                }
            }
        }

        return Result.Success();
    }

    private List<string>? DetectCycleRecursive(
        string pluginId,
        Dictionary<string, PluginDiscoveryInfo> pluginMap,
        HashSet<string> visited,
        HashSet<string> recursionStack,
        List<string> path)
    {
        visited.Add(pluginId);
        recursionStack.Add(pluginId);
        path.Add(pluginId);

        if (pluginMap.TryGetValue(pluginId, out var plugin))
        {
            foreach (var dep in plugin.Dependencies.Where(d => !d.IsOptional))
            {
                if (!visited.Contains(dep.PluginId))
                {
                    var cycle = DetectCycleRecursive(dep.PluginId, pluginMap, visited, recursionStack, path);
                    if (cycle != null)
                        return cycle;
                }
                else if (recursionStack.Contains(dep.PluginId))
                {
                    // Found a cycle - return the cycle path
                    var cycleStart = path.IndexOf(dep.PluginId);
                    var cyclePath = path.Skip(cycleStart).ToList();
                    cyclePath.Add(dep.PluginId);
                    return cyclePath;
                }
            }
        }

        recursionStack.Remove(pluginId);
        path.RemoveAt(path.Count - 1);
        return null;
    }

    private static bool IsVersionSatisfied(string actual, string minimum)
    {
        try
        {
            var actualVersion = new Version(actual);
            var minVersion = new Version(minimum);
            return actualVersion >= minVersion;
        }
        catch
        {
            // Fall back to string comparison
            return string.Compare(actual, minimum, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
