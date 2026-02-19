using MediatR;
using SaveState.Application.Common;
using SaveState.Core.RomManagement.RomValidation.Services;

namespace SaveState.Application.RomManagement.RomValidation.Queries;

/// <summary>
/// Query to get rename suggestions for ROM files based on DAT matches.
/// </summary>
public sealed record GetRomRenameSuggestionsQuery(
    Guid? PlatformId = null) : IRequest<Result<List<RomRenameSuggestion>>>;
