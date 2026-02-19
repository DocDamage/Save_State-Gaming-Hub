using FluentAssertions;
using SaveState.Core.RomManagement.RomValidation;

namespace SaveState.Core.Tests.RomManagement.RomValidation;

public class DatFileEntryTests
{
    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        // Arrange & Act
        var entry = new DatFileEntry();

        // Assert
        entry.Name.Should().BeEmpty();
        entry.SourceDat.Should().BeEmpty();
        entry.Languages.Should().NotBeNull();
        entry.Languages.Should().BeEmpty();
        entry.IsVerified.Should().BeFalse();
        entry.DumpStatus.Should().Be(RomDumpStatus.Good);
    }

    [Fact]
    public void Properties_CanBeSet()
    {
        // Arrange & Act
        var entry = new DatFileEntry
        {
            Name = "Super Mario Bros",
            GameTitle = "Super Mario Bros (USA)",
            Region = "USA",
            Crc32 = "a1b2c3d4",
            Md5 = "d41d8cd98f00b204e9800998ecf8427e",
            Sha1 = "da39a3ee5e6b4b0d3255bfef95601890afd80709",
            Size = 40960,
            SourceDat = "nointro.xml",
            DatVersion = "2024-01-01",
            IsVerified = true,
            DumpStatus = RomDumpStatus.Verified,
            Notes = "Test notes"
        };

        // Assert
        entry.Name.Should().Be("Super Mario Bros");
        entry.GameTitle.Should().Be("Super Mario Bros (USA)");
        entry.Region.Should().Be("USA");
        entry.Crc32.Should().Be("a1b2c3d4");
        entry.Md5.Should().Be("d41d8cd98f00b204e9800998ecf8427e");
        entry.Sha1.Should().Be("da39a3ee5e6b4b0d3255bfef95601890afd80709");
        entry.Size.Should().Be(40960);
        entry.SourceDat.Should().Be("nointro.xml");
        entry.DatVersion.Should().Be("2024-01-01");
        entry.IsVerified.Should().BeTrue();
        entry.DumpStatus.Should().Be(RomDumpStatus.Verified);
        entry.Notes.Should().Be("Test notes");
    }

    [Theory]
    [InlineData(RomDumpStatus.Good, true)]
    [InlineData(RomDumpStatus.Verified, true)]
    [InlineData(RomDumpStatus.Bad, false)]
    [InlineData(RomDumpStatus.Corrupt, false)]
    [InlineData(RomDumpStatus.Overdump, false)]
    [InlineData(RomDumpStatus.Underdump, false)]
    [InlineData(RomDumpStatus.Unknown, false)]
    public void IsGoodDump_EvaluatesCorrectly(RomDumpStatus status, bool expected)
    {
        // Arrange
        var entry = new DatFileEntry { DumpStatus = status };

        // Act
        var isGood = entry.DumpStatus == RomDumpStatus.Good || entry.DumpStatus == RomDumpStatus.Verified;

        // Assert
        isGood.Should().Be(expected);
    }

    [Fact]
    public void Languages_CanAddMultiple()
    {
        // Arrange
        var entry = new DatFileEntry();

        // Act
        entry.Languages.Add("En");
        entry.Languages.Add("Fr");
        entry.Languages.Add("De");

        // Assert
        entry.Languages.Should().HaveCount(3);
        entry.Languages.Should().ContainInOrder("En", "Fr", "De");
    }

    [Fact]
    public void CloneOf_CanBeSet()
    {
        // Arrange & Act
        var entry = new DatFileEntry
        {
            Name = "Super Mario Bros [b]",
            CloneOf = "Super Mario Bros"
        };

        // Assert
        entry.CloneOf.Should().Be("Super Mario Bros");
    }
}
