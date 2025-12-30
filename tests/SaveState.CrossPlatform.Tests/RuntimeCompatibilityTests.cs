using FluentAssertions;
using Xunit;

namespace SaveState.CrossPlatform.Tests;

/// <summary>
/// Tests for runtime platform compatibility.
/// Ensures the application runs correctly on different operating systems.
/// </summary>
public class RuntimeCompatibilityTests
{
    [Fact]
    public void OperatingSystem_Detection_Works()
    {
        // Arrange & Act - Check OS detection
        var isWindows = OperatingSystem.IsWindows();
        var isLinux = OperatingSystem.IsLinux();
        var isMacOS = OperatingSystem.IsMacOS();

        // Assert - Exactly one should be true
        var trueCount = new[] { isWindows, isLinux, isMacOS }.Count(x => x);
        trueCount.Should().Be(1, "Exactly one operating system should be detected");

        // At least one should be true (we're running somewhere)
        (isWindows || isLinux || isMacOS).Should().BeTrue();
    }

    [Fact]
    public void RuntimeIdentifier_IsAvailable()
    {
        // Arrange & Act
        var rid = System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier;

        // Assert - Runtime identifier should be available
        rid.Should().NotBeNullOrEmpty();

        // Should contain platform information
        if (OperatingSystem.IsWindows())
        {
            rid.Should().Contain("win");
        }
        else if (OperatingSystem.IsLinux())
        {
            rid.Should().Contain("linux");
        }
        else if (OperatingSystem.IsMacOS())
        {
            rid.Should().Contain("osx");
        }
    }

    [Fact]
    public void ProcessArchitecture_IsDetected()
    {
        // Arrange & Act
        var architecture = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture;

        // Assert - Architecture should be detected
        // Architecture should be a valid value (not checking for Unknown since it may not exist in all .NET versions)

        // Should be a valid architecture
        var validArchitectures = new[]
        {
            System.Runtime.InteropServices.Architecture.X86,
            System.Runtime.InteropServices.Architecture.X64,
            System.Runtime.InteropServices.Architecture.Arm,
            System.Runtime.InteropServices.Architecture.Arm64
        };

        validArchitectures.Should().Contain(architecture);
    }

    [Fact]
    public void FrameworkDescription_IsAvailable()
    {
        // Arrange & Act
        var framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;

        // Assert - Framework should be identified
        framework.Should().NotBeNullOrEmpty();
        framework.Should().Contain(".NET"); // Should be a .NET runtime
    }

    [Fact]
    public void Environment_NewLine_IsPlatformAppropriate()
    {
        // Arrange & Act
        var newLine = Environment.NewLine;

        // Assert - New line should be platform-appropriate
        if (OperatingSystem.IsWindows())
        {
            newLine.Should().Be("\r\n");
        }
        else
        {
            // Unix-like systems (Linux, macOS)
            newLine.Should().Be("\n");
        }
    }

    [Fact]
    public void Path_PathSeparator_IsPlatformAppropriate()
    {
        // Arrange & Act
        var pathSeparator = Path.PathSeparator;

        // Assert - Path separator should be platform-appropriate
        if (OperatingSystem.IsWindows())
        {
            pathSeparator.Should().Be(';');
        }
        else
        {
            // Unix-like systems
            pathSeparator.Should().Be(':');
        }
    }

    [Fact]
    public void Path_DirectorySeparatorChar_IsPlatformAppropriate()
    {
        // Arrange & Act
        var directorySeparator = Path.DirectorySeparatorChar;

        // Assert - Directory separator should be platform-appropriate
        if (OperatingSystem.IsWindows())
        {
            directorySeparator.Should().Be('\\');
        }
        else
        {
            // Unix-like systems
            directorySeparator.Should().Be('/');
        }
    }

