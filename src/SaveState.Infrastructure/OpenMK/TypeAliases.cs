// Type aliases for backward compatibility after OpenMKService refactoring.
// These aliases ensure existing code continues to work without modification.
// Deprecated: Use SaveState.Infrastructure.OpenMK.Services.OpenMK namespace instead.

using Microsoft.Extensions.Logging;
using SaveState.Infrastructure.OpenMK.Services.OpenMK;

#pragma warning disable CS0618 // Type or member is obsolete

namespace SaveState.Infrastructure.OpenMK;

/// <summary>
/// Backward compatibility alias for OpenMKService.
/// </summary>
[Obsolete("Use SaveState.Infrastructure.OpenMK.Services.OpenMK.OpenMKService instead")]
public class OpenMKServiceAlias : OpenMKService
{
    public OpenMKServiceAlias(
        Core.OpenMK.Repositories.IOpenMKCharacterRepository characterRepository,
        Core.OpenMK.Repositories.IOpenMKProgressRepository progressRepository,
        Core.OpenMK.Repositories.IOpenMKMatchStateRepository matchStateRepository,
        ILoggerFactory loggerFactory)
        : base(characterRepository, progressRepository, matchStateRepository, loggerFactory)
    {
    }
}

/// <summary>
/// Backward compatibility typedef - OpenMK service is now in Services.OpenMK namespace.
/// </summary>
public static class OpenMKTypeAliases
{
    /// <summary>
    /// The actual OpenMKService type in its new location.
    /// </summary>
    public static Type Service => typeof(OpenMKService);
}
