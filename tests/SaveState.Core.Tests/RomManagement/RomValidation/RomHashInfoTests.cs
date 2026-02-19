using FluentAssertions;
using SaveState.Core.RomManagement.RomValidation;

namespace SaveState.Core.Tests.RomManagement.RomValidation;

public class RomHashInfoTests
{
    [Fact]
    public void Create_WithAllHashes_SetsAllProperties()
    {
        // Arrange
        var romFileId = Guid.NewGuid();
        const string crc32 = "a1b2c3d4";
        const string md5 = "d41d8cd98f00b204e9800998ecf8427e";
        const string sha1 = "da39a3ee5e6b4b0d3255bfef95601890afd80709";
        const string sha256 = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";

        // Act
        var hashInfo = RomHashInfo.Create(romFileId, crc32, md5, sha1, sha256);

        // Assert
        hashInfo.RomFileId.Should().Be(romFileId);
        hashInfo.Crc32.Should().Be(crc32.ToLowerInvariant());
        hashInfo.Md5.Should().Be(md5);
        hashInfo.Sha1.Should().Be(sha1);
        hashInfo.Sha256.Should().Be(sha256);
        hashInfo.IsComplete.Should().BeTrue();
        hashInfo.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Create_WithPartialHashes_SetsIsCompleteTrue()
    {
        // Arrange
        var romFileId = Guid.NewGuid();
        const string sha1 = "da39a3ee5e6b4b0d3255bfef95601890afd80709";

        // Act
        var hashInfo = RomHashInfo.Create(romFileId, sha1: sha1);

        // Assert
        hashInfo.Sha1.Should().Be(sha1);
        hashInfo.Crc32.Should().BeNull();
        hashInfo.Md5.Should().BeNull();
        hashInfo.IsComplete.Should().BeTrue();
    }

    [Fact]
    public void Create_WithNoHashes_SetsIsCompleteFalse()
    {
        // Arrange
        var romFileId = Guid.NewGuid();

        // Act
        var hashInfo = RomHashInfo.Create(romFileId);

        // Assert
        hashInfo.IsComplete.Should().BeFalse();
    }

    [Fact]
    public void GetPrimaryHash_WithSha1_ReturnsSha1()
    {
        // Arrange
        var hashInfo = RomHashInfo.Create(
            Guid.NewGuid(),
            crc32: "a1b2c3d4",
            md5: "d41d8cd98f00b204e9800998ecf8427e",
            sha1: "da39a3ee5e6b4b0d3255bfef95601890afd80709");

        // Act
        var primary = hashInfo.GetPrimaryHash();

        // Assert
        primary.Should().Be("da39a3ee5e6b4b0d3255bfef95601890afd80709");
    }

    [Fact]
    public void GetPrimaryHash_WithNoSha1_ReturnsMd5()
    {
        // Arrange
        var hashInfo = RomHashInfo.Create(
            Guid.NewGuid(),
            crc32: "a1b2c3d4",
            md5: "d41d8cd98f00b204e9800998ecf8427e");

        // Act
        var primary = hashInfo.GetPrimaryHash();

        // Assert
        primary.Should().Be("d41d8cd98f00b204e9800998ecf8427e");
    }

    [Fact]
    public void GetPrimaryHash_WithOnlyCrc32_ReturnsCrc32()
    {
        // Arrange
        var hashInfo = RomHashInfo.Create(
            Guid.NewGuid(),
            crc32: "a1b2c3d4");

        // Act
        var primary = hashInfo.GetPrimaryHash();

        // Assert
        primary.Should().Be("a1b2c3d4");
    }

    [Fact]
    public void Matches_WithExactSha1Match_ReturnsTrue()
    {
        // Arrange
        const string sha1 = "da39a3ee5e6b4b0d3255bfef95601890afd80709";
        var hashInfo = RomHashInfo.Create(Guid.NewGuid(), sha1: sha1);
        var entry = new DatFileEntry { Sha1 = sha1 };

        // Act
        var matches = hashInfo.Matches(entry);

        // Assert
        matches.Should().BeTrue();
    }

    [Fact]
    public void Matches_WithCaseInsensitiveMatch_ReturnsTrue()
    {
        // Arrange
        var hashInfo = RomHashInfo.Create(Guid.NewGuid(), sha1: "da39a3ee5e6b4b0d3255bfef95601890afd80709");
        var entry = new DatFileEntry { Sha1 = "DA39A3EE5E6B4B0D3255BFEF95601890AFD80709" };

        // Act
        var matches = hashInfo.Matches(entry);

        // Assert
        matches.Should().BeTrue();
    }

    [Fact]
    public void Matches_WithMd5Match_ReturnsTrue()
    {
        // Arrange
        const string md5 = "d41d8cd98f00b204e9800998ecf8427e";
        var hashInfo = RomHashInfo.Create(Guid.NewGuid(), md5: md5);
        var entry = new DatFileEntry { Md5 = md5 };

        // Act
        var matches = hashInfo.Matches(entry);

        // Assert
        matches.Should().BeTrue();
    }

    [Fact]
    public void Matches_WithNoMatch_ReturnsFalse()
    {
        // Arrange
        var hashInfo = RomHashInfo.Create(Guid.NewGuid(), sha1: "da39a3ee5e6b4b0d3255bfef95601890afd80709");
        var entry = new DatFileEntry { Sha1 = "0000000000000000000000000000000000000000" };

        // Act
        var matches = hashInfo.Matches(entry);

        // Assert
        matches.Should().BeFalse();
    }

    [Fact]
    public void Matches_WithNullEntryHashes_ReturnsFalse()
    {
        // Arrange
        var hashInfo = RomHashInfo.Create(Guid.NewGuid(), sha1: "da39a3ee5e6b4b0d3255bfef95601890afd80709");
        var entry = new DatFileEntry();

        // Act
        var matches = hashInfo.Matches(entry);

        // Assert
        matches.Should().BeFalse();
    }

    [Fact]
    public void Create_SetsCalculatedAtToUtcNow()
    {
        // Arrange
        var before = DateTime.UtcNow.AddSeconds(-1);

        // Act
        var hashInfo = RomHashInfo.Create(Guid.NewGuid(), sha1: "test");

        // Arrange
        var after = DateTime.UtcNow.AddSeconds(1);

        // Assert
        hashInfo.CalculatedAt.Should().BeAfter(before);
        hashInfo.CalculatedAt.Should().BeBefore(after);
    }
}
