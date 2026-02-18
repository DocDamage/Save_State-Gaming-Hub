namespace SaveState.Application.Mugen.Services.RealityWarping.Engines;

using Microsoft.Extensions.Logging;
using SaveState.Application.Mugen.Models.RealityWarping;

public class DistortionEngine
{
    private readonly ILogger<DistortionEngine> _logger;

    public DistortionEngine(ILogger<DistortionEngine> logger) => _logger = logger;

    /// <summary>
    /// Generates an energy signature for a dimensional rift based on its type.
    /// </summary>
    public string GenerateEnergySignature(RiftType riftType)
    {
        string baseSignature = riftType switch
        {
            RiftType.Portal => "PORTAL",
            RiftType.Wormhole => "WORMHOLE",
            RiftType.Fold => "SPATIAL_FOLD",
            RiftType.Bridge => "DIMENSIONAL_BRIDGE",
            _ => "UNKNOWN"
        };

        string uniqueId = Guid.NewGuid().ToString("N")[..8].ToUpper();
        string signature = $"{baseSignature}_{uniqueId}";

        _logger.LogDebug("Generated energy signature: {Signature} for rift type {RiftType}",
            signature, riftType);

        return signature;
    }
}
