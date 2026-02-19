using FluentAssertions;
using SaveState.Core.RomManagement.RomValidation;

namespace SaveState.Core.Tests.RomManagement.RomValidation;

public class RomValidationOptionsTests
{
    [Fact]
    public void DefaultConstructor_SetsDefaultValues()
    {
        // Arrange & Act
        var options = new RomValidationOptions();

        // Assert
        options.CalculateCrc32.Should().BeTrue();
        options.CalculateMd5.Should().BeTrue();
        options.CalculateSha1.Should().BeTrue();
        options.CalculateSha256.Should().BeFalse();
        options.MatchAgainstDatFiles.Should().BeTrue();
        options.SkipValidated.Should().BeFalse();
        options.VerifyFileIntegrity.Should().BeTrue();
        options.DatFilePaths.Should().NotBeNull();
        options.DatFilePaths.Should().BeEmpty();
    }

    [Fact]
    public void Properties_CanBeModified()
    {
        // Arrange
        var options = new RomValidationOptions();

        // Act
        options.CalculateCrc32 = false;
        options.CalculateMd5 = false;
        options.CalculateSha1 = false;
        options.CalculateSha256 = true;
        options.MatchAgainstDatFiles = false;
        options.SkipValidated = true;
        options.VerifyFileIntegrity = false;
        options.DatFilePaths = new List<string> { "test.dat" };

        // Assert
        options.CalculateCrc32.Should().BeFalse();
        options.CalculateMd5.Should().BeFalse();
        options.CalculateSha1.Should().BeFalse();
        options.CalculateSha256.Should().BeTrue();
        options.MatchAgainstDatFiles.Should().BeFalse();
        options.SkipValidated.Should().BeTrue();
        options.VerifyFileIntegrity.Should().BeFalse();
        options.DatFilePaths.Should().ContainSingle().Which.Should().Be("test.dat");
    }

    [Fact]
    public void DatFilePaths_CanAddMultiplePaths()
    {
        // Arrange
        var options = new RomValidationOptions();

        // Act
        options.DatFilePaths.Add("nointro.xml");
        options.DatFilePaths.Add("redump.xml");
        options.DatFilePaths.Add("custom.dat");

        // Assert
        options.DatFilePaths.Should().HaveCount(3);
        options.DatFilePaths.Should().ContainInOrder("nointro.xml", "redump.xml", "custom.dat");
    }

    [Fact]
    public void CalculateOnlySha1_MinimalHashCalculation()
    {
        // Arrange & Act
        var options = new RomValidationOptions
        {
            CalculateCrc32 = false,
            CalculateMd5 = false,
            CalculateSha1 = true,
            CalculateSha256 = false
        };

        // Assert
        options.CalculateSha1.Should().BeTrue();
        options.CalculateCrc32.Should().BeFalse();
        options.CalculateMd5.Should().BeFalse();
        options.CalculateSha256.Should().BeFalse();
    }

    [Fact]
    public void MaximumValidation_EnableAllOptions()
    {
        // Arrange & Act
        var options = new RomValidationOptions
        {
            CalculateCrc32 = true,
            CalculateMd5 = true,
            CalculateSha1 = true,
            CalculateSha256 = true,
            MatchAgainstDatFiles = true,
            VerifyFileIntegrity = true,
            SkipValidated = false
        };

        // Assert - all validation options enabled
        options.CalculateCrc32.Should().BeTrue();
        options.CalculateMd5.Should().BeTrue();
        options.CalculateSha1.Should().BeTrue();
        options.CalculateSha256.Should().BeTrue();
        options.MatchAgainstDatFiles.Should().BeTrue();
        options.VerifyFileIntegrity.Should().BeTrue();
        options.SkipValidated.Should().BeFalse();
    }

    [Fact]
    public void QuickValidation_MinimalOptions()
    {
        // Arrange & Act - for quick validation, only calculate SHA1 and skip already validated
        var options = new RomValidationOptions
        {
            CalculateCrc32 = false,
            CalculateMd5 = false,
            CalculateSha1 = true,
            CalculateSha256 = false,
            MatchAgainstDatFiles = false,
            VerifyFileIntegrity = false,
            SkipValidated = true
        };

        // Assert
        options.CalculateSha1.Should().BeTrue();
        options.CalculateCrc32.Should().BeFalse();
        options.CalculateMd5.Should().BeFalse();
        options.CalculateSha256.Should().BeFalse();
        options.MatchAgainstDatFiles.Should().BeFalse();
        options.VerifyFileIntegrity.Should().BeFalse();
        options.SkipValidated.Should().BeTrue();
    }
}
