using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Application.Mugen.Services;
using Microsoft.Extensions.Logging;

namespace SaveState.Application.Mugen.Managers;

/// <summary>
/// Manager for partner communication operations.
/// </summary>
public class CommunicationManager
{
    private readonly ILogger<CommunicationManager> _logger;
    private readonly ITimeProvider _timeProvider;

    public CommunicationManager(ILogger<CommunicationManager> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task<Result<SymbioticPartnerServiceCommunicationResponse>> ProcessCommunicationAsync(
        SymbioticPartnerServiceSymbioticPartner partner,
        SymbioticPartnerServiceCommunicationRequest request,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Communicating with partner {PartnerId}: {SymbioticPartnerServiceMessageType}", partner.PartnerId, request.SymbioticPartnerServiceMessageType);

            var response = new SymbioticPartnerServiceCommunicationResponse
            {
                PartnerId = partner.PartnerId,
                SymbioticPartnerServiceResponseType = DetermineResponseType(request.SymbioticPartnerServiceMessageType),
                Message = GenerateResponseMessage(partner, request),
                EmotionalResponse = CalculateEmotionalResponse(partner, request),
                TrustChange = request.SymbioticPartnerServiceMessageType == SymbioticPartnerServiceMessageType.Encouragement ? 0.02f : 0.0f,
                BondChange = request.SymbioticPartnerServiceMessageType == SymbioticPartnerServiceMessageType.Praise ? 0.01f : 0.0f,
                Timestamp = _timeProvider.UtcNow
            };

            return Result.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error communicating with partner {PartnerId}", partner.PartnerId);
            return Result.Failure<SymbioticPartnerServiceCommunicationResponse>($"Communication failed: {ex.Message}");
        }
    }

    private SymbioticPartnerServiceResponseType DetermineResponseType(SymbioticPartnerServiceMessageType messageType)
    {
        return messageType switch
        {
            SymbioticPartnerServiceMessageType.Encouragement => SymbioticPartnerServiceResponseType.Positive,
            SymbioticPartnerServiceMessageType.Criticism => SymbioticPartnerServiceResponseType.Constructive,
            SymbioticPartnerServiceMessageType.Praise => SymbioticPartnerServiceResponseType.Grateful,
            _ => SymbioticPartnerServiceResponseType.Neutral
        };
    }

    private string GenerateResponseMessage(SymbioticPartnerServiceSymbioticPartner partner, SymbioticPartnerServiceCommunicationRequest request)
    {
        return request.SymbioticPartnerServiceMessageType switch
        {
            SymbioticPartnerServiceMessageType.Encouragement => $"{partner.Name} seems motivated!",
            SymbioticPartnerServiceMessageType.Praise => $"{partner.Name} appreciates the recognition!",
            SymbioticPartnerServiceMessageType.Request => $"{partner.Name} acknowledges the request.",
            _ => $"{partner.Name} responds thoughtfully."
        };
    }

    private float CalculateEmotionalResponse(SymbioticPartnerServiceSymbioticPartner partner, SymbioticPartnerServiceCommunicationRequest request)
    {
        return request.SymbioticPartnerServiceMessageType switch
        {
            SymbioticPartnerServiceMessageType.Encouragement => 0.8f,
            SymbioticPartnerServiceMessageType.Praise => 0.9f,
            SymbioticPartnerServiceMessageType.Criticism => 0.6f,
            _ => 0.5f
        };
    }
}
