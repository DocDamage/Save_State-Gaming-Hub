using SaveState.Core.Interfaces;
using Serilog;

namespace SaveState.Core.Services;

public class ProviderManager
{
    private readonly List<IGameProvider> _providers = new();
    private readonly ILogger _logger;

    public ProviderManager()
    {
        _logger = Log.ForContext<ProviderManager>();
    }

    public void RegisterProvider(IGameProvider provider)
    {
        if (_providers.Any(p => p.Id == provider.Id))
        {
            _logger.Warning("Provider with ID {Id} is already registered.", provider.Id);
            return;
        }

        _logger.Information("Registering provider: {Name} ({Id})", provider.Name, provider.Id);
        _providers.Add(provider);
    }

    public IEnumerable<IGameProvider> GetProviders() => _providers;

    public IGameProvider? GetProvider(string id) => _providers.FirstOrDefault(p => p.Id == id);
}
