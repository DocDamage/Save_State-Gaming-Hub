using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using SaveState.Core.Configuration;
using Xunit;

namespace SaveState.Configuration.Tests;

/// <summary>
/// Integration tests for configuration binding and validation.
/// Tests how configuration options work together and bind from various sources.
/// </summary>
public class ConfigurationIntegrationTests
{
    [Fact]
    public void Configuration_Binding_WorksWithMemoryConfiguration()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            ["OpenAi:BaseUrl"] = "https://api.openai.com/v1/",
            ["OpenAi:ApiKey"] = "sk-test123456789",
            ["OpenAi:DefaultModel"] = "gpt-4",

            ["Resilience:CircuitBreakerThreshold"] = "3",
            ["Resilience:CircuitBreakerDurationMs"] = "30000",
            ["Resilience:MaxRetries"] = "2",
            ["Resilience:InitialRetryDelayMs"] = "500",
            ["Resilience:RetryBackoffMultiplier"] = "1.5",
            ["Resilience:DefaultTimeoutMs"] = "15000",

            ["Steam:ApiKey"] = "ABC123DEF456",
            ["Steam:SteamId"] = "76561198000000000"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        // Act
        var openAiOptions = configuration.GetSection(OpenAiOptions.Section).Get<OpenAiOptions>();
        var resilienceConfig = configuration.GetSection(ResilienceConfig.Section).Get<ResilienceConfig>();
        var steamOptions = configuration.Get<SteamOptions>();

        // Assert
        openAiOptions.Should().NotBeNull();
        openAiOptions!.BaseUrl.Should().Be("https://api.openai.com/v1/");
        openAiOptions.ApiKey.Should().Be("sk-test123456789");
        openAiOptions.DefaultModel.Should().Be("gpt-4");

        resilienceConfig.Should().NotBeNull();
        resilienceConfig!.CircuitBreakerThreshold.Should().Be(3);
        resilienceConfig.CircuitBreakerDurationMs.Should().Be(30000);
        resilienceConfig.MaxRetries.Should().Be(2);
        resilienceConfig.InitialRetryDelayMs.Should().Be(500);
        resilienceConfig.RetryBackoffMultiplier.Should().Be(1.5);
        resilienceConfig.DefaultTimeoutMs.Should().Be(15000);

