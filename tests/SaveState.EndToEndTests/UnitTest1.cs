using FluentAssertions;
using Xunit;

namespace SaveState.EndToEndTests;

/// <summary>
/// Basic end-to-end test framework validation.
/// Tests that the testing infrastructure itself works correctly.
/// </summary>
public class TestFrameworkEndToEndTests
{
    [Fact]
    public void TestFramework_CanExecuteTests_Success()
    {
        // Arrange
        var expected = "E2E test working";
        var actual = "E2E test working";

        // Act & Assert
        actual.Should().Be(expected);
    }

    [Fact]
    public void EndToEndTestInfrastructure_IsConfiguredCorrectly()
    {
        // Assert: Basic test execution works
        true.Should().BeTrue();
    }

    [Fact]
    public void ProjectReferences_AreAvailable()
    {
        // Assert: Core and Infrastructure assemblies are accessible
        typeof(SaveState.Core.GameLibrary.Entities.Game).Should().NotBeNull();
        typeof(SaveState.Infrastructure.RomManagement.Services.PlatformExtensionRegistry).Should().NotBeNull();
    }
}
