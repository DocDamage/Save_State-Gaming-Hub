using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;

namespace SaveState.Application.Mugen.Services.SymbioticPartner;

/// <summary>
/// Manages communication between player and symbiotic partner.
/// Handles message processing, response generation, and emotional state updates.
/// </summary>
public class CommunicationManager
{
    private readonly ILogger<CommunicationManager> _logger;
    private readonly ITimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommunicationManager"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="timeProvider">The time provider for timestamp generation.</param>
    public CommunicationManager(ILogger<CommunicationManager> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Processes communication with a symbiotic partner and generates a response.
    /// Updates trust and bond levels based on the message type.
    /// </summary>
    /// <param name="partner">The symbiotic partner to communicate with.</param>
    /// <param name="request">The communication request containing message details.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the communication response.</returns>
    public async Task<Result<SymbioticPartnerServiceCommunicationResponse>> CommunicateWithPartnerAsync(
        SymbioticPartnerServiceSymbioticPartner partner,
        SymbioticPartnerServiceCommunicationRequest request,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation(
                "Communicating with partner {PartnerId}: {MessageType}",
                partner.PartnerId,
                request.SymbioticPartnerServiceMessageType);

            var response = await ProcessCommunicationAsync(partner, request, ct);

            // Update trust and bond based on communication
            partner.TrustLevel = Math.Min(partner.TrustLevel + response.TrustChange, 1.0f);
            partner.BondStrength = Math.Min(partner.BondStrength + response.BondChange, 1.0f);
            partner.LastInteraction = _timeProvider.UtcNow;

            return Result<SymbioticPartnerServiceCommunicationResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error communicating with partner {PartnerId}", partner.PartnerId);
            return Result<SymbioticPartnerServiceCommunicationResponse>.Failure(
                $"Communication failed: {ex.Message}",
                ErrorType.Internal);
        }
    }

    /// <summary>
    /// Processes the communication request and generates an appropriate response.
    /// </summary>
    /// <param name="partner">The symbiotic partner.</param>
    /// <param name="request">The communication request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The communication response.</returns>
    public Task<SymbioticPartnerServiceCommunicationResponse> ProcessCommunicationAsync(
        SymbioticPartnerServiceSymbioticPartner partner,
        SymbioticPartnerServiceCommunicationRequest request,
        CancellationToken ct)
    {
        // Process communication with partner
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

        return Task.FromResult(response);
    }

    /// <summary>
    /// Determines the response type based on the incoming message type.
    /// </summary>
    /// <param name="messageType">The type of message received.</param>
    /// <returns>The appropriate response type.</returns>
    private SymbioticPartnerServiceResponseType DetermineResponseType(SymbioticPartnerServiceMessageType messageType)
    {
        // Determine response type based on message type
        return messageType switch
        {
            SymbioticPartnerServiceMessageType.Encouragement => SymbioticPartnerServiceResponseType.Positive,
            SymbioticPartnerServiceMessageType.Criticism => SymbioticPartnerServiceResponseType.Constructive,
            SymbioticPartnerServiceMessageType.Praise => SymbioticPartnerServiceResponseType.Grateful,
            _ => SymbioticPartnerServiceResponseType.Neutral
        };
    }

    /// <summary>
    /// Generates a response message based on the partner's personality and the request.
    /// </summary>
    /// <param name="partner">The symbiotic partner.</param>
    /// <param name="request">The communication request.</param>
    /// <returns>The generated response message.</returns>
    private string GenerateResponseMessage(
        SymbioticPartnerServiceSymbioticPartner partner,
        SymbioticPartnerServiceCommunicationRequest request)
    {
        // Generate appropriate response message
        return request.SymbioticPartnerServiceMessageType switch
        {
            SymbioticPartnerServiceMessageType.Encouragement => $"{partner.Name} seems motivated!",
            SymbioticPartnerServiceMessageType.Praise => $"{partner.Name} appreciates the recognition!",
            SymbioticPartnerServiceMessageType.Request => $"{partner.Name} acknowledges the request.",
            _ => $"{partner.Name} responds thoughtfully."
        };
    }

    /// <summary>
    /// Calculates the emotional response intensity based on the message type.
    /// </summary>
    /// <param name="partner">The symbiotic partner.</param>
    /// <param name="request">The communication request.</param>
    /// <returns>The emotional response intensity (0.0 to 1.0).</returns>
    private float CalculateEmotionalResponse(
        SymbioticPartnerServiceSymbioticPartner partner,
        SymbioticPartnerServiceCommunicationRequest request)
    {
        // Calculate emotional response intensity
        return request.SymbioticPartnerServiceMessageType switch
        {
            SymbioticPartnerServiceMessageType.Encouragement => 0.8f,
            SymbioticPartnerServiceMessageType.Praise => 0.9f,
            SymbioticPartnerServiceMessageType.Criticism => 0.6f,
            _ => 0.5f
        };
    }
}
