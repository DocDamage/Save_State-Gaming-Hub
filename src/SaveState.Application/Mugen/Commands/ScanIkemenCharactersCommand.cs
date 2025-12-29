namespace SaveState.Application.Mugen.Commands;

using MediatR;

/// <summary>
/// Command to scan all IKEMEN character directories (Street Fighter, MVC2, builtin).
/// </summary>
public record ScanIkemenCharactersCommand : IRequest<Unit>;
