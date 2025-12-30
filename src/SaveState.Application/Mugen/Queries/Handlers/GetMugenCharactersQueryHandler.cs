namespace SaveState.Application.Mugen.Queries.Handlers;

using MediatR;
using SaveState.Application.Mugen.DTOs;
using SaveState.Core.Mugen;
using SaveState.Core.Mugen.Entities;

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
        // Use paginated repository method for better performance
        // For now, use a reasonable page size; in the future, this could be made configurable
        var pageSize = 1000; // Large enough for most use cases while preventing memory issues
        var allCharacters = new List<MugenCharacter>();

        var pageNumber = 1;
        while (true)
        {
            var pagedResult = await _characterRepository.GetCharactersAsync(
                pageNumber: pageNumber,
                pageSize: pageSize,
                nameFilter: request.NameFilter,
                authorFilter: request.AuthorFilter,
                ct: cancellationToken);

            allCharacters.AddRange(pagedResult.Items);

            if (pagedResult.Items.Count < pageSize)
                break;

            pageNumber++;
        }

        var filteredCharacters = allCharacters
            .Where(c => request.IncludeInvalid || c.IsValid)
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
