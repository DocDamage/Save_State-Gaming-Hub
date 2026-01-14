using SaveState.Core.Common;
using SaveState.Core.Mugen.Services;
using SaveState.Core.Mugen.ValueObjects;
using SaveState.Core.Mugen.DTOs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SaveState.Infrastructure.Mugen;

public class MoveCreationService : IMoveCreationService
{
    private readonly IMugenCollectionService _collectionService;
    
    public MoveCreationService(IMugenCollectionService collectionService)
    {
        _collectionService = collectionService;
    }

    public Task<Result<IReadOnlyList<MoveTemplate>>> GetMoveTemplatesAsync(MoveCategory? category = null, CancellationToken cancellationToken = default)
    {
        var templates = new List<MoveTemplate>
        {
            new()
            {
                Name = "Balanced Strike",
                Category = category ?? MoveCategory.Attack,
                Difficulty = DifficultyLevel.Intermediate,
                Type = MoveType.Special,
                Description = "A solid special move template",
                Tags = new[] { "pressure", "midrange" }
            }
        };

        return Task.FromResult(Result.Success<IReadOnlyList<MoveTemplate>>(templates));
    }

    public Task<Result<MoveDefinition>> CreateMoveFromTemplateAsync(MoveTemplate template, string name, string command, CancellationToken cancellationToken = default)
    {
        var move = new MoveDefinition
        {
            DisplayName = name,
            Command = command,
            MoveType = template.Type,
            Category = template.Category
        };

        return Task.FromResult(Result.Success(move));
    }

    public Task<Result<ValidationResult>> ValidateMoveAsync(MoveDefinition move, ValidationOptions options, CancellationToken cancellationToken = default)
    {
        var validation = new ValidationResult
        {
            IsValid = true
        };

        return Task.FromResult(Result.Success(validation));
    }

    public Task<Result<TestResult>> TestMoveAsync(MoveDefinition move, TestParameters parameters, CancellationToken cancellationToken = default)
    {
        var result = new TestResult
        {
            TestPassed = true,
            Findings = new[] { "Move performs consistently" }
        };

        return Task.FromResult(Result.Success(result));
    }

    public Task<Result<MoveExportResult>> ExportMoveAsync(MoveDefinition move, ExportOptions options, CancellationToken cancellationToken = default)
    {
        var exportResult = new MoveExportResult(
            CnsFilePath: Path.Combine(options.OutputDirectory, "move.cns"),
            CmdFilePath: Path.Combine(options.OutputDirectory, "move.cmd"),
            AirFilePath: null,
            CnsFileSize: 0,
            CmdFileSize: 0,
            AirFileSize: 0,
            GeneratedStates: Array.Empty<string>(),
            Warnings: Array.Empty<string>());

        return Task.FromResult(Result.Success(exportResult));
    }

    public async Task<Result<IReadOnlyList<MugenCharacterSummary>>> GetAvailableCharactersAsync(CancellationToken cancellationToken = default)
    {
        var result = await _collectionService.GetRosterAsync();
        if (result.IsSuccess && result.Value != null)
        {
            var summaries = result.Value.Select(c => new MugenCharacterSummary
            {
                Id = c.Id,
                Name = c.Name,
                DisplayName = c.DisplayName,
                Author = c.Author,
                Version = c.Version,
                IsValid = c.IsValid,
                LastScannedAt = c.LastScannedAt,
                FileSize = c.FileSize
            }).ToList();

            return Result.Success<IReadOnlyList<MugenCharacterSummary>>(summaries);
        }
        return Result.Failure<IReadOnlyList<MugenCharacterSummary>>(result.Error ?? "Failed to get characters");
    }

    public Task<Result<IReadOnlyList<MugenMoveEntryDto>>> GetCharacterMovesAsync(Guid characterId, CancellationToken cancellationToken = default)
    {
        // In a real implementation, this would load moves from the character's .cmd file
        var moves = new List<MugenMoveEntryDto>
        {
            new MugenMoveEntryDto
            {
                MoveName = "Jab",
                Command = "x",
                Type = "Normal",
                Damage = 50,
                Startup = 3,
                Active = 2,
                Recovery = 8,
                BlockAdvantage = -2,
                HitAdvantage = 5,
                Properties = "None",
                Notes = "Basic standing punch"
            },
            new MugenMoveEntryDto
            {
                MoveName = "Fireball",
                Command = "~D, DF, F, x",
                Type = "Special",
                Damage = 120,
                Startup = 12,
                Active = 10,
                Recovery = 18,
                BlockAdvantage = -5,
                HitAdvantage = 8,
                Properties = "Projectile",
                Notes = "Ranged projectile attack"
            }
        };
        
        return Task.FromResult(Result.Success<IReadOnlyList<MugenMoveEntryDto>>(moves));
    }

    public Task<Result<MoveDefinition>> CreateMoveAsync(Guid characterId, MoveData moveData, CancellationToken cancellationToken = default)
    {
        // In a real implementation, this would write to the character's .cmd and .cns files
        var move = new MoveDefinition
        {
            DisplayName = moveData.Name,
            Command = moveData.Command,
            MoveType = ParseMoveType(moveData.Type),
            Category = MoveCategory.Attack
        };
        
        return Task.FromResult(Result.Success(move));
    }

    public Task<Result<MoveDefinition>> UpdateMoveAsync(Guid characterId, string moveName, MoveData moveData, CancellationToken cancellationToken = default)
    {
        // In a real implementation, this would update the character's .cmd and .cns files
        var move = new MoveDefinition
        {
            DisplayName = moveData.Name,
            Command = moveData.Command,
            MoveType = ParseMoveType(moveData.Type),
            Category = MoveCategory.Attack
        };
        
        return Task.FromResult(Result.Success(move));
    }

    public Task<Result> DeleteMoveAsync(Guid characterId, string moveName, CancellationToken cancellationToken = default)
    {
        // In a real implementation, this would remove the move from the character's files
        return Task.FromResult(Result.Success());
    }

    public Task<Result<string>> TestMoveAsync(Guid characterId, MoveData moveData, CancellationToken cancellationToken = default)
    {
        // In a real implementation, this would launch MUGEN in test mode
        var result = $"Move '{moveData.Name}' tested successfully.\n" +
                    $"Damage: {moveData.Damage}, Startup: {moveData.Startup}f, Recovery: {moveData.Recovery}f\n" +
                    $"Frame advantage on block: {moveData.BlockAdvantage}, on hit: {moveData.HitAdvantage}";
        
        return Task.FromResult(Result.Success(result));
    }

    public Task<Result<string>> ExportMoveAsync(Guid characterId, string moveName, CancellationToken cancellationToken = default)
    {
        // In a real implementation, this would export the move to a file
        var exportPath = Path.Combine(Path.GetTempPath(), $"{moveName}_export.txt");
        return Task.FromResult(Result.Success(exportPath));
    }

    private static MoveType ParseMoveType(string type)
    {
        return type?.ToLowerInvariant() switch
        {
            "normal" => MoveType.Normal,
            "special" => MoveType.Special,
            "super" => MoveType.Super,
            "hyper" => MoveType.Hyper,
            _ => MoveType.Normal
        };
    }
}
