using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SaveState.Application.MobileCompanion;
using SaveState.Application.MobileCompanion.Commands;
using SaveState.Application.MobileCompanion.Queries;
using SaveState.Core.Common;
using SaveState.Core.MobileCompanion.Models;
using MediatR;

namespace SaveState.Infrastructure.MobileCompanion.Controllers;

/// <summary>
/// REST API controller for mobile companion functionality.
/// </summary>
[ApiController]
[Route("api/mobile")]
[Produces("application/json")]
public class MobileCompanionController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<MobileCompanionController> _logger;

    public MobileCompanionController(IMediator mediator, ILogger<MobileCompanionController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Generates a new pairing code for device pairing.
    /// </summary>
    /// <returns>A pairing request containing the 6-digit code.</returns>
    [HttpGet("pairing/code")]
    [ProducesResponseType(typeof(PairingRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PairingRequestDto>> GeneratePairingCode()
    {
        _logger.LogInformation("Generating new pairing code");

        var result = await _mediator.Send(new CreatePairingRequestCommand());

        if (result.IsFailure)
        {
            _logger.LogError("Failed to generate pairing code: {Error}", result.Error);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ProblemDetails { Title = "Pairing Error", Detail = result.Error });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Completes device pairing using a pairing code.
    /// </summary>
    /// <param name="request">The pairing completion request.</param>
    /// <returns>The paired mobile device information.</returns>
    [HttpPost("pairing/complete")]
    [ProducesResponseType(typeof(MobileDeviceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MobileDeviceDto>> CompletePairing([FromBody] CompletePairingRequest request)
    {
        _logger.LogInformation("Completing pairing with code {PairingCode}", request.PairingCode);

        var deviceInfo = new DeviceInfoDto
        {
            DeviceName = request.DeviceName,
            DeviceType = request.DeviceType,
            DeviceModel = request.DeviceModel,
            OsVersion = request.OsVersion,
            AppVersion = request.AppVersion,
            PushNotificationToken = request.PushNotificationToken
        };

        var result = await _mediator.Send(new CompletePairingCommand(request.PairingCode, deviceInfo));

        if (result.IsFailure)
        {
            if (result.ErrorType == ErrorType.NotFound)
            {
                return NotFound(new ProblemDetails { Title = "Pairing Code Not Found", Detail = result.Error });
            }

            return BadRequest(new ProblemDetails { Title = "Pairing Failed", Detail = result.Error });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Lists all paired devices.
    /// </summary>
    /// <returns>List of paired mobile devices.</returns>
    [HttpGet("devices")]
    [ProducesResponseType(typeof(IReadOnlyList<MobileDeviceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MobileDeviceDto>>> GetPairedDevices()
    {
        var result = await _mediator.Send(new GetPairedDevicesQuery());

        if (result.IsFailure)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ProblemDetails { Title = "Error", Detail = result.Error });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Gets a specific paired device.
    /// </summary>
    /// <param name="id">The device ID.</param>
    /// <returns>The mobile device information.</returns>
    [HttpGet("devices/{id:guid}")]
    [ProducesResponseType(typeof(MobileDeviceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MobileDeviceDto>> GetDevice(Guid id)
    {
        var result = await _mediator.Send(new GetDeviceQuery(id));

        if (result.IsFailure)
        {
            if (result.ErrorType == ErrorType.NotFound)
            {
                return NotFound(new ProblemDetails { Title = "Device Not Found", Detail = result.Error });
            }

            return StatusCode(StatusCodes.Status500InternalServerError,
                new ProblemDetails { Title = "Error", Detail = result.Error });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Unpairs a device.
    /// </summary>
    /// <param name="id">The device ID to unpair.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete("devices/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnpairDevice(Guid id)
    {
        _logger.LogInformation("Unpairing device {DeviceId}", id);

        var result = await _mediator.Send(new UnpairDeviceCommand(id));

        if (result.IsFailure)
        {
            if (result.ErrorType == ErrorType.NotFound)
            {
                return NotFound(new ProblemDetails { Title = "Device Not Found", Detail = result.Error });
            }

            return StatusCode(StatusCodes.Status500InternalServerError,
                new ProblemDetails { Title = "Error", Detail = result.Error });
        }

        return NoContent();
    }

    /// <summary>
    /// Gets the game library information for sync.
    /// </summary>
    /// <returns>Library sync information.</returns>
    [HttpGet("library")]
    [ProducesResponseType(typeof(LibrarySyncInfoDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<LibrarySyncInfoDto>> GetLibrary()
    {
        var result = await _mediator.Send(new GetLibrarySyncInfoQuery());

        if (result.IsFailure)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ProblemDetails { Title = "Error", Detail = result.Error });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Gets the current system status.
    /// </summary>
    /// <returns>System status information.</returns>
    [HttpGet("status")]
    [ProducesResponseType(typeof(SystemStatusDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SystemStatusDto>> GetSystemStatus()
    {
        var result = await _mediator.Send(new GetSystemStatusQuery());

        if (result.IsFailure)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ProblemDetails { Title = "Error", Detail = result.Error });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Sends a notification to a specific device.
    /// </summary>
    /// <param name="deviceId">The target device ID.</param>
    /// <param name="request">The notification request.</param>
    /// <returns>No content on success.</returns>
    [HttpPost("notify/{deviceId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SendNotification(Guid deviceId, [FromBody] SendNotificationRequest request)
    {
        _logger.LogInformation("Sending notification to device {DeviceId}", deviceId);

        // This would typically be handled by the service directly
        // For now, returning not implemented
        return StatusCode(StatusCodes.Status501NotImplemented,
            new ProblemDetails { Title = "Not Implemented", Detail = "Direct notification endpoint not yet implemented" });
    }

    /// <summary>
    /// Gets active remote sessions.
    /// </summary>
    /// <returns>List of active sessions.</returns>
    [HttpGet("sessions")]
    [ProducesResponseType(typeof(IReadOnlyList<RemoteSessionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RemoteSessionDto>>> GetActiveSessions()
    {
        var result = await _mediator.Send(new GetActiveSessionsQuery());

        if (result.IsFailure)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ProblemDetails { Title = "Error", Detail = result.Error });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Updates device permissions.
    /// </summary>
    /// <param name="deviceId">The device ID.</param>
    /// <param name="request">The permissions update request.</param>
    /// <returns>No content on success.</returns>
    [HttpPut("devices/{deviceId:guid}/permissions")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePermissions(Guid deviceId, [FromBody] UpdatePermissionsRequest request)
    {
        var result = await _mediator.Send(new UpdateDevicePermissionsCommand(deviceId, request.Permissions));

        if (result.IsFailure)
        {
            if (result.ErrorType == ErrorType.NotFound)
            {
                return NotFound(new ProblemDetails { Title = "Device Not Found", Detail = result.Error });
            }

            return BadRequest(new ProblemDetails { Title = "Update Failed", Detail = result.Error });
        }

        return NoContent();
    }
}

/// <summary>
/// Request to complete device pairing.
/// </summary>
public sealed record CompletePairingRequest
{
    public string PairingCode { get; init; } = string.Empty;
    public string DeviceName { get; init; } = string.Empty;
    public string DeviceType { get; init; } = string.Empty;
    public string? DeviceModel { get; init; }
    public string? OsVersion { get; init; }
    public string? AppVersion { get; init; }
    public string? PushNotificationToken { get; init; }
}

/// <summary>
/// Request to send a notification.
/// </summary>
public sealed record SendNotificationRequest
{
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public NotificationType Type { get; init; } = NotificationType.Info;
    public string? ActionUrl { get; init; }
    public Dictionary<string, string>? Data { get; init; }
}

/// <summary>
/// Request to update device permissions.
/// </summary>
public sealed record UpdatePermissionsRequest
{
    public List<string> Permissions { get; init; } = new();
}
