using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Intelligence.AiContent.Services;
using SaveState.Infrastructure.Intelligence.AiContent;

namespace SaveState.Core.Tests.Intelligence.AiContent;

public class ThumbnailGeneratorServiceTests
{
    private readonly ThumbnailGeneratorService _service;
    private readonly Mock<ILogger<ThumbnailGeneratorService>> _loggerMock;
    private readonly Mock<ITimeProvider> _timeProviderMock;

    public ThumbnailGeneratorServiceTests()
    {
        _loggerMock = new Mock<ILogger<ThumbnailGeneratorService>>();
        _timeProviderMock = new Mock<ITimeProvider>();
        _timeProviderMock.SetupGet(tp => tp.UtcNow).Returns(new DateTime(2026, 2, 19, 18, 0, 0, DateTimeKind.Utc));

        var options = Options.Create(new AiContentGenerationOptions
        {
            DefaultProvider = "OpenAI",
            MaxConcurrentGenerations = 3,
            GenerationTimeout = TimeSpan.FromMinutes(5),
            EnableCaching = true
        });

        _service = new ThumbnailGeneratorService(options, _loggerMock.Object, _timeProviderMock.Object);
    }

    [Fact]
    public async Task GenerateThumbnailAsync_WithValidRequest_ReturnsThumbnail()
    {
        // Arrange
        var request = new ThumbnailGenerationRequest(
            GameId: Guid.NewGuid(),
            GameTitle: "Test Game",
            Description: "A test game",
            Genres: new List<string> { "RPG" },
            Tags: new List<string> { "Fantasy" },
            Quality: GenerationQuality.Standard);

        // Act
        var result = await _service.GenerateThumbnailAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.PromptUsed.Should().Contain("Test Game");
        result.Value.Url.Should().NotBeNullOrEmpty();
        result.Value.Metadata.Provider.Should().Be("OpenAI");
    }

    [Fact]
    public async Task GenerateThumbnailAsync_WithCustomPrompt_UsesCustomPrompt()
    {
        // Arrange
        var customPrompt = "Epic fantasy battle scene with dragons";
        var request = new ThumbnailGenerationRequest(
            GameId: Guid.NewGuid(),
            GameTitle: "Dragon Battle",
            Description: "Dragon game",
            Genres: null,
            Tags: null,
            CustomPrompt: customPrompt);

        // Act
        var result = await _service.GenerateThumbnailAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.PromptUsed.Should().Be(customPrompt);
    }

    [Fact]
    public async Task GenerateVariationsAsync_ReturnsMultipleThumbnails()
    {
        // Arrange
        var request = new ThumbnailGenerationRequest(
            GameId: Guid.NewGuid(),
            GameTitle: "Test Game",
            Description: "A test game",
            Genres: null,
            Tags: null);

        // Act
        var result = await _service.GenerateVariationsAsync(request, 3);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
    }

    [Fact]
    public async Task UpscaleAsync_WithValidImage_ReturnsUpscaledThumbnail()
    {
        // Arrange
        var imageUrl = "https://example.com/image.png";
        var targetResolution = ImageResolution.Uhd4K;

        // Act
        var result = await _service.UpscaleAsync(imageUrl, targetResolution);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Resolution.Width.Should().Be(3840);
        result.Value.Resolution.Height.Should().Be(2160);
    }

    [Fact]
    public async Task GetAvailableStylesAsync_ReturnsStyles()
    {
        // Act
        var result = await _service.GetAvailableStylesAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        result.Value.Should().Contain(s => s.Id == "realistic");
        result.Value.Should().Contain(s => s.Id == "pixel_art");
        result.Value.Should().Contain(s => s.Id == "anime");
    }

    [Fact]
    public async Task GetGenerationHistoryAsync_ReturnsPagedHistory()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Generate a thumbnail first to add to history
        var request = new ThumbnailGenerationRequest(
            GameId: Guid.NewGuid(),
            GameTitle: "History Test",
            Description: "Test",
            Genres: null,
            Tags: null);
        await _service.GenerateThumbnailAsync(request);

