using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Sync.Services.DTOs;

namespace SaveState.Application.Sync.Queries;

/// <summary>
/// Query to get all available cloud gaming providers.
/// </summary>
public record GetCloudProvidersQuery : IRequest<Result<IReadOnlyList<CloudGamingProvider>>>;