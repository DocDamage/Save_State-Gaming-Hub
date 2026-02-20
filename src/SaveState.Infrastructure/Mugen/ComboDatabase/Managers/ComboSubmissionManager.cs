using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Mugen.ComboDatabase;
using SaveState.Infrastructure.Persistence;

namespace SaveState.Infrastructure.Mugen.ComboDatabase.Managers;

/// <summary>
/// Manages combo submission workflow.
/// </summary>
public class ComboSubmissionManager
{
    private readonly SaveStateDbContext _dbContext;
    private readonly ILogger<ComboSubmissionManager> _logger;
    private readonly ITimeProvider _timeProvider;

    public ComboSubmissionManager(
        SaveStateDbContext dbContext,
        ILogger<ComboSubmissionManager> logger,
        ITimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Submits a combo for approval.
    /// </summary>
    public async Task<Result<ComboSubmission>> SubmitComboAsync(
        Guid comboId,
        string submitterName,
        string? submitterId = null,
        CancellationToken ct = default)
    {
        try
        {
            var combo = await _dbContext.ComboEntries
                .FirstOrDefaultAsync(c => c.Id == comboId, ct);

            if (combo == null)
                return Result<ComboSubmission>.Failure($"Combo {comboId} not found", ErrorType.NotFound);

            combo.IsPendingApproval = true;

            var submission = new ComboSubmission
            {
                ComboId = comboId,
                SubmitterName = submitterName,
                SubmitterId = submitterId,
                SubmittedAt = _timeProvider.UtcNow,
                Status = SubmissionStatus.Pending
            };

            _dbContext.ComboSubmissions.Add(submission);
            await _dbContext.SaveChangesAsync(ct);

            return Result<ComboSubmission>.Success(submission);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit combo");
            return Result<ComboSubmission>.Failure($"Failed to submit: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Reviews a submitted combo.
    /// </summary>
    public async Task<Result> ReviewSubmissionAsync(
        Guid submissionId,
        SubmissionStatus status,
        string? reviewerNotes = null,
        string? reviewedBy = null,
        CancellationToken ct = default)
    {
        try
        {
            var submission = await _dbContext.ComboSubmissions
                .FirstOrDefaultAsync(s => s.Id == submissionId, ct);

            if (submission == null)
                return Result.Failure($"Submission {submissionId} not found", ErrorType.NotFound);

            submission.Status = status;
            submission.ReviewerNotes = reviewerNotes;
            submission.ReviewedBy = reviewedBy;
            submission.ReviewedAt = _timeProvider.UtcNow;

            // Update combo status
            var combo = await _dbContext.ComboEntries
                .FirstOrDefaultAsync(c => c.Id == submission.ComboId, ct);

            if (combo != null)
            {
                combo.IsPendingApproval = status == SubmissionStatus.Pending || status == SubmissionStatus.UnderReview;
                combo.IsVerified = status == SubmissionStatus.Approved;
            }

            await _dbContext.SaveChangesAsync(ct);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to review submission");
            return Result.Failure($"Failed to review: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Gets pending submissions with pagination.
    /// </summary>
    public async Task<Result<List<ComboSubmission>>> GetPendingSubmissionsAsync(
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        try
        {
            var submissions = await _dbContext.ComboSubmissions
                .AsNoTracking()
                .Where(s => s.Status == SubmissionStatus.Pending)
                .OrderByDescending(s => s.SubmittedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return Result<List<ComboSubmission>>.Success(submissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get pending submissions");
            return Result<List<ComboSubmission>>.Failure($"Query failed: {ex.Message}", ErrorType.Internal);
        }
    }
}
