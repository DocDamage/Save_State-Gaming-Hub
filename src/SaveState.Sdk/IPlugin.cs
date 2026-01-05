using System.Threading;
using System.Threading.Tasks;

namespace SaveState.Sdk;

/// <summary>
/// Core interface that all plugins must implement.
/// </summary>
public interface IPlugin
{
    string Id { get; }
    string Name { get; }
    string Version { get; }
    string Author { get; }
    string? Description { get; }

    PluginCapabilities Capabilities { get; }

    Task InitializeAsync(IPluginContext context, CancellationToken ct = default);
    Task ShutdownAsync(CancellationToken ct = default);
}
