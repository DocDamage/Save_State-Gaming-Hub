using System.Threading.Tasks;

namespace SaveState.Sdk;

public interface IPluginContext
{
    string DataDirectory { get; }
    Task RegisterGameProviderAsync(IGameProvider provider);
}
