using MediatR;
using Microsoft.Extensions.Logging;
using SaveState.Application.CloudServices.Services;
using SaveState.Application.Common;
using SaveState.Core.Common;
using SaveState.Core.Common.Enums;
using SaveState.Core.Common.ValueObjects;

namespace SaveState.Application.CloudServices.Commands.Handlers;

public class CreateBackupCommandHandler : IRequestHandler<CreateBackupCommand, Result<BackupId>>
{
    private readonly IBackupService _backupService;
    private readonly ILogger<CreateBackupCommandHandler> _logger;

    public CreateBackupCommandHandler(
        IBackupService backupService,
        ILogger<CreateBackupCommandHandler> logger)
    {
        _backupService = backupService;
        _logger = logger;
    }

    public async Task<Result<BackupId>> Handle(CreateBackupCommand request, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Creating backup of type {Type} with name '{Name}'",
                request.Type, request.Name ?? "unnamed");

            var result = await _backupService.CreateBackupAsync(
                request.Type,
                request.Name,
                request.GameIds,
                request.IncludeSettings,
                ct).ConfigureAwait(false);

            _logger.LogInformation("Backup created successfully: {BackupId}, {Size} bytes, {Games} games",
                result.BackupId, result.TotalSize, result.GamesBackedUp);

            return Result<BackupId>.Success(result.BackupId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create backup of type {Type}", request.Type);
            return Result<BackupId>.Failure($"Backup creation failed: {ex.Message}");
        }
    }
}