        // Act
        var result = await _service.GetGenerationHistoryAsync(userId, 1, 20);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().NotBeEmpty();
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task DeleteThumbnailAsync_RemovesFromHistory()
    {
        // Arrange
        var request = new ThumbnailGenerationRequest(
            GameId: Guid.NewGuid(),
            GameTitle: "Delete Test",
            Description: "Test",
            Genres: null,
            Tags: null);
        var generationResult = await _service.GenerateThumbnailAsync(request);
        var thumbnailId = generationResult.Value!.Id;

        // Act
        var deleteResult = await _service.DeleteThumbnailAsync(thumbnailId);

        // Assert
        deleteResult.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ImageResolution_StaticProperties_ReturnCorrectDimensions()
    {
        // Assert
        ImageResolution.Small.Width.Should().Be(256);
        ImageResolution.Small.Height.Should().Be(256);
        ImageResolution.Medium.Width.Should().Be(512);
        ImageResolution.Large.Width.Should().Be(1024);
        ImageResolution.Uhd4K.Width.Should().Be(3840);
        ImageResolution.Uhd4K.Height.Should().Be(2160);
    }

    [Fact]
    public void ImageResolution_ForAspectRatio_ReturnsCorrectDimensions()
    {
        // Act
        var square = ImageResolution.ForAspectRatio(ThumbnailAspectRatio.Square, 1024);
        var portrait = ImageResolution.ForAspectRatio(ThumbnailAspectRatio.Portrait, 1024);
        var landscape = ImageResolution.ForAspectRatio(ThumbnailAspectRatio.Landscape, 1024);
        var wide = ImageResolution.ForAspectRatio(ThumbnailAspectRatio.Wide, 1024);

        // Assert
        square.Width.Should().Be(square.Height);
        portrait.Width.Should().BeLessThan(portrait.Height);
        landscape.Width.Should().BeGreaterThan(landscape.Height);
        wide.Width.Should().BeGreaterThan(wide.Height);
    }

    [Fact]
    public void GenerationQuality_Enum_HasExpectedValues()
    {
        // Assert
        Enum.GetValues<GenerationQuality>().Should().Contain(new[]
        {
            GenerationQuality.Draft,
            GenerationQuality.Standard,
            GenerationQuality.High,
            GenerationQuality.Premium
        });
    }

    [Fact]
    public void ThumbnailAspectRatio_Enum_HasExpectedValues()
    {
        // Assert
        Enum.GetValues<ThumbnailAspectRatio>().Should().Contain(new[]
        {
            ThumbnailAspectRatio.Square,
            ThumbnailAspectRatio.Portrait,
            ThumbnailAspectRatio.Landscape,
            ThumbnailAspectRatio.Wide,
            ThumbnailAspectRatio.Ultrawide
        });
    }

    [Fact]
    public void GeneratedThumbnail_Properties_WorkCorrectly()
    {
        // Arrange
        var id = Guid.NewGuid();
        var resolution = ImageResolution.Large;
        var style = new ArtStyle("pixel", "Pixel Art", "Retro style", null, null, 1.0f);
        var metadata = new GenerationMetadata("OpenAI", TimeSpan.FromSeconds(5), 12345, 0.04f, null);

        // Act
        var thumbnail = new GeneratedThumbnail(
            id, "url", "local", resolution, ThumbnailAspectRatio.Square,
            style, GenerationQuality.High, "prompt", DateTime.UtcNow, metadata);

        // Assert
        thumbnail.Id.Should().Be(id);
        thumbnail.Resolution.Should().Be(resolution);
        thumbnail.Style.Should().Be(style);
        thumbnail.Quality.Should().Be(GenerationQuality.High);
        thumbnail.Metadata.Provider.Should().Be("OpenAI");
    }
}
