namespace SaveState.Application.GameLibrary.EventHandlers;

using MediatR;
using Microsoft.Extensions.Logging;
using SaveState.Core.GameLibrary.Events;

public class GameImportedEventHandler : INotificationHandler<GameImportedEvent>
{
    private readonly ILogger<GameImportedEventHandler> _logger;

    public GameImportedEventHandler(ILogger<GameImportedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(GameImportedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Game imported: {GameId} from {Source} ({SourceId}) at {ImportedAt}",
            notification.GameId,
            notification.Source,
            notification.SourceId ?? "N/A",
            notification.ImportedAt);

        // In a real application, this might:
        // - Update search indexes
        // - Send notifications
        // - Trigger background processing
        // - Update statistics

        return Task.CompletedTask;
    }
}
