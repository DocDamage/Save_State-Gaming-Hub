using MediatR;
using SaveState.Application.Common;
using SaveState.Core.Analytics.DTOs;

namespace SaveState.Application.GameLibrary.Queries;

/// <summary>
/// Query to get gaming heatmap data for a specific year.
/// </summary>
public record GetGamingHeatmapQuery(int Year) : IRequest<Result<GamingHeatmapData>>;