using FluentAssertions;
using SaveState.Core.RomManagement.RomValidation;

namespace SaveState.Core.Tests.RomManagement.RomValidation;

public class RomValidationReportTests
{
    [Fact]
    public void IsValid_WithVerifiedStatus_ReturnsTrue()
    {
        // Arrange
        var report = new RomValidationReport
        {
            Status = ValidationStatus.Verified
        };

        // Act & Assert
        report.IsValid.Should().BeTrue();
    }

    [Fact]
    public void IsValid_WithValidStatus_ReturnsTrue()
    {
        // Arrange
        var report = new RomValidationReport
        {
            Status = ValidationStatus.Valid
        };

        // Act & Assert
        report.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(ValidationStatus.Invalid)]
    [InlineData(ValidationStatus.Corrupted)]
    [InlineData(ValidationStatus.BadDump)]
    [InlineData(ValidationStatus.Unknown)]
    [InlineData(ValidationStatus.Pending)]
    [InlineData(ValidationStatus.Validating)]
    public void IsValid_WithNonValidStatuses_ReturnsFalse(ValidationStatus status)
    {
        // Arrange
        var report = new RomValidationReport
        {
            Status = status
        };

        // Act & Assert
        report.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        // Arrange & Act
        var report = new RomValidationReport();

        // Assert
        report.Issues.Should().NotBeNull();
        report.Issues.Should().BeEmpty();
        report.Recommendations.Should().NotBeNull();
        report.Recommendations.Should().BeEmpty();
    }

    [Fact]
    public void ValidationIssue_Constructor_SetsProperties()
    {
        // Arrange & Act
        var issue = new ValidationIssue
        {
            Severity = IssueSeverity.Error,
            Category = IssueCategory.Hash,
            Message = "Test message",
            SuggestedFix = "Test fix"
        };

        // Assert
        issue.Severity.Should().Be(IssueSeverity.Error);
        issue.Category.Should().Be(IssueCategory.Hash);
        issue.Message.Should().Be("Test message");
        issue.SuggestedFix.Should().Be("Test fix");
    }

    [Fact]
    public void RomMatchResult_IsGoodDump_WithVerifiedEntry_ReturnsTrue()
    {
        // Arrange
        var result = new RomMatchResult
        {
            MatchedEntry = new DatFileEntry { DumpStatus = RomDumpStatus.Verified }
        };

        // Act & Assert
        result.IsGoodDump.Should().BeTrue();
    }

    [Fact]
    public void RomMatchResult_IsGoodDump_WithGoodEntry_ReturnsTrue()
    {
        // Arrange
        var result = new RomMatchResult
        {
            MatchedEntry = new DatFileEntry { DumpStatus = RomDumpStatus.Good }
        };

        // Act & Assert
        result.IsGoodDump.Should().BeTrue();
    }

    [Theory]
    [InlineData(RomDumpStatus.Bad)]
    [InlineData(RomDumpStatus.Corrupt)]
    [InlineData(RomDumpStatus.Overdump)]
    [InlineData(RomDumpStatus.Underdump)]
    [InlineData(RomDumpStatus.Unknown)]
    public void RomMatchResult_IsGoodDump_WithBadDumps_ReturnsFalse(RomDumpStatus status)
    {
        // Arrange
        var result = new RomMatchResult
        {
            MatchedEntry = new DatFileEntry { DumpStatus = status }
        };

        // Act & Assert
        result.IsGoodDump.Should().BeFalse();
    }

    [Fact]
    public void RomMatchResult_IsGoodDump_WithNullEntry_ReturnsFalse()
    {
        // Arrange
        var result = new RomMatchResult
        {
            MatchedEntry = null
        };

        // Act & Assert
        result.IsGoodDump.Should().BeFalse();
    }

    [Fact]
    public void DuplicateRomInfo_Count_ReturnsDuplicatesCount()
    {
        // Arrange
        var info = new DuplicateRomInfo
        {
            Duplicates = new List<RomDuplicateEntry>
            {
                new(),
                new(),
                new()
            }
        };

        // Act & Assert
        info.Count.Should().Be(3);
    }

    [Fact]
    public void DuplicateRomInfo_AreInDifferentLocations_WithSameDirectory_ReturnsFalse()
    {
        // Arrange
        var info = new DuplicateRomInfo
        {
            Duplicates = new List<RomDuplicateEntry>
            {
                new() { Directory = @"C:\Roms" },
                new() { Directory = @"C:\Roms" }
            }
        };

        // Act & Assert
        info.AreInDifferentLocations.Should().BeFalse();
    }

    [Fact]
    public void DuplicateRomInfo_AreInDifferentLocations_WithDifferentDirectories_ReturnsTrue()
    {
        // Arrange
        var info = new DuplicateRomInfo
        {
            Duplicates = new List<RomDuplicateEntry>
            {
                new() { Directory = @"C:\Roms" },
                new() { Directory = @"D:\Games" }
            }
        };

        // Act & Assert
        info.AreInDifferentLocations.Should().BeTrue();
    }

    [Fact]
    public void DuplicateRomInfo_WastedSpace_CalculatesCorrectly()
    {
        // Arrange
        var info = new DuplicateRomInfo
        {
            Duplicates = new List<RomDuplicateEntry>
            {
                new() { FileSize = 1000 },
                new() { FileSize = 1000 },
                new() { FileSize = 500 }
            }
        };

        // Act & Assert
        info.WastedSpace.Should().Be(1500); // 1000 + 1000 + 500 - 1000 (max)
    }

    [Fact]
    public void DuplicateRomInfo_WastedSpace_WithSingleDuplicate_ReturnsZero()
    {
        // Arrange
        var info = new DuplicateRomInfo
        {
            Duplicates = new List<RomDuplicateEntry>
            {
                new() { FileSize = 1000 }
            }
        };

        // Act & Assert
        info.WastedSpace.Should().Be(0);
    }

    [Fact]
    public void MissingGameReport_CompletionPercentage_CalculatesCorrectly()
    {
        // Arrange
        var report = new MissingGameReport
        {
            TotalGames = 100,
            MissingGames = new List<MissingGameEntry> { new(), new(), new() },
            OwnedGames = new List<string> { "a", "b", "c", "d", "e", "f", "g" }
        };

        // Act & Assert
        report.CompletionPercentage.Should().Be(97m); // (100-3)/100 * 100
    }

    [Fact]
    public void RomValidationJob_ProgressPercentage_CalculatesCorrectly()
    {
        // Arrange
        var job = new RomValidationJob
        {
            TotalRoms = 100,
            ProcessedRoms = 50
        };

        // Act & Assert
        job.ProgressPercentage.Should().Be(50);
    }

    [Fact]
    public void RomValidationJob_ProgressPercentage_WithZeroTotal_ReturnsZero()
    {
        // Arrange
        var job = new RomValidationJob
        {
            TotalRoms = 0,
            ProcessedRoms = 0
        };

        // Act & Assert
        job.ProgressPercentage.Should().Be(0);
    }

    [Fact]
    public void PlatformValidationStats_CompletionPercentage_CalculatesCorrectly()
    {
        // Arrange
        var stats = new PlatformValidationStats
        {
            TotalRoms = 100,
            ValidatedRoms = 75
        };

        // Act & Assert
        stats.CompletionPercentage.Should().Be(75m);
    }

    [Fact]
    public void RomValidationStatistics_ValidationPercentage_CalculatesCorrectly()
    {
        // Arrange
        var stats = new RomValidationStatistics
        {
            TotalRoms = 200,
            ValidatedRoms = 50
        };

        // Act & Assert
        stats.ValidationPercentage.Should().Be(25m);
    }
}
