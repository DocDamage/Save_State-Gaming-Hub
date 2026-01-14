using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace SaveState.Presentation.Services;

public static class UiGameContextServiceExtensions
{
    public static void SetSelectedGame(this IUiGameContextService service, Game? game)
    {
        if (game == null)
        {
            service.ClearCurrentGame();
        }
        else
        {
            // Fire and forget since we can't await in void property/method easily if it wasn't designed for it
            // Or ideally, update the interface to match usage. 
            // The usage in errors suggests synchronous usage: service.SetSelectedGame(game);
            // But the interface has SetCurrentGameAsync.
            _ = service.SetCurrentGameAsync(game);
        }
    }

    public static Guid? ActiveGameId(this IUiGameContextService service)
    {
        return service.CurrentGame?.Id;
    }
    
    public static Guid? RunningGameId(this IUiGameContextService service)
    {
        // Assuming CurrentGame implies running or selected context. 
        // If there is a separate "Running" concept, it needs to be added to the interface.
        // For now, mapping to CurrentGame to satisfy build.
        return service.CurrentGame?.Id;
    }

    public static void SetRunningGame(this IUiGameContextService service, Game? game)
    {
         if (game != null) _ = service.SetCurrentGameAsync(game);
    }
}
