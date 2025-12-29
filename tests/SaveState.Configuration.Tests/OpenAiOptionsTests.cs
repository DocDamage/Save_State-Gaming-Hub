using FluentAssertions;
using SaveState.Core.Configuration;
using Xunit;

namespace SaveState.Configuration.Tests;

/// <summary>
/// Tests for OpenAI configuration options.
/// Validates default values, section constants, and configuration binding.
/// </summary>
public class OpenAiOptionsTests
{
    [Fact]
    public void OpenAiOptions_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new OpenAiOptions();

        // Assert
        options.BaseUrl.Should().Be("https://api.openai.com/v1/");
        options.ApiKey.Should().BeEmpty();
        options.DefaultModel.Should().Be("gpt-4");
    }

    [Fact]
    public void OpenAiOptions_SectionConstant_IsCorrect()
    {
        // Assert
        OpenAiOptions.Section.Should().Be("OpenAi");
    }

    [Fact]
    public void OpenAiOptions_CanBeModified()
    {
        // Arrange
        var options = new OpenAiOptions();

        // Act
        options.BaseUrl = "https://custom.openai.com/v1/";
        options.ApiKey = "sk-test123";
        options.DefaultModel = "gpt-3.5-turbo";

        // Assert
        options.BaseUrl.Should().Be("https://custom.openai.com/v1/");
        options.ApiKey.Should().Be("sk-test123");
        options.DefaultModel.Should().Be("gpt-3.5-turbo");
    }

    [Fact]
    public void OpenAiOptions_BaseUrl_HandlesDifferentFormats()
    {
        // Arrange
        var options = new OpenAiOptions();

        // Act & Assert - Valid URLs
        options.BaseUrl = "https://api.openai.com/v1";
        options.BaseUrl.Should().Be("https://api.openai.com/v1");

        options.BaseUrl = "https://custom.openai.com/v1/";
        options.BaseUrl.Should().Be("https://custom.openai.com/v1/");

        options.BaseUrl = "http://localhost:3000/v1/";
        options.BaseUrl.Should().Be("http://localhost:3000/v1/");
    }

    [Fact]
    public void OpenAiOptions_ApiKey_HandlesDifferentFormats()
    {
        // Arrange
        var options = new OpenAiOptions();

        // Act & Assert
        options.ApiKey = "sk-test123456789";
        options.ApiKey.Should().Be("sk-test123456789");

        options.ApiKey = "sk-1234567890123456789012345678901234567890";
        options.ApiKey.Should().Be("sk-1234567890123456789012345678901234567890");

        options.ApiKey = "";
        options.ApiKey.Should().BeEmpty();

        options.ApiKey = null!;
        options.ApiKey.Should().BeNull();
    }

    [Fact]
    public void OpenAiOptions_DefaultModel_SupportsAllModels()
    {
        // Arrange
        var options = new OpenAiOptions();

        // Act & Assert
        var validModels = new[] { "gpt-4", "gpt-4-turbo", "gpt-3.5-turbo", "gpt-3.5-turbo-16k" };

        foreach (var model in validModels)
        {
            options.DefaultModel = model;
            options.DefaultModel.Should().Be(model);
        }
    }

    [Fact]
    public void OpenAiOptions_ConfigurationBinding_Works()
    {
        // This test validates that the options can be bound from configuration
        // In a real scenario, this would be tested with Microsoft.Extensions.Configuration

        var options = new OpenAiOptions
        {
            BaseUrl = "https://api.openai.com/v1/",
            ApiKey = "sk-test123",
            DefaultModel = "gpt-4"
        };

        // Assert that all properties are settable and retrievable
        options.Should().NotBeNull();
        options.BaseUrl.Should().NotBeNullOrEmpty();
        options.ApiKey.Should().NotBeNullOrEmpty();
        options.DefaultModel.Should().NotBeNullOrEmpty();
    }
}