    [Fact]
    public void Environment_Variables_AreAccessible()
    {
        // Arrange & Act
        var pathVar = Environment.GetEnvironmentVariable("PATH");
        var tempVar = Environment.GetEnvironmentVariable("TMP") ??
                     Environment.GetEnvironmentVariable("TEMP") ??
                     Environment.GetEnvironmentVariable("TMPDIR");

        // Assert - Common environment variables should exist
        pathVar.Should().NotBeNullOrEmpty("PATH environment variable should exist");
        tempVar.Should().NotBeNullOrEmpty("Temporary directory environment variable should exist");
    }

    [Fact]
    public void FileSystem_CaseSensitivity_IsHandled()
    {
        // Arrange
        var testDir = Path.Combine(Path.GetTempPath(), "SaveStateTest");
        var upperCaseDir = Path.Combine(Path.GetTempPath(), "SAVESTATETEST");

        try
        {
            // Act - Create directory and test case sensitivity
            Directory.CreateDirectory(testDir);
            var existsLower = Directory.Exists(testDir);

            var existsUpper = Directory.Exists(upperCaseDir);

            // Assert - Case sensitivity depends on platform
            existsLower.Should().BeTrue("Directory we created should exist");

            if (OperatingSystem.IsWindows())
            {
                // Windows is case-insensitive
                existsUpper.Should().BeTrue("Windows should be case-insensitive");
            }
            else
            {
                // Unix-like systems are case-sensitive
                existsUpper.Should().BeFalse("Unix systems should be case-sensitive");
            }
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(testDir))
                Directory.Delete(testDir);
        }
    }

    [Fact]
    public void Memory_AndPerformance_IsAdequate()
    {
        // Arrange & Act
        var totalMemory = GC.GetTotalMemory(false);
        var processorCount = Environment.ProcessorCount;

        // Assert - Should have reasonable resources
        totalMemory.Should().BeGreaterThan(0, "Should have some memory allocated");
        processorCount.Should().BeGreaterThan(0, "Should have at least one processor");

        // Should be reasonable values
        totalMemory.Should().BeLessThan(10L * 1024 * 1024 * 1024, "Memory usage should be reasonable");
        processorCount.Should().BeLessThanOrEqualTo(128, "Processor count should be reasonable");
    }

    [Fact]
    public void TimeZone_Handling_Works()
    {
        // Arrange & Act
        var localTime = DateTime.Now;
        var utcTime = DateTime.UtcNow;
        var timeZone = TimeZoneInfo.Local;

        // Assert - Time zone information should be available
        localTime.Should().NotBe(default);
        utcTime.Should().NotBe(default);
        timeZone.Should().NotBeNull();

        // Local time should be different from UTC (unless in UTC timezone)
        var offset = timeZone.BaseUtcOffset;
        offset.Should().NotBe(default(TimeSpan));

        // Should be able to convert between time zones
        var convertedToUtc = TimeZoneInfo.ConvertTimeToUtc(localTime);
        var convertedToLocal = TimeZoneInfo.ConvertTimeFromUtc(utcTime, timeZone);

        convertedToUtc.Should().NotBe(default);
        convertedToLocal.Should().NotBe(default);
    }

    [Fact]
    public void CultureInfo_IsAvailable()
    {
        // Arrange & Act
        var currentCulture = System.Globalization.CultureInfo.CurrentCulture;
        var currentUICulture = System.Globalization.CultureInfo.CurrentUICulture;

        // Assert - Culture information should be available
        currentCulture.Should().NotBeNull();
        currentUICulture.Should().NotBeNull();

        // Culture names should be valid
        currentCulture.Name.Should().NotBeNullOrEmpty();
        currentUICulture.Name.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Encoding_Default_IsPlatformAppropriate()
    {
        // Arrange & Act
        var defaultEncoding = System.Text.Encoding.Default;

        // Assert - Default encoding should be available
        defaultEncoding.Should().NotBeNull();

        // Should be able to encode and decode text
        var testString = "Hello, 世界!";
        var bytes = defaultEncoding.GetBytes(testString);
        var decodedString = defaultEncoding.GetString(bytes);

        decodedString.Should().Be(testString);
    }

    [Fact]
    public void Threading_IsSupported()
    {
        // Arrange & Act
        var threadId = Environment.CurrentManagedThreadId;
        var isThreadPoolThread = Thread.CurrentThread.IsThreadPoolThread;

        // Assert - Threading should work
        threadId.Should().BeGreaterThan(0);
        // isThreadPoolThread may be true or false depending on execution context
    }

    [Fact]
    public void Random_Number_Generation_Works()
    {
        // Arrange & Act
        var random = new Random();
        var numbers = new List<int>();

        for (int i = 0; i < 100; i++)
        {
            numbers.Add(random.Next());
        }

        // Assert - Should generate different numbers
        var distinctCount = numbers.Distinct().Count();
        distinctCount.Should().BeGreaterThan(90, "Random numbers should be mostly unique");

        // All numbers should be non-negative
        numbers.Should().OnlyContain(x => x >= 0);
    }

    [Fact]
    public void Hashing_Algorithms_AreAvailable()
    {
        // Arrange
        var testData = "Test data for hashing";

        // Act
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(testData));

        // Assert
        hash.Should().NotBeNull();
        hash.Length.Should().Be(32, "SHA256 should produce 32 bytes");

        // Same input should produce same hash
        var hash2 = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(testData));
        hash.Should().Equal(hash2);
    }

    [Fact]
    public void Compression_IsSupported()
    {
        // Arrange
        var originalData = System.Text.Encoding.UTF8.GetBytes("This is test data for compression. " +
            "It needs to be long enough to actually compress effectively. " +
            "Lorem ipsum dolor sit amet, consectetur adipiscing elit.");

        // Act
        using var compressedStream = new MemoryStream();
        using (var gzipStream = new System.IO.Compression.GZipStream(compressedStream, System.IO.Compression.CompressionMode.Compress))
        {
            gzipStream.Write(originalData, 0, originalData.Length);
        }

        var compressedData = compressedStream.ToArray();

        // Decompress
        using var decompressedStream = new MemoryStream();
        using (var gzipStream = new System.IO.Compression.GZipStream(new MemoryStream(compressedData), System.IO.Compression.CompressionMode.Decompress))
        {
            gzipStream.CopyTo(decompressedStream);
        }

        var decompressedData = decompressedStream.ToArray();

        // Assert
        compressedData.Length.Should().BeLessThan(originalData.Length, "Compression should reduce size");
        decompressedData.Should().Equal(originalData, "Decompression should restore original data");
    }

    [Fact]
    public void Network_Connectivity_CanBeTested()
    {
        // Arrange & Act
        // Test basic network connectivity detection
        var canCreateTcpClient = true;
        try
        {
            using var client = new System.Net.Sockets.TcpClient();
        }
        catch
        {
            canCreateTcpClient = false;
        }

        // Assert - Should be able to create basic network objects
        canCreateTcpClient.Should().BeTrue("Should be able to create TCP client");
    }

    [Fact]
    public void DateTime_Operations_Work()
    {
        // Arrange
        var now = DateTime.Now;
        var utcNow = DateTime.UtcNow;
        var today = DateTime.Today;

        // Act
        var tomorrow = today.AddDays(1);
        var nextWeek = today.AddDays(7);
        var now2 = DateTime.UtcNow;
        var difference = now2 - utcNow;

        // Assert
        tomorrow.Should().BeAfter(today);
        nextWeek.Should().BeAfter(tomorrow);
        difference.Should().BeCloseTo(TimeSpan.Zero, TimeSpan.FromSeconds(5), "Sequential UtcNow calls should be very close");
    }

    [Fact]
    public void File_Permissions_CanBeChecked()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();

        try
        {
            // Act - Test basic file operations
            File.WriteAllText(tempFile, "test content");
            var canRead = File.Exists(tempFile);
            var canWrite = true;

            try
            {
                File.AppendAllText(tempFile, " more content");
            }
            catch
            {
                canWrite = false;
            }

            // Assert
            canRead.Should().BeTrue("Should be able to read created file");
            canWrite.Should().BeTrue("Should be able to write to created file");

            var content = File.ReadAllText(tempFile);
            content.Should().Be("test content more content");
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }
}
