using MediatR;
using SaveState.Core.Common;

namespace SaveState.Application.Performance.Commands;

/// <summary>
/// Command to compact the database to reduce file size and improve performance.
/// </summary>
public sealed record CompactDatabaseCommand : IRequest<Result>;
