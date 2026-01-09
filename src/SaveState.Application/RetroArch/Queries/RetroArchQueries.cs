using MediatR;
using SaveState.Core.Common;
using SaveState.Core.RetroArch;

namespace SaveState.Application.RetroArch.Queries;

/// <summary>
/// Query to get all RetroArch games.
/// </summary>
public record GetRetroArchGamesQuery : IRequest<Result<IReadOnlyList<RetroArchGame>>>;

/// <summary>
/// Query to get installed RetroArch cores.
/// </summary>
public record GetInstalledCoresQuery : IRequest<Result<IReadOnlyList<RetroArchCore>>>;

/// <summary>
/// Query to get available RetroArch cores for download.
/// </summary>
public record GetAvailableCoresQuery : IRequest<Result<IReadOnlyList<RetroArchCore>>>;

/// <summary>
/// Query to get RetroArch configuration.
/// </summary>
public record GetRetroArchConfigQuery : IRequest<Result<RetroArchConfig>>;
