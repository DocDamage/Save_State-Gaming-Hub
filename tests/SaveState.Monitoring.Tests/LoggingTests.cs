using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using System.Collections.Concurrent;

namespace SaveState.Monitoring.Tests;

/// <summary>
/// Tests for logging functionality.
/// Validates log levels, message formatting, and structured logging.
/// </summary>
public class LoggingTests
{
    [Fact]
    public void Logger_LogLevels_WorkCorrectly()
    {
        // Arrange
        var logger = new Mock<ILogger<TestClass>>();
        var loggedMessages = new ConcurrentBag<(LogLevel Level, string Message)>();

        logger.Setup(x => x.Log(It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Callback<LogLevel, EventId, object, Exception, Delegate>((level, _, message, _, _) =>
            {
                loggedMessages.Add((level, message?.ToString() ?? ""));
            });

        // Act
        var testClass = new TestClass(logger.Object);
        testClass.LogAllLevels();

        // Assert - All log levels should work
        loggedMessages.Should().HaveCount(6);

        var levels = loggedMessages.Select(x => x.Level).ToList();
        levels.Should().Contain(LogLevel.Trace);
        levels.Should().Contain(LogLevel.Debug);
        levels.Should().Contain(LogLevel.Information);
        levels.Should().Contain(LogLevel.Warning);
        levels.Should().Contain(LogLevel.Error);
        levels.Should().Contain(LogLevel.Critical);
    }

    [Fact]
    public void Logger_IsEnabled_ChecksWork()
    {
        // Arrange
        var logger = new Mock<ILogger<TestClass>>();
        logger.Setup(x => x.IsEnabled(LogLevel.Information)).Returns(true);
        logger.Setup(x => x.IsEnabled(LogLevel.Debug)).Returns(false);

        // Act & Assert
        logger.Object.IsEnabled(LogLevel.Information).Should().BeTrue();
        logger.Object.IsEnabled(LogLevel.Debug).Should().BeFalse();
    }

    [Fact]
    public void Logger_StructuredLogging_Works()
    {
        // Arrange
        var logger = new Mock<ILogger<TestClass>>();
        var loggedData = new ConcurrentBag<object>();

        logger.Setup(x => x.Log(It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Callback<LogLevel, EventId, object, Exception, Delegate>((_, _, state, _, _) =>
            {
                loggedData.Add(state);
            });

        // Act
        var testClass = new TestClass(logger.Object);
        testClass.LogStructuredData();

        // Assert - Structured data should be logged
        loggedData.Should().NotBeEmpty();
        loggedData.First().Should().NotBeNull();
    }

    [Fact]
    public void Logger_ExceptionLogging_IncludesExceptionDetails()
    {
        // Arrange
        var logger = new Mock<ILogger<TestClass>>();
        var loggedExceptions = new ConcurrentBag<Exception>();

        logger.Setup(x => x.Log(It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Callback<LogLevel, EventId, object, Exception, Delegate>((_, _, _, ex, _) =>
            {
                if (ex != null) loggedExceptions.Add(ex);
            });

        // Act
        var testClass = new TestClass(logger.Object);
        testClass.LogException();

        // Assert - Exception should be included in log
        loggedExceptions.Should().HaveCount(1);
        loggedExceptions.First().Should().BeOfType<InvalidOperationException>();
        loggedExceptions.First().Message.Should().Be("Test exception");
    }

    [Fact]
    public void Logger_Scope_WorksCorrectly()
    {
        // Arrange
        var logger = new Mock<ILogger<TestClass>>();
        var scopes = new ConcurrentBag<object>();

        logger.Setup(x => x.BeginScope(It.IsAny<It.IsAnyType>()))
            .Callback<object>(scope => scopes.Add(scope))
            .Returns(Mock.Of<IDisposable>());

        // Act
        var testClass = new TestClass(logger.Object);
        testClass.UseScope();

        // Assert - Scope should be created
        scopes.Should().HaveCount(1);
        scopes.First().Should().NotBeNull();
    }

    [Fact]
    public void Logger_EventId_IsUsedCorrectly()
    {
        // Arrange
        var logger = new Mock<ILogger<TestClass>>();
        var eventIds = new ConcurrentBag<EventId>();

        logger.Setup(x => x.Log(It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Callback<LogLevel, EventId, object, Exception, Delegate>((_, eventId, _, _, _) =>
            {
                eventIds.Add(eventId);
            });

        // Act
        var testClass = new TestClass(logger.Object);
        testClass.LogWithEventId();

        // Assert - Event ID should be used
        eventIds.Should().HaveCount(1);
        eventIds.First().Id.Should().Be(1001);
        eventIds.First().Name.Should().Be("TestEvent");
    }

    [Fact]
    public void Logger_LogLevel_Filtering_Works()
    {
        // Arrange
        var logger = new Mock<ILogger<TestClass>>();
        logger.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns<LogLevel>(level => level >= LogLevel.Warning);

        // Act
        var enabledLevels = Enum.GetValues<LogLevel>()
            .Where(level => logger.Object.IsEnabled(level))
            .ToList();

        // Assert - Only Warning and above should be enabled
        enabledLevels.Should().Contain(LogLevel.Warning);
        enabledLevels.Should().Contain(LogLevel.Error);
        enabledLevels.Should().Contain(LogLevel.Critical);
        enabledLevels.Should().NotContain(LogLevel.Trace);
        enabledLevels.Should().NotContain(LogLevel.Debug);
        enabledLevels.Should().NotContain(LogLevel.Information);
    }

    [Fact]
    public void Logger_MessageTemplate_Formatting_Works()
    {
        // Arrange
        var logger = new Mock<ILogger<TestClass>>();
        var formattedMessages = new ConcurrentBag<string>();

        logger.Setup(x => x.Log(It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Callback<LogLevel, EventId, object, Exception, Func<object, Exception?, string>>((_, _, _, _, formatter) =>
            {
                if (formatter != null)
                {
                    var message = formatter("User {UserId} performed {Action}", null);
                    formattedMessages.Add(message);
                }
            });

        // Act
        var testClass = new TestClass(logger.Object);
        testClass.LogWithTemplate();

        // Assert - Message should be formatted
        formattedMessages.Should().HaveCount(1);
        formattedMessages.First().Should().Contain("User");
        formattedMessages.First().Should().Contain("performed");
    }

    private class TestClass
    {
        private readonly ILogger<TestClass> _logger;

        public TestClass(ILogger<TestClass> logger)
        {
            _logger = logger;
        }

        public void LogAllLevels()
        {
            _logger.LogTrace("Trace message");
            _logger.LogDebug("Debug message");
            _logger.LogInformation("Information message");
            _logger.LogWarning("Warning message");
            _logger.LogError("Error message");
            _logger.LogCritical("Critical message");
        }

        public void LogStructuredData()
        {
            _logger.LogInformation("User {UserId} logged in", 12345);
        }

        public void LogException()
        {
            _logger.LogError(new InvalidOperationException("Test exception"), "An error occurred");
        }

        public void UseScope()
        {
            using (_logger.BeginScope("TestScope"))
            {
                _logger.LogInformation("Message in scope");
            }
        }

        public void LogWithEventId()
        {
            _logger.LogInformation(new EventId(1001, "TestEvent"), "Test message");
        }

        public void LogWithTemplate()
        {
            _logger.LogInformation("User {UserId} performed {Action}", 12345, "Login");
        }
    }
}
