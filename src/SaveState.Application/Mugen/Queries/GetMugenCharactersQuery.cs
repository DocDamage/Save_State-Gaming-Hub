namespace SaveState.Application.Mugen.Queries;

using MediatR;
using SaveState.Application.Mugen.DTOs;

/// <summary>
/// Query to retrieve MUGEN characters from the library.
/// </summary>
public record GetMugenCharactersQuery(
    string? AuthorFilter = null,
    string? NameFilter = null,
    bool IncludeInvalid = false
) : IRequest<IReadOnlyList<MugenCharacterSummaryDto>>;
