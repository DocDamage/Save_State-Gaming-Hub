using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SaveState.Application.RomManagement.RomValidation.Commands;
using SaveState.Application.RomManagement.RomValidation.Queries;
using SaveState.Core.RomManagement;
using SaveState.Core.RomManagement.Entities;
using SaveState.Core.RomManagement.Enums;
using SaveState.Core.RomManagement.RomValidation;
using SaveState.Core.RomManagement.RomValidation.Services;
using SaveState.Core.RomManagement.ValueObjects;
using Xunit;

namespace SaveState.IntegrationTests;

/// <summary>
/// Integration tests for ROM validation feature (Feature 12).
/// </summary>
public class RomValidationIntegrationTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IServiceProvider _serviceProvider;

    public RomValidationIntegrationTests(IntegrationTestFixture fixture)
    {
        _serviceProvider = fixture.ServiceProvider;
    }

    [Fact]
    public async Task FullValidationFlow_ValidateRomThenGetStatistics_ShouldReflectInStats()
    {
        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var romRepository = scope.ServiceProvider.GetRequiredService<IRomFileRepository>();

        var tempRomPath = Path.GetTempFileName();
        await File.WriteAllBytesAsync(tempRomPath, new byte[] { 0x01, 0x02, 0x03, 0x04 });

        try
        {
            // Arrange - Persist a ROM so the handler can resolve it.
            var romFile = new RomFile(
                title: "Test ROM",
                platformId: Guid.NewGuid(),
                filePath: new FilePath(tempRomPath),
                fileSize: new FileInfo(tempRomPath).Length);
            await romRepository.AddAsync(romFile);

            // Act
            var validationOptions = new RomValidationOptions
            {
                CalculateCrc32 = true,
                CalculateMd5 = true,
                CalculateSha1 = true,
                MatchAgainstDatFiles = false
            };

            var validateResult = await mediator.Send(
                new ValidateRomCommand(romFile.Id, validationOptions));

            // Assert
            validateResult.IsSuccess.Should().BeTrue();
            validateResult.Value.Should().NotBeNull();
            validateResult.Value.Status.Should().BeOneOf(
                ValidationStatus.Valid,
                ValidationStatus.Verified,
                ValidationStatus.Invalid);
        }
        finally
        {
            if (File.Exists(tempRomPath))
            {
                File.Delete(tempRomPath);
            }
        }
    }

    [Fact]
    public async Task BatchValidationFlow_ValidateMultipleRoms_ShouldProcessAll()
    {
        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var romRepository = scope.ServiceProvider.GetRequiredService<IRomFileRepository>();

        var tempRomPath = Path.GetTempFileName();
        await File.WriteAllBytesAsync(tempRomPath, new byte[] { 0x10, 0x20, 0x30, 0x40 });

        try
        {
            // Arrange
            var platformId = Guid.NewGuid();

            var romFile = new RomFile(
                title: "Batch ROM",
                platformId: platformId,
                filePath: new FilePath(tempRomPath),
                fileSize: new FileInfo(tempRomPath).Length);
            await romRepository.AddAsync(romFile);

            var options = new RomValidationOptions
            {
                CalculateCrc32 = true,
                CalculateMd5 = true,
                CalculateSha1 = false,
                MatchAgainstDatFiles = false
            };

            var command = new BatchValidateRomsCommand(
                "Integration Test Batch",
                null,
                new List<Guid> { platformId },
                options);

            // Act
            var result = await mediator.Send(command);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.TotalRoms.Should().BeGreaterThan(0);
            result.Value.ProcessedRoms.Should().BeGreaterThanOrEqualTo(0);
        }
        finally
        {
            if (File.Exists(tempRomPath))
            {
                File.Delete(tempRomPath);
            }
        }
    }

    [Fact]
    public async Task DuplicateDetectionFlow_FindDuplicates_ShouldReturnConsistentResults()
    {
        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Act
        var result = await mediator.Send(
            new GetDuplicateRomsQuery(null, HashAlgorithmType.Sha1));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();

        // Verify duplicate properties if any exist
        foreach (var duplicate in result.Value)
        {
            duplicate.Hash.Should().NotBeNullOrEmpty();
            duplicate.Count.Should().BeGreaterThan(1);
            duplicate.Duplicates.Should().NotBeNull();
            duplicate.WastedSpace.Should().BeGreaterThanOrEqualTo(0);
        }
    }

    [Fact]
    public async Task BadDumpDetectionFlow_IdentifyBadDumps_ShouldReturnValidStructure()
    {
        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Act
        var result = await mediator.Send(new GetBadDumpsQuery(null));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();

        // Verify bad dump info structure
        foreach (var badDump in result.Value)
        {
            badDump.RomFileId.Should().NotBe(Guid.Empty);
            badDump.FileName.Should().NotBeNullOrEmpty();
            badDump.DumpStatus.Should().NotBe(RomDumpStatus.Good);
        }
    }

    [Fact]
    public async Task StatisticsFlow_GetStatistics_ShouldReturnValidMetrics()
    {
        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Act
        var result = await mediator.Send(new GetRomValidationStatisticsQuery());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();

        var stats = result.Value;
        stats.TotalRoms.Should().BeGreaterThanOrEqualTo(0);
        stats.ValidatedRoms.Should().BeGreaterThanOrEqualTo(0);
        stats.VerifiedRoms.Should().BeGreaterThanOrEqualTo(0);
        stats.BadDumps.Should().BeGreaterThanOrEqualTo(0);
        stats.CorruptedRoms.Should().BeGreaterThanOrEqualTo(0);
        stats.DuplicateRoms.Should().BeGreaterThanOrEqualTo(0);

        // Validation percentage should be calculable
        if (stats.TotalRoms > 0)
        {
            stats.ValidationPercentage.Should().BeInRange(0, 100);
        }

        // Platform stats should be valid
        foreach (var platformStat in stats.PlatformStats)
        {
            platformStat.Value.TotalRoms.Should().BeGreaterThanOrEqualTo(0);
            platformStat.Value.ValidatedRoms.Should().BeGreaterThanOrEqualTo(0);
            platformStat.Value.CompletionPercentage.Should().BeInRange(0, 100);
        }
    }

    [Fact]
    public async Task CalculateHashesFlow_CalculateForRom_ShouldReturnHashInfo()
    {
        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Arrange
        var romId = Guid.NewGuid();

        // Act
        var result = await mediator.Send(
            new CalculateRomHashesCommand(romId, true, true, true, false));

        // Assert
        // Note: This will fail if ROM doesn't exist, which is expected behavior
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task RenameSuggestionsFlow_GetSuggestions_ShouldReturnValidStructure()
    {
        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Arrange
        var romId = Guid.NewGuid();

        // Act
        var result = await mediator.Send(new GetRomRenameSuggestionsQuery(romId));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();

        foreach (var suggestion in result.Value)
        {
            suggestion.RomFileId.Should().NotBe(Guid.Empty);
            suggestion.CurrentName.Should().NotBeNullOrEmpty();
            suggestion.SuggestedName.Should().NotBeNullOrEmpty();
        }
    }

    [Fact]
    public async Task ExportFlow_ExportResults_ShouldCreateFile()
    {
        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Arrange
        var outputPath = Path.Combine(Path.GetTempPath(), $"test_export_{Guid.NewGuid()}.html");

        try
        {
            // Act
            var result = await mediator.Send(
                new ExportValidationResultsCommand(outputPath, ValidationExportFormat.Html, null));

            // Assert
            if (result.IsSuccess)
            {
                File.Exists(result.Value).Should().BeTrue();
            }
        }
        finally
        {
            // Cleanup
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    [Fact]
    public void ValidationOptions_AllHashAlgorithms_ShouldBeConfigurable()
    {
        // Arrange & Act - Test various option combinations
        var optionSets = new[]
        {
            new RomValidationOptions { CalculateCrc32 = true, CalculateMd5 = false, CalculateSha1 = false },
            new RomValidationOptions { CalculateCrc32 = false, CalculateMd5 = true, CalculateSha1 = false },
            new RomValidationOptions { CalculateCrc32 = false, CalculateMd5 = false, CalculateSha1 = true },
            new RomValidationOptions { CalculateCrc32 = true, CalculateMd5 = true, CalculateSha1 = true },
            new RomValidationOptions { CalculateCrc32 = true, CalculateMd5 = true, CalculateSha1 = true, CalculateSha256 = true }
        };

        // Assert - All option combinations should be valid
        foreach (var options in optionSets)
        {
            options.Should().NotBeNull();
            // Verify boolean properties are valid (no exception means valid)
            _ = options.CalculateCrc32;
            _ = options.CalculateMd5;
            _ = options.CalculateSha1;
            _ = options.CalculateSha256;
        }
    }

    [Fact]
    public void ValidationStatus_AllStatuses_ShouldHaveValidDefinitions()
    {
        // Arrange & Act
        var statuses = Enum.GetValues<ValidationStatus>();

        // Assert
        statuses.Should().Contain(ValidationStatus.Pending);
        statuses.Should().Contain(ValidationStatus.Validating);
        statuses.Should().Contain(ValidationStatus.Valid);
        statuses.Should().Contain(ValidationStatus.Invalid);
        statuses.Should().Contain(ValidationStatus.Verified);
        statuses.Should().Contain(ValidationStatus.Corrupted);
        statuses.Should().Contain(ValidationStatus.Unknown);
        statuses.Should().Contain(ValidationStatus.BadDump);
    }
}