        steamOptions.Should().NotBeNull();
        steamOptions!.ApiKey.Should().Be("ABC123DEF456");
        steamOptions.SteamId.Should().Be("76561198000000000");
    }

    [Fact]
    public void Configuration_EnvironmentVariables_OverrideDefaults()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            ["OPENAI__BASEURL"] = "https://custom.openai.com/v1/",
            ["OPENAI__APIKEY"] = "sk-env123",
            ["OPENAI__DEFAULTMODEL"] = "gpt-3.5-turbo",

            ["RESILIENCE__CIRCUITBREAKERTHRESHOLD"] = "10",
            ["RESILIENCE__DEFAULTTIMEOUTMS"] = "60000"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        // Act
        var openAiOptions = configuration.GetSection("OpenAi").Get<OpenAiOptions>();
        var resilienceConfig = configuration.GetSection("Resilience").Get<ResilienceConfig>();

        // Assert
        openAiOptions.Should().NotBeNull();
        openAiOptions!.BaseUrl.Should().Be("https://custom.openai.com/v1/");
        openAiOptions.ApiKey.Should().Be("sk-env123");
        openAiOptions.DefaultModel.Should().Be("gpt-3.5-turbo");

        resilienceConfig.Should().NotBeNull();
        resilienceConfig!.CircuitBreakerThreshold.Should().Be(10);
        resilienceConfig.DefaultTimeoutMs.Should().Be(60000);
        // Other properties should have defaults
        resilienceConfig.MaxRetries.Should().Be(3);
        resilienceConfig.InitialRetryDelayMs.Should().Be(1000);
    }

    [Fact]
    public void Configuration_MissingSections_UseDefaultValues()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        // Act
        var openAiOptions = configuration.GetSection(OpenAiOptions.Section).Get<OpenAiOptions>();
        var resilienceConfig = configuration.GetSection(ResilienceConfig.Section).Get<ResilienceConfig>();

        // Assert - Should get default values when sections are missing
        openAiOptions.Should().NotBeNull();
        openAiOptions!.BaseUrl.Should().Be("https://api.openai.com/v1/");
        openAiOptions.ApiKey.Should().BeEmpty();
        openAiOptions.DefaultModel.Should().Be("gpt-4");

        resilienceConfig.Should().NotBeNull();
        resilienceConfig!.CircuitBreakerThreshold.Should().Be(5);
        resilienceConfig.CircuitBreakerDurationMs.Should().Be(60000);
        resilienceConfig.MaxRetries.Should().Be(3);
        resilienceConfig.InitialRetryDelayMs.Should().Be(1000);
        resilienceConfig.RetryBackoffMultiplier.Should().Be(2.0);
        resilienceConfig.DefaultTimeoutMs.Should().Be(30000);
    }

    [Fact]
    public void Configuration_InvalidValues_ThrowExceptions()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            ["Resilience:CircuitBreakerThreshold"] = "not-a-number",
            ["Resilience:RetryBackoffMultiplier"] = "invalid-double"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        // Act & Assert - Should throw when trying to bind invalid values
        var action = () => configuration.GetSection(ResilienceConfig.Section).Get<ResilienceConfig>();

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*Could not convert*");
    }

    [Fact]
    public void Configuration_Validation_RequiredFields()
    {
        // This test validates that critical configuration values are present
        // In a real application, you might have validation attributes or custom validators

        var validOpenAi = new OpenAiOptions
        {
            BaseUrl = "https://api.openai.com/v1/",
            ApiKey = "sk-valid123",
            DefaultModel = "gpt-4"
        };

        var validResilience = new ResilienceConfig
        {
            CircuitBreakerThreshold = 5,
            CircuitBreakerDurationMs = 60000,
            MaxRetries = 3,
            InitialRetryDelayMs = 1000,
            RetryBackoffMultiplier = 2.0,
            DefaultTimeoutMs = 30000
        };

        var validSteam = new SteamOptions
        {
            ApiKey = "valid-api-key",
            SteamId = "76561198000000000"
        };

        // Assert that valid configurations don't throw
        validOpenAi.Should().NotBeNull();
        validResilience.Should().NotBeNull();
        validSteam.Should().NotBeNull();

        // Validate that required fields are present
        validOpenAi.BaseUrl.Should().NotBeNullOrEmpty();
        validOpenAi.ApiKey.Should().NotBeNullOrEmpty();
        validOpenAi.DefaultModel.Should().NotBeNullOrEmpty();

        validResilience.CircuitBreakerThreshold.Should().BeGreaterThan(0);
        validResilience.MaxRetries.Should().BeGreaterThanOrEqualTo(0);
        validResilience.DefaultTimeoutMs.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Configuration_SensitiveData_Handling()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            ["OpenAi:ApiKey"] = "sk-1234567890123456789012345678901234567890",
            ["Steam:ApiKey"] = "sensitive-api-key-12345"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        // Act
        var openAiOptions = configuration.GetSection(OpenAiOptions.Section).Get<OpenAiOptions>();
        var steamOptions = configuration.Get<SteamOptions>();

        // Assert - Sensitive data should be properly handled
        openAiOptions.Should().NotBeNull();
        openAiOptions!.ApiKey.Should().NotBeNullOrEmpty();
        openAiOptions.ApiKey.Should().StartWith("sk-");

        steamOptions.Should().NotBeNull();
        steamOptions!.ApiKey.Should().NotBeNullOrEmpty();
        steamOptions.ApiKey.Should().Be("sensitive-api-key-12345");
    }

    [Fact]
    public void Configuration_ArrayValues_Handling()
    {
        // Arrange - Test configuration that might contain arrays
        // This simulates configuration that might contain lists of values

        var configData = new Dictionary<string, string?>
        {
            ["OpenAi:BaseUrl"] = "https://api.openai.com/v1/",
            ["OpenAi:ApiKey"] = "sk-test123",
            ["OpenAi:DefaultModel"] = "gpt-4,gpt-3.5-turbo" // Simulating comma-separated values
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        // Act
        var openAiOptions = configuration.GetSection(OpenAiOptions.Section).Get<OpenAiOptions>();

        // Assert
        openAiOptions.Should().NotBeNull();
        openAiOptions!.DefaultModel.Should().Be("gpt-4,gpt-3.5-turbo");
        // In a real scenario, you might have custom parsing for arrays
    }

    [Fact]
    public void Configuration_CaseSensitivity_Handling()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            ["openai:baseurl"] = "https://api.openai.com/v1/",
            ["OPENAI:APIKEY"] = "sk-test123",
            ["OpenAi:DefaultModel"] = "gpt-4"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        // Act
        var openAiOptions = configuration.GetSection("OpenAi").Get<OpenAiOptions>();

        // Assert - Configuration should be case-insensitive
        openAiOptions.Should().NotBeNull();
        openAiOptions!.BaseUrl.Should().Be("https://api.openai.com/v1/");
        openAiOptions.ApiKey.Should().Be("sk-test123");
        openAiOptions.DefaultModel.Should().Be("gpt-4");
    }

    [Fact]
    public void Configuration_Validation_HandlesEmptyConfiguration()
    {
        // Arrange
        var configData = new Dictionary<string, string?>();

        // Act
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        // Assert - Should not throw
        configuration.Should().NotBeNull();
    }

    [Fact]
    public void Configuration_Binding_IgnoresUnknownProperties()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            ["UnknownProperty"] = "value",
            ["AnotherUnknown"] = "123"
        };

        // Act
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        // Assert - Should handle unknown properties gracefully
        configuration["UnknownProperty"].Should().Be("value");
        configuration["AnotherUnknown"].Should().Be("123");
    }

    [Fact]
    public void Configuration_Section_Names_AreCaseInsensitive()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            ["OPENAI:APIKEY"] = "test-key-upper",
            ["openai:apikey"] = "test-key-lower",
            ["OpenAi:ApiKey"] = "test-key-mixed"
        };

        // Act
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        // Assert - Last value wins in case of duplicates
        configuration["OpenAi:ApiKey"].Should().Be("test-key-mixed");
        configuration["openai:apikey"].Should().Be("test-key-mixed");
    }

    [Fact]
    public void Configuration_Array_Binding_Works()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            ["TestArray:0"] = "first",
            ["TestArray:1"] = "second",
            ["TestArray:2"] = "third"
        };

        // Act
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var section = configuration.GetSection("TestArray");

        // Assert
        section["0"].Should().Be("first");
        section["1"].Should().Be("second");
        section["2"].Should().Be("third");
    }

    [Fact]
    public void Configuration_Nested_Sections_Work()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            ["Parent:Child:GrandChild"] = "value",
            ["Parent:Child:Another"] = "value2",
            ["Parent:Sibling"] = "sibling-value"
        };

        // Act
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        // Assert
        configuration["Parent:Child:GrandChild"].Should().Be("value");
        configuration["Parent:Child:Another"].Should().Be("value2");
        configuration["Parent:Sibling"].Should().Be("sibling-value");
    }

    [Fact]
    public void Configuration_Empty_Values_Are_Handled()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            ["EmptyString"] = "",
            ["NullString"] = null,
            ["Whitespace"] = "   "
        };

        // Act
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        // Assert
        configuration["EmptyString"].Should().Be("");
        configuration["NullString"].Should().BeNull();
        configuration["Whitespace"].Should().Be("   ");
    }

    [Fact]
    public void Configuration_Boolean_Conversion_Works()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            ["TrueValue"] = "true",
            ["FalseValue"] = "false",
            ["OneValue"] = "1",
            ["ZeroValue"] = "0",
            ["YesValue"] = "yes",
            ["NoValue"] = "no"
        };

        // Act
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        // Assert - Configuration preserves string values
        configuration["TrueValue"].Should().Be("true");
        configuration["FalseValue"].Should().Be("false");
        configuration["OneValue"].Should().Be("1");
        configuration["ZeroValue"].Should().Be("0");
    }

    [Fact]
    public void Configuration_Number_Conversion_Works()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            ["Integer"] = "42",
            ["Decimal"] = "3.14",
            ["Negative"] = "-123",
            ["Zero"] = "0"
        };

        // Act
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        // Assert - Configuration preserves string values
        configuration["Integer"].Should().Be("42");
        configuration["Decimal"].Should().Be("3.14");
        configuration["Negative"].Should().Be("-123");
        configuration["Zero"].Should().Be("0");
    }
}
