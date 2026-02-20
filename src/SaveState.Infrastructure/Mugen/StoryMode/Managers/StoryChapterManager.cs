using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Mugen.Services;

namespace SaveState.Infrastructure.Mugen.StoryMode.Managers;

/// <summary>
/// Manages story chapters and organization.
/// </summary>
public class StoryChapterManager
{
    private readonly ILogger<StoryChapterManager> _logger;
    private readonly ConcurrentDictionary<Guid, StoryChapter> _chapters;

    public StoryChapterManager(ILogger<StoryChapterManager> logger)
    {
        _logger = logger;
        _chapters = new ConcurrentDictionary<Guid, StoryChapter>();
    }

    public ConcurrentDictionary<Guid, StoryChapter> Chapters => _chapters;

    /// <summary>
    /// Creates a new chapter.
    /// </summary>
    public Task<Result<StoryChapter>> CreateChapterAsync(
        string title,
        int? orderIndex = null,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Creating chapter: {Title}", title);

            var index = orderIndex ?? _chapters.Count;
            var chapter = new StoryChapter(
                Guid.NewGuid(),
                title,
                null,
                index,
                new List<StoryScene>(),
                null);

            _chapters[chapter.Id] = chapter;

            return Task.FromResult(Result<StoryChapter>.Success(chapter));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create chapter");
            return Task.FromResult(Result<StoryChapter>.Failure($"Create chapter failed: {ex.Message}", ErrorType.Internal));
        }
    }

    /// <summary>
    /// Gets all chapters ordered by OrderIndex.
    /// </summary>
    public Task<Result<IReadOnlyList<StoryChapter>>> GetChaptersAsync(
        CancellationToken ct = default)
    {
        var chapters = _chapters.Values.OrderBy(c => c.OrderIndex).ToList();
        return Task.FromResult(Result<IReadOnlyList<StoryChapter>>.Success(chapters));
    }

    /// <summary>
    /// Reorders chapters based on the provided chapter IDs list.
    /// </summary>
    public Task<Result> ReorderChaptersAsync(
        IReadOnlyList<Guid> chapterIds,
        CancellationToken ct = default)
    {
        try
        {
            for (int i = 0; i < chapterIds.Count; i++)
            {
                if (_chapters.TryGetValue(chapterIds[i], out var chapter))
                {
                    _chapters[chapterIds[i]] = chapter with { OrderIndex = i };
                }
            }

            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reorder chapters");
            return Task.FromResult(Result.Failure($"Reorder failed: {ex.Message}", ErrorType.Internal));
        }
    }

    /// <summary>
    /// Deletes a chapter.
    /// </summary>
    public Task<Result> DeleteChapterAsync(
        Guid chapterId,
        CancellationToken ct = default)
    {
        _chapters.TryRemove(chapterId, out _);
        return Task.FromResult(Result.Success());
    }

    /// <summary>
    /// Duplicates a chapter.
    /// </summary>
    public Task<Result<StoryChapter>> DuplicateChapterAsync(
        Guid chapterId,
        CancellationToken ct = default)
    {
        if (!_chapters.TryGetValue(chapterId, out var source))
        {
            return Task.FromResult(Result<StoryChapter>.Failure("Chapter not found", ErrorType.NotFound));
        }

        var copy = source with
        {
            Id = Guid.NewGuid(),
            Title = $"{source.Title} (Copy)",
            OrderIndex = _chapters.Count
        };

        _chapters[copy.Id] = copy;
        return Task.FromResult(Result<StoryChapter>.Success(copy));
    }
}
