using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SaveState.Core.Input;
using SaveState.Core.Input.Entities;
using SaveState.Core.Input.Services;
using SaveState.Infrastructure.Input;

namespace SaveState.Infrastructure.Tests.Input;

/// <summary>
/// Unit tests for ControllerProfileService.
/// </summary>
public class ControllerProfileServiceTests
{
    private readonly Mock<IControllerProfileRepository> _mockRepository;
    private readonly ControllerProfileService _service;

    public ControllerProfileServiceTests()
    {
        _mockRepository = new Mock<IControllerProfileRepository>();
        _service = new ControllerProfileService(_mockRepository.Object, NullLogger<ControllerProfileService>.Instance);
    }

    [Fact]
    public async Task CreateProfileAsync_ShouldCreateProfile_WithDefaultMappings()
    {
        // Arrange
        var profileName = "Test Xbox Profile";
        var controllerType = ControllerType.Xbox;

        _mockRepository.Setup(r => r.AddAsync(It.IsAny<ControllerProfile>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateProfileAsync(profileName, controllerType);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be(profileName);
        result.Value.Type.Should().Be(controllerType);
        result.Value.GetMappings().Should().NotBeEmpty();
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<ControllerProfile>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateProfileAsync_ShouldCreateGameSpecificProfile()
    {
        // Arrange
        var profileName = "Game Specific Profile";
        var controllerType = ControllerType.PlayStation;
        var gameId = Guid.NewGuid();

        _mockRepository.Setup(r => r.AddAsync(It.IsAny<ControllerProfile>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateProfileAsync(profileName, controllerType, gameId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.GameId.Should().Be(gameId);
    }

    [Fact]
    public async Task GetProfileAsync_ShouldReturnProfile_WhenExists()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var expectedProfile = ControllerProfile.Create("Test", ControllerType.Xbox);

        _mockRepository.Setup(r => r.GetByIdAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedProfile);

        // Act
        var result = await _service.GetProfileAsync(profileId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedProfile);
    }

    [Fact]
    public async Task GetProfileAsync_ShouldReturnFailure_WhenNotExists()
    {
        // Arrange
        var profileId = Guid.NewGuid();

        _mockRepository.Setup(r => r.GetByIdAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ControllerProfile?)null);

        // Act
        var result = await _service.GetProfileAsync(profileId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task UpdateMappingsAsync_ShouldUpdateMappings()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var profile = ControllerProfile.Create("Test", ControllerType.Xbox);
        var newMappings = new Dictionary<string, string>
        {
            ["A"] = "Jump",
            ["B"] = "Run"
        };

        _mockRepository.Setup(r => r.GetByIdAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<ControllerProfile>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateMappingsAsync(profileId, newMappings);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<ControllerProfile>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetAsDefaultAsync_ShouldSetProfileAsDefault()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var profileId = Guid.NewGuid();
        var profile = ControllerProfile.Create("Test", ControllerType.Xbox, gameId);
        var existingDefault = ControllerProfile.Create("Old Default", ControllerType.Xbox, gameId);
        existingDefault.SetAsDefault();

        _mockRepository.Setup(r => r.GetByIdAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        _mockRepository.Setup(r => r.GetByGameIdAsync(gameId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ControllerProfile> { existingDefault });
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<ControllerProfile>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.SetAsDefaultAsync(profileId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<ControllerProfile>(), It.IsAny<CancellationToken>()), Times.AtLeast(2));
    }

    [Fact]
    public async Task SetAsDefaultAsync_ShouldFail_ForGlobalProfile()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var profile = ControllerProfile.Create("Global Profile", ControllerType.Xbox); // No gameId

        _mockRepository.Setup(r => r.GetByIdAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        // Act
        var result = await _service.SetAsDefaultAsync(profileId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("game-specific");
    }

    [Fact]
    public async Task DeleteProfileAsync_ShouldDeleteProfile()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var profile = ControllerProfile.Create("Test", ControllerType.Xbox);

        _mockRepository.Setup(r => r.GetByIdAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        _mockRepository.Setup(r => r.DeleteAsync(profileId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteProfileAsync(profileId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockRepository.Verify(r => r.DeleteAsync(profileId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetProfilesByTypeAsync_ShouldReturnProfiles()
    {
        // Arrange
        var profiles = new List<ControllerProfile>
        {
            ControllerProfile.Create("Xbox 1", ControllerType.Xbox),
            ControllerProfile.Create("Xbox 2", ControllerType.Xbox)
        };

        _mockRepository.Setup(r => r.GetByTypeAsync(ControllerType.Xbox, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profiles);

        // Act
        var result = await _service.GetProfilesByTypeAsync(ControllerType.Xbox);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }
}
