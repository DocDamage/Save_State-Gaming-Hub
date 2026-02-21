# SaveState Reborn Plugin SDK

The **SaveState Plugin SDK** (`SaveState.Sdk`) is the formalized API for creating third-party extensions for SaveState Reborn. It provides a stable contract that ensures forward compatibility and isolation from core internal changes.

## 📦 Architecture

The SDK separates the **Plugin Contracts** from the **Application Implementation**.

* **Interfaces**: `IPlugin`, `IPluginContext`, `IGameProvider`.
* **DTOs**: Lightweight Data Transfer Objects (e.g., `GameInfo`) replace Entity Framework entities to prevent database coupling.

## 🚀 Getting Started

### 1. Create a Project

Create a new .NET 9 Class Library:

```bash
dotnet new classlib -n My.Awesome.Plugin
dotnet add reference path/to/SaveState.Sdk.csproj
```

### 2. Implement IPlugin

The entry point for every plugin is a class implementing `SaveState.Sdk.IPlugin`.

```csharp
using SaveState.Sdk;
using System.Threading;
using System.Threading.Tasks;

public class MyPlugin : IPlugin
{
    public string Id => "com.example.myplugin";
    public string Name => "My Plugin";
    public string Version => "1.0.0";
    public string Author => "Me";
    public string? Description => "Adds awesome features.";

    public PluginCapabilities Capabilities => PluginCapabilities.None;

    public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        // Initialization logic here
        return Task.CompletedTask;
    }

    public Task ShutdownAsync(CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }
}
```

## 🔌 Extending Functionality

### Game Providers

To add support for a new game store or library, implement `IGameProvider` and register it in `InitializeAsync`.

```csharp
public class MyGameProvider : IGameProvider
{
    public string Name => "My Store";

    public async Task<IReadOnlyList<GameInfo>> GetInstalledGamesAsync(CancellationToken ct)
    {
        return new List<GameInfo>
        {
            new GameInfo { Title = "Example Game", InstallPath = "C:\\Games\\Example" }
        };
    }
}

// In MyPlugin.InitializeAsync:
await context.RegisterGameProviderAsync(new MyGameProvider());
```

## ⚠️ Migration Note (Phase 6)

We are currently in the process of migrating internal plugins (`SaveState.Plugins.*`) to use `SaveState.Sdk`.
Legacy plugins currently reference `SaveState.Core` directly. New third-party plugins **should only reference SaveState.Sdk** to ensure stability.
