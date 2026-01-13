using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SaveState.Core.Input.Entities;
using SaveState.Infrastructure.Input;

namespace SaveState.Infrastructure.Tests.Input;

/// <summary>
/// Unit tests for InputService.
/// </summary>
public class InputServiceTests
{
    private readonly InputService _service;

    public InputServiceTests()
    {
        _service = new InputService(NullLogger<InputService>.Instance);
    }

    [Fact]
    public async Task ApplyControllerMappingsAsync_ShouldSucceed_WithValidMappings()
    {
        // Arrange
        var mappings = new Dictionary<string, string>
        {
            ["A"] = "Jump",
            ["B"] = "Run",
            ["X"] = "Attack"
        };

        // Act
        var result = await _service.ApplyControllerMappingsAsync(mappings);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ApplyControllerMappingsAsync_ShouldFail_WithNullMappings()
    {
        // Act
        var result = await _service.ApplyControllerMappingsAsync(null!);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ApplyControllerMappingsAsync_ShouldFail_WithEmptyMappings()
    {
        // Arrange
        var mappings = new Dictionary<string, string>();

        // Act
        var result = await _service.ApplyControllerMappingsAsync(mappings);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("No mappings provided");
    }

    [Fact]
    public async Task GetCurrentMappingsAsync_ShouldReturnEmpty_Initially()
    {
        // Act
        var result = await _service.GetCurrentMappingsAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCurrentMappingsAsync_ShouldReturnAppliedMappings()
    {
        // Arrange
        var mappings = new Dictionary<string, string>
        {
            ["A"] = "Jump",
            ["B"] = "Run"
        };
        await _service.ApplyControllerMappingsAsync(mappings);

        // Act
        var result = await _service.GetCurrentMappingsAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value!["A"].Should().Be("Jump");
        result.Value["B"].Should().Be("Run");
    }

    [Fact]
    public async Task ClearMappingsAsync_ShouldClearAllMappings()
    {
        // Arrange
        var mappings = new Dictionary<string, string>
        {
            ["A"] = "Jump",
            ["B"] = "Run"
        };
        await _service.ApplyControllerMappingsAsync(mappings);

        // Act
        var clearResult = await _service.ClearMappingsAsync();
        var getMappingsResult = await _service.GetCurrentMappingsAsync();

        // Assert
        clearResult.IsSuccess.Should().BeTrue();
        getMappingsResult.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task DetectDevicesAsync_ShouldReturnDevices()
    {
        // Act
        var result = await _service.DetectDevicesAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().NotBeEmpty();
        result.Value!.First().Type.Should().Be(ControllerType.Keyboard);
    }

    [Fact]
    public async Task ApplyControllerMappingsAsync_ShouldOverwrite_ExistingMappings()
    {
        // Arrange
        var firstMappings = new Dictionary<string, string>
        {
            ["A"] = "Jump"
        };
        var secondMappings = new Dictionary<string, string>
        {
            ["B"] = "Run"
        };

        await _service.ApplyControllerMappingsAsync(firstMappings);

        // Act
        await _service.ApplyControllerMappingsAsync(secondMappings);
        var result = await _service.GetCurrentMappingsAsync();

        // Assert
        result.Value.Should().HaveCount(1);
        result.Value.Should().ContainKey("B");
        result.Value.Should().NotContainKey("A");
    }
}
