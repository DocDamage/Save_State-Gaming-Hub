namespace SaveState.Application.Mugen.Queries.Handlers;

using MediatR;
using SaveState.Application.Mugen.DTOs;
using SaveState.Core.Mugen;

/// <summary>
/// Handles the GetMugenCharactersQuery by retrieving and filtering characters.
/// </summary>
public class GetMugenCharactersQueryHandler : IRequestHandler<GetMugenCharactersQuery, IReadOnlyList<MugenCharacterSummaryDto>>
{
    private readonly IMugenCharacterRepository _characterRepository;

    /// <summary>
    /// Initializes a new instance of the GetMugenCharactersQueryHandler.
    /// </summary>
    /// <param name="characterRepository">The character repository.</param>
    public GetMugenCharactersQueryHandler(IMugenCharacterRepository characterRepository)
    {
        _characterRepository = characterRepository;
    }

    /// <summary>
    /// Handles the query by retrieving and filtering MUGEN characters.
    /// </summary>
    /// <param name="request">The query request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of character summary DTOs.</returns>
    public async Task<IReadOnlyList<MugenCharacterSummaryDto>> Handle(GetMugenCharactersQuery request, CancellationToken cancellationToken)
    {
        var characters = await _characterRepository.GetAllAsync(cancellationToken);

        var filteredCharacters = characters
            .Where(c => request.IncludeInvalid || c.IsValid)
            .Where(c => string.IsNullOrEmpty(request.AuthorFilter) ||
                       c.Author.Contains(request.AuthorFilter, StringComparison.OrdinalIgnoreCase))
            .Where(c => string.IsNullOrEmpty(request.NameFilter) ||
                       c.Name.Contains(request.NameFilter, StringComparison.OrdinalIgnoreCase))
            .Select(c => new MugenCharacterSummaryDto(
                c.Id,
                c.Name,
                c.DisplayName,
                c.Author,
                c.Version,
                c.IsValid,
                c.LastScannedAt,
                c.FileSize
            ))
            .ToList();

        return filteredCharacters;
    }
}
