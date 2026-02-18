using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Mugen.ComboDatabase;
using SaveState.Core.Mugen.ComboDatabase.Services;
using SaveState.Infrastructure.Persistence;

namespace SaveState.Infrastructure.Mugen.ComboDatabaseServices;

/// <summary>
/// Service for managing the combo database and discovery.
/// </summary>
public class ComboDatabaseService : IComboDatabaseService
{
    private readonly SaveStateDbContext _dbContext;
    private readonly ILogger<ComboDatabaseService> _logger;

    public ComboDatabaseService(
        SaveStateDbContext dbContext,
        ILogger<ComboDatabaseService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<ComboEntry>> AddComboAsync(
        AddComboRequest request, 
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Adding combo {ComboName} for {Character}", 
                request.Name, request.CharacterName);

            var combo = new ComboEntry
            {
                CharacterName = request.CharacterName,
                Name = request.Name,
                Description = request.Description,
                Difficulty = request.Difficulty,
                HitCount = request.HitCount,
                Damage = request.Damage,
                StartingPosition = request.StartingPosition,
                MeterRequired = request.MeterRequired,
                Moves = request.Moves ?? new List<ComboMoveEntry>(),
                InputNotation = request.InputNotation,
                VideoUrl = request.VideoUrl,
                Creator = request.Creator,
                Tags = request.Tags ?? new List<string>(),
                IsTouchOfDeath = request.IsTouchOfDeath,
                GameVersion = request.GameVersion,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Auto-calculate properties
            combo.IsOptimal = await DetermineIfOptimalAsync(combo, ct);

            _dbContext.ComboEntries.Add(combo);
            await _dbContext.SaveChangesAsync(ct);

            _logger.LogInformation("Added combo {ComboId} for {Character}", 
                combo.Id, combo.CharacterName);

            return Result<ComboEntry>.Success(combo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add combo for {Character}", request.CharacterName);
            return Result<ComboEntry>.Failure($"Failed to add combo: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<ComboEntry>> UpdateComboAsync(
        Guid comboId, 
        UpdateComboRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var combo = await _dbContext.ComboEntries
                .FirstOrDefaultAsync(c => c.Id == comboId, ct);

            if (combo == null)
            {
                return Result<ComboEntry>.Failure($"Combo {comboId} not found", ErrorType.NotFound);
            }

            if (request.Name != null) combo.Name = request.Name;
            if (request.Description != null) combo.Description = request.Description;
            if (request.Difficulty.HasValue) combo.Difficulty = request.Difficulty.Value;
            if (request.HitCount.HasValue) combo.HitCount = request.HitCount.Value;
            if (request.Damage.HasValue) combo.Damage = request.Damage.Value;
            if (request.Moves != null) combo.Moves = request.Moves;
            if (request.InputNotation != null) combo.InputNotation = request.InputNotation;
            if (request.VideoUrl != null) combo.VideoUrl = request.VideoUrl;
            if (request.Tags != null) combo.Tags = request.Tags;
            if (request.IsVerified.HasValue) combo.IsVerified = request.IsVerified.Value;
            if (request.IsOptimal.HasValue) combo.IsOptimal = request.IsOptimal.Value;

            combo.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(ct);

            _logger.LogInformation("Updated combo {ComboId}", comboId);
            return Result<ComboEntry>.Success(combo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update combo {ComboId}", comboId);
            return Result<ComboEntry>.Failure($"Failed to update combo: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<ComboEntry>> GetComboAsync(Guid comboId, CancellationToken ct = default)
    {
        try
        {
            var combo = await _dbContext.ComboEntries
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == comboId, ct);

            if (combo == null)
            {
                return Result<ComboEntry>.Failure($"Combo {comboId} not found", ErrorType.NotFound);
            }

            // Increment view count
            combo.UsageStats.ViewCount++;
            await _dbContext.SaveChangesAsync(ct);

            return Result<ComboEntry>.Success(combo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get combo {ComboId}", comboId);
            return Result<ComboEntry>.Failure($"Failed to get combo: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result> DeleteComboAsync(Guid comboId, CancellationToken ct = default)
    {
        try
        {
            var combo = await _dbContext.ComboEntries
                .FirstOrDefaultAsync(c => c.Id == comboId, ct);

            if (combo == null)
            {
                return Result.Failure($"Combo {comboId} not found", ErrorType.NotFound);
            }

            _dbContext.ComboEntries.Remove(combo);
            await _dbContext.SaveChangesAsync(ct);

            _logger.LogInformation("Deleted combo {ComboId}", comboId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete combo {ComboId}", comboId);
            return Result.Failure($"Failed to delete combo: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<List<ComboEntry>>> SearchCombosAsync(
        ComboFilter filter, 
        int page = 1, 
        int pageSize = 20,
        CancellationToken ct = default)
    {
        try
        {
            var query = _dbContext.ComboEntries.AsQueryable();

            if (!string.IsNullOrEmpty(filter.CharacterName))
                query = query.Where(c => c.CharacterName == filter.CharacterName);

            if (filter.Difficulty.HasValue)
                query = query.Where(c => c.Difficulty == filter.Difficulty.Value);

            if (filter.MinDamage.HasValue)
                query = query.Where(c => c.Damage >= filter.MinDamage.Value);

            if (filter.MaxDamage.HasValue)
                query = query.Where(c => c.Damage <= filter.MaxDamage.Value);

            if (filter.MinHits.HasValue)
                query = query.Where(c => c.HitCount >= filter.MinHits.Value);

            if (!string.IsNullOrEmpty(filter.StartingPosition))
                query = query.Where(c => c.StartingPosition == filter.StartingPosition);

            if (filter.MaxMeterRequired.HasValue)
                query = query.Where(c => c.MeterRequired <= filter.MaxMeterRequired.Value);

            if (filter.IsVerified.HasValue)
                query = query.Where(c => c.IsVerified == filter.IsVerified.Value);

            if (filter.IsOptimal.HasValue)
                query = query.Where(c => c.IsOptimal == filter.IsOptimal.Value);

            if (filter.IsTouchOfDeath.HasValue)
                query = query.Where(c => c.IsTouchOfDeath == filter.IsTouchOfDeath.Value);

            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                var term = filter.SearchTerm.ToLower();
                query = query.Where(c => 
                    c.Name.ToLower().Contains(term) ||
                    (c.Description != null && c.Description.ToLower().Contains(term)) ||
                    c.InputNotation.ToLower().Contains(term));
            }

            // Apply sorting
            query = filter.SortBy switch
            {
                ComboSortOption.Damage => filter.SortDescending 
                    ? query.OrderByDescending(c => c.Damage) 
                    : query.OrderBy(c => c.Damage),
                ComboSortOption.HitCount => filter.SortDescending 
                    ? query.OrderByDescending(c => c.HitCount) 
                    : query.OrderBy(c => c.HitCount),
                ComboSortOption.Difficulty => filter.SortDescending 
                    ? query.OrderByDescending(c => c.Difficulty) 
                    : query.OrderBy(c => c.Difficulty),
                ComboSortOption.DateAdded => filter.SortDescending 
                    ? query.OrderByDescending(c => c.CreatedAt) 
                    : query.OrderBy(c => c.CreatedAt),
                ComboSortOption.Rating => filter.SortDescending 
                    ? query.OrderByDescending(c => c.Ratings.AverageRating) 
                    : query.OrderBy(c => c.Ratings.AverageRating),
                ComboSortOption.Usage => filter.SortDescending 
                    ? query.OrderByDescending(c => c.UsageStats.MatchUsageCount) 
                    : query.OrderBy(c => c.UsageStats.MatchUsageCount),
                ComboSortOption.MeterEfficiency => filter.SortDescending 
                    ? query.OrderByDescending(c => c.Damage / (c.MeterRequired + 1)) 
                    : query.OrderBy(c => c.Damage / (c.MeterRequired + 1)),
                _ => query.OrderByDescending(c => c.Damage)
            };

            var combos = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return Result<List<ComboEntry>>.Success(combos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search combos");
            return Result<List<ComboEntry>>.Failure($"Search failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<CharacterComboDatabase>> GetCharacterCombosAsync(
        string characterName, 
        CancellationToken ct = default)
    {
        try
        {
            var combos = await _dbContext.ComboEntries
                .AsNoTracking()
                .Where(c => c.CharacterName == characterName)
                .ToListAsync(ct);

            var summary = new CharacterComboDatabase
            {
                CharacterName = characterName,
                TotalCombos = combos.Count,
                EasyCombos = combos.Count(c => c.Difficulty == ComboDifficulty.Easy),
                MediumCombos = combos.Count(c => c.Difficulty == ComboDifficulty.Medium),
                HardCombos = combos.Count(c => c.Difficulty == ComboDifficulty.Hard),
                ExpertCombos = combos.Count(c => c.Difficulty == ComboDifficulty.Expert),
                OptimalCombos = combos.Count(c => c.IsOptimal),
                TouchOfDeathCombos = combos.Count(c => c.IsTouchOfDeath),
                AverageDamage = combos.Any() ? (decimal)combos.Average(c => (double)c.Damage) : 0,
                MaxComboHits = combos.Any() ? combos.Max(c => c.HitCount) : 0,
                HighestDamage = combos.Any() ? combos.Max(c => c.Damage) : 0,
                FeaturedCombos = combos
                    .Where(c => c.IsOptimal || c.Ratings.AverageRating >= 4)
                    .Take(10)
                    .ToList(),
                CombosByStarter = combos
                    .GroupBy(c => c.StartingPosition)
                    .ToDictionary(g => g.Key, g => g.Count()),
                CombosByPosition = combos
                    .GroupBy(c => c.EndingPosition)
                    .ToDictionary(g => g.Key, g => g.Count())
            };

            return Result<CharacterComboDatabase>.Success(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get character combos for {Character}", characterName);
            return Result<CharacterComboDatabase>.Failure($"Query failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<List<ComboEntry>>> GetCombosByDifficultyAsync(
        string characterName, 
        ComboDifficulty difficulty,
        CancellationToken ct = default)
    {
        try
        {
            var combos = await _dbContext.ComboEntries
                .AsNoTracking()
                .Where(c => c.CharacterName == characterName && c.Difficulty == difficulty)
                .OrderByDescending(c => c.Damage)
                .ToListAsync(ct);

            return Result<List<ComboEntry>>.Success(combos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get combos by difficulty");
            return Result<List<ComboEntry>>.Failure($"Query failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<List<ComboEntry>>> GetOptimalCombosAsync(
        string characterName, 
        string? startingPosition = null,
        CancellationToken ct = default)
    {
        try
        {
            var query = _dbContext.ComboEntries
                .AsNoTracking()
                .Where(c => c.CharacterName == characterName && c.IsOptimal);

            if (!string.IsNullOrEmpty(startingPosition))
                query = query.Where(c => c.StartingPosition == startingPosition);

            var combos = await query
                .OrderByDescending(c => c.Damage)
                .ToListAsync(ct);

            return Result<List<ComboEntry>>.Success(combos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get optimal combos");
            return Result<List<ComboEntry>>.Failure($"Query failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<List<ComboEntry>>> GetTouchOfDeathCombosAsync(
        string? characterName = null,
        CancellationToken ct = default)
    {
        try
        {
            var query = _dbContext.ComboEntries
                .AsNoTracking()
                .Where(c => c.IsTouchOfDeath);

            if (!string.IsNullOrEmpty(characterName))
                query = query.Where(c => c.CharacterName == characterName);

            var combos = await query
                .OrderByDescending(c => c.Damage)
                .ToListAsync(ct);

            return Result<List<ComboEntry>>.Success(combos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get ToD combos");
            return Result<List<ComboEntry>>.Failure($"Query failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<List<ComboEntry>>> GetCombosByTagAsync(
        string tag, 
        string? characterName = null,
        CancellationToken ct = default)
    {
        try
        {
            var query = _dbContext.ComboEntries
                .AsNoTracking()
                .Where(c => c.Tags.Contains(tag));

            if (!string.IsNullOrEmpty(characterName))
                query = query.Where(c => c.CharacterName == characterName);

            var combos = await query
                .OrderByDescending(c => c.Ratings.AverageRating)
                .ToListAsync(ct);

            return Result<List<ComboEntry>>.Success(combos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get combos by tag");
            return Result<List<ComboEntry>>.Failure($"Query failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<ComboMatchupInfo>> GetMatchupCombosAsync(
        string characterName, 
        string opponentName,
        CancellationToken ct = default)
    {
        try
        {
            var characterCombos = await _dbContext.ComboEntries
                .AsNoTracking()
                .Where(c => c.CharacterName == characterName)
                .ToListAsync(ct);

            // Filter for matchup-specific recommendations
            var recommended = characterCombos
                .Where(c => c.Universal || !c.CharacterExceptions.Contains(opponentName))
                .OrderByDescending(c => c.Damage / (c.MeterRequired + 1))
                .Take(5)
                .ToList();

            var optimal = characterCombos
                .Where(c => c.IsOptimal)
                .Take(3)
                .ToList();

            var meterEfficient = characterCombos
                .Where(c => c.MeterRequired == 0)
                .OrderByDescending(c => c.Damage)
                .Take(3)
                .ToList();

            var info = new ComboMatchupInfo
            {
                CharacterName = characterName,
                OpponentName = opponentName,
                RecommendedCombos = recommended,
                OptimalCombos = optimal,
                MeterEfficientCombos = meterEfficient,
                Analysis = $"Found {characterCombos.Count} combos for {characterName} vs {opponentName}",
                CharacterAdvantage = CalculateCharacterAdvantage(characterCombos)
            };

            return Result<ComboMatchupInfo>.Success(info);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get matchup combos");
            return Result<ComboMatchupInfo>.Failure($"Query failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result> RecordComboUsageAsync(
        Guid comboId, 
        bool successful,
        CancellationToken ct = default)
    {
        try
        {
            var combo = await _dbContext.ComboEntries
                .FirstOrDefaultAsync(c => c.Id == comboId, ct);

            if (combo == null)
                return Result.Failure($"Combo {comboId} not found", ErrorType.NotFound);

            combo.UsageStats.MatchUsageCount++;
            await _dbContext.SaveChangesAsync(ct);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record combo usage");
            return Result.Failure($"Failed to record usage: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result> RateComboAsync(
        Guid comboId, 
        int rating, 
        string? userId = null,
        CancellationToken ct = default)
    {
        try
        {
            if (rating < 1 || rating > 5)
                return Result.Failure("Rating must be between 1 and 5", ErrorType.Validation);

            var combo = await _dbContext.ComboEntries
                .FirstOrDefaultAsync(c => c.Id == comboId, ct);

            if (combo == null)
                return Result.Failure($"Combo {comboId} not found", ErrorType.NotFound);

            var ratings = combo.Ratings;
            ratings.RatingCount++;
            
            if (!ratings.RatingDistribution.ContainsKey(rating))
                ratings.RatingDistribution[rating] = 0;
            ratings.RatingDistribution[rating]++;

            ratings.AverageRating = ratings.RatingDistribution.Sum(r => r.Key * r.Value) / ratings.RatingCount;

            await _dbContext.SaveChangesAsync(ct);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rate combo");
            return Result.Failure($"Failed to rate combo: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result> UpvoteComboAsync(Guid comboId, CancellationToken ct = default)
    {
        try
        {
            var combo = await _dbContext.ComboEntries
                .FirstOrDefaultAsync(c => c.Id == comboId, ct);

            if (combo == null)
                return Result.Failure($"Combo {comboId} not found", ErrorType.NotFound);

            combo.Ratings.Upvotes++;
            await _dbContext.SaveChangesAsync(ct);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upvote combo");
            return Result.Failure($"Failed to upvote: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result> DownvoteComboAsync(Guid comboId, CancellationToken ct = default)
    {
        try
        {
            var combo = await _dbContext.ComboEntries
                .FirstOrDefaultAsync(c => c.Id == comboId, ct);

            if (combo == null)
                return Result.Failure($"Combo {comboId} not found", ErrorType.NotFound);

            combo.Ratings.Downvotes++;
            await _dbContext.SaveChangesAsync(ct);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to downvote combo");
            return Result.Failure($"Failed to downvote: {ex.Message}", ErrorType.Internal);
        }
    }

    public Task<Result<ComboPracticeSession>> StartPracticeSessionAsync(
        Guid comboId, 
        CancellationToken ct = default)
    {
        try
        {
            var session = new ComboPracticeSession
            {
                ComboId = comboId,
                StartedAt = DateTime.UtcNow,
                Attempts = 0,
                Successes = 0
            };

            _dbContext.ComboPracticeSessions.Add(session);

            return Task.FromResult(Result<ComboPracticeSession>.Success(session));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start practice session");
            return Task.FromResult(Result<ComboPracticeSession>.Failure(
                $"Failed to start session: {ex.Message}", ErrorType.Internal));
        }
    }

    public async Task<Result> RecordPracticeAttemptAsync(
        Guid sessionId, 
        PracticeAttempt attempt,
        CancellationToken ct = default)
    {
        try
        {
            var session = await _dbContext.ComboPracticeSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId, ct);

            if (session == null)
                return Result.Failure($"Session {sessionId} not found", ErrorType.NotFound);

            session.Attempts++;
            if (attempt.Success) session.Successes++;
            session.AttemptsLog.Add(attempt);

            await _dbContext.SaveChangesAsync(ct);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record practice attempt");
            return Result.Failure($"Failed to record attempt: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<ComboPracticeSession>> CompletePracticeSessionAsync(
        Guid sessionId, 
        CancellationToken ct = default)
    {
        try
        {
            var session = await _dbContext.ComboPracticeSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId, ct);

            if (session == null)
                return Result<ComboPracticeSession>.Failure($"Session {sessionId} not found", ErrorType.NotFound);

            session.IsCompleted = true;
            session.CompletedAt = DateTime.UtcNow;
            session.TotalPracticeTime = session.CompletedAt.Value - session.StartedAt;

            // Calculate consistency rating
            if (session.Attempts > 0)
            {
                var rate = (double)session.Successes / session.Attempts;
                session.ConsistencyRating = rate switch
                {
                    >= 0.9 => 10,
                    >= 0.8 => 8,
                    >= 0.6 => 6,
                    >= 0.4 => 4,
                    _ => 2
                };
            }

            await _dbContext.SaveChangesAsync(ct);

            return Result<ComboPracticeSession>.Success(session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete practice session");
            return Result<ComboPracticeSession>.Failure(
                $"Failed to complete session: {ex.Message}", ErrorType.Internal);
        }
    }

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
                SubmittedAt = DateTime.UtcNow,
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
            submission.ReviewedAt = DateTime.UtcNow;

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

    public async Task<Result<ComboCollection>> CreateCollectionAsync(
        string name, 
        string? description, 
        string? characterName,
        string creator,
        bool isPublic = true,
        CancellationToken ct = default)
    {
        try
        {
            var collection = new ComboCollection
            {
                Name = name,
                Description = description,
                CharacterName = characterName,
                Creator = creator,
                IsPublic = isPublic,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                ComboIds = new List<Guid>()
            };

            _dbContext.ComboCollections.Add(collection);
            await _dbContext.SaveChangesAsync(ct);

            return Result<ComboCollection>.Success(collection);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create collection");
            return Result<ComboCollection>.Failure($"Failed to create: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result> AddToCollectionAsync(
        Guid collectionId, 
        Guid comboId,
        CancellationToken ct = default)
    {
        try
        {
            var collection = await _dbContext.ComboCollections
                .FirstOrDefaultAsync(c => c.Id == collectionId, ct);

            if (collection == null)
                return Result.Failure($"Collection {collectionId} not found", ErrorType.NotFound);

            if (!collection.ComboIds.Contains(comboId))
            {
                collection.ComboIds.Add(comboId);
                collection.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync(ct);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add to collection");
            return Result.Failure($"Failed to add: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<ComboCollection>> GetCollectionAsync(
        Guid collectionId, 
        CancellationToken ct = default)
    {
        try
        {
            var collection = await _dbContext.ComboCollections
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == collectionId, ct);

            if (collection == null)
                return Result<ComboCollection>.Failure($"Collection {collectionId} not found", ErrorType.NotFound);

            collection.ViewCount++;
            await _dbContext.SaveChangesAsync(ct);

            return Result<ComboCollection>.Success(collection);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get collection");
            return Result<ComboCollection>.Failure($"Query failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<List<ComboCollection>>> GetCharacterCollectionsAsync(
        string characterName, 
        bool includePrivate = false,
        CancellationToken ct = default)
    {
        try
        {
            var query = _dbContext.ComboCollections
                .AsNoTracking()
                .Where(c => c.CharacterName == characterName);

            if (!includePrivate)
                query = query.Where(c => c.IsPublic);

            var collections = await query
                .OrderByDescending(c => c.LikeCount)
                .ToListAsync(ct);

            return Result<List<ComboCollection>>.Success(collections);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get character collections");
            return Result<List<ComboCollection>>.Failure($"Query failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<List<ComboEntry>>> DiscoverCombosFromReplayAsync(
        Guid replayAnalysisId,
        CancellationToken ct = default)
    {
        try
        {
            var replay = await _dbContext.ReplayAnalyses
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == replayAnalysisId, ct);

            if (replay == null)
                return Result<List<ComboEntry>>.Failure($"Replay {replayAnalysisId} not found", ErrorType.NotFound);

            var discovered = new List<ComboEntry>();

            foreach (var detectedCombo in replay.Combos)
            {
                var combo = new ComboEntry
                {
                    CharacterName = detectedCombo.Character,
                    Name = $"{detectedCombo.HitCount}-hit Combo ({detectedCombo.TotalDamage} dmg)",
                    HitCount = detectedCombo.HitCount,
                    Damage = detectedCombo.TotalDamage,
                    Difficulty = MapDifficulty(detectedCombo.Difficulty),
                    Moves = detectedCombo.Moves.Select((m, i) => new ComboMoveEntry
                    {
                        Name = m.MoveName,
                        Input = m.Input,
                        SequenceOrder = i,
                        Damage = m.Damage
                    }).ToList(),
                    Source = "Replay Analysis",
                    IsTouchOfDeath = detectedCombo.IsTouchOfDeath
                };

                _dbContext.ComboEntries.Add(combo);
                discovered.Add(combo);
            }

            await _dbContext.SaveChangesAsync(ct);

            return Result<List<ComboEntry>>.Success(discovered);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to discover combos from replay");
            return Result<List<ComboEntry>>.Failure($"Discovery failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public Task<Result<int>> ImportCombosAsync(
        string source, 
        string data,
        CancellationToken ct = default)
    {
        try
        {
            var combos = JsonSerializer.Deserialize<List<ComboEntry>>(data);
            if (combos == null)
                return Task.FromResult(Result<int>.Failure("Invalid data format", ErrorType.Validation));

            foreach (var combo in combos)
            {
                // Id is set by EntityBase constructor
                combo.CreatedAt = DateTime.UtcNow;
                combo.Source = source;
                _dbContext.ComboEntries.Add(combo);
            }

            return Task.FromResult(Result<int>.Success(combos.Count));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import combos");
            return Task.FromResult(Result<int>.Failure($"Import failed: {ex.Message}", ErrorType.Internal));
        }
    }

    public async Task<Result<string>> ExportCombosAsync(
        string characterName, 
        ExportFormat format = ExportFormat.Json,
        CancellationToken ct = default)
    {
        try
        {
            var combos = await _dbContext.ComboEntries
                .AsNoTracking()
                .Where(c => c.CharacterName == characterName)
                .ToListAsync(ct);

            var result = format switch
            {
                ExportFormat.Json => JsonSerializer.Serialize(combos, new JsonSerializerOptions { WriteIndented = true }),
                ExportFormat.Csv => ConvertToCsv(combos),
                ExportFormat.Markdown => ConvertToMarkdown(combos),
                _ => JsonSerializer.Serialize(combos)
            };

            return Result<string>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export combos");
            return Result<string>.Failure($"Export failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public Task<Result<List<DamageOptimizationSuggestion>>> GetOptimizationSuggestionsAsync(
        Guid comboId,
        CancellationToken ct = default)
    {
        // Placeholder - would analyze combo for optimization opportunities
        return Task.FromResult(Result<List<DamageOptimizationSuggestion>>.Success(new List<DamageOptimizationSuggestion>()));
    }

    public Task<Result<DamageOptimizationSuggestion>> SuggestImprovementAsync(
        Guid comboId, 
        string suggestion, 
        int potentialDamage,
        string method,
        CancellationToken ct = default)
    {
        try
        {
            var opt = new DamageOptimizationSuggestion
            {
                ComboId = comboId,
                Suggestion = suggestion,
                PotentialExtraDamage = potentialDamage,
                Method = method,
                Verified = false
            };

            return Task.FromResult(Result<DamageOptimizationSuggestion>.Success(opt));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result<DamageOptimizationSuggestion>.Failure(
                $"Failed to suggest: {ex.Message}", ErrorType.Internal));
        }
    }

    public async Task<Result<List<ComboEntry>>> FindSimilarCombosAsync(
        Guid comboId, 
        int maxResults = 10,
        CancellationToken ct = default)
    {
        try
        {
            var combo = await _dbContext.ComboEntries
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == comboId, ct);

            if (combo == null)
                return Result<List<ComboEntry>>.Failure($"Combo {comboId} not found", ErrorType.NotFound);

            var similar = await _dbContext.ComboEntries
                .AsNoTracking()
                .Where(c => c.Id != comboId && 
                           c.CharacterName == combo.CharacterName &&
                           c.StartingPosition == combo.StartingPosition)
                .OrderBy(c => Math.Abs(c.Damage - combo.Damage))
                .Take(maxResults)
                .ToListAsync(ct);

            return Result<List<ComboEntry>>.Success(similar);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to find similar combos");
            return Result<List<ComboEntry>>.Failure($"Search failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<ComboRoutesAnalysis>> GetComboRoutesAsync(
        string characterName,
        CancellationToken ct = default)
    {
        try
        {
            var combos = await _dbContext.ComboEntries
                .AsNoTracking()
                .Where(c => c.CharacterName == characterName)
                .ToListAsync(ct);

            var analysis = new ComboRoutesAnalysis
            {
                CharacterName = characterName
            };

            // Analyze common starters
            analysis.CommonStarters = combos
                .GroupBy(c => c.Moves.FirstOrDefault()?.Name ?? "Unknown")
                .Select(g => new RouteInfo
                {
                    Move = g.Key,
                    OccurrenceCount = g.Count(),
                    AverageDamage = (int)g.Average(c => c.Damage)
                })
                .OrderByDescending(r => r.OccurrenceCount)
                .Take(5)
                .ToList();

            // Analyze common enders
            analysis.CommonEnders = combos
                .GroupBy(c => c.Moves.LastOrDefault()?.Name ?? "Unknown")
                .Select(g => new RouteInfo
                {
                    Move = g.Key,
                    OccurrenceCount = g.Count(),
                    AverageDamage = (int)g.Average(c => c.Damage)
                })
                .OrderByDescending(r => r.OccurrenceCount)
                .Take(5)
                .ToList();

            return Result<ComboRoutesAnalysis>.Success(analysis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get combo routes");
            return Result<ComboRoutesAnalysis>.Failure($"Analysis failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result> LinkRelatedCombosAsync(
        Guid comboId1, 
        Guid comboId2,
        CancellationToken ct = default)
    {
        try
        {
            var combo1 = await _dbContext.ComboEntries
                .FirstOrDefaultAsync(c => c.Id == comboId1, ct);

            if (combo1 == null)
                return Result.Failure($"Combo {comboId1} not found", ErrorType.NotFound);

            if (!combo1.RelatedComboIds.Contains(comboId2))
            {
                combo1.RelatedComboIds.Add(comboId2);
                await _dbContext.SaveChangesAsync(ct);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to link combos");
            return Result.Failure($"Failed to link: {ex.Message}", ErrorType.Internal);
        }
    }

    // Helper methods

    private async Task<bool> DetermineIfOptimalAsync(ComboEntry combo, CancellationToken ct)
    {
        var sameStarter = await _dbContext.ComboEntries
            .AsNoTracking()
            .Where(c => c.CharacterName == combo.CharacterName && 
                       c.StartingPosition == combo.StartingPosition &&
                       c.Id != combo.Id)
            .ToListAsync(ct);

        if (!sameStarter.Any()) return true;

        return combo.Damage >= sameStarter.Max(c => c.Damage);
    }

    private static decimal CalculateCharacterAdvantage(List<ComboEntry> combos)
    {
        if (!combos.Any()) return 0;

        var avgDamage = combos.Average(c => c.Damage);
        var avgMeterEfficiency = combos.Average(c => c.Damage / (c.MeterRequired + 1));

        // Simple formula: normalize and combine
        return (decimal)(avgDamage / 1000.0 + avgMeterEfficiency / 100.0);
    }

    private static ComboDifficulty MapDifficulty(Core.Mugen.ReplayAnalysis.ComboDifficulty difficulty)
    {
        return difficulty switch
        {
            Core.Mugen.ReplayAnalysis.ComboDifficulty.Easy => ComboDifficulty.Easy,
            Core.Mugen.ReplayAnalysis.ComboDifficulty.Medium => ComboDifficulty.Medium,
            Core.Mugen.ReplayAnalysis.ComboDifficulty.Hard => ComboDifficulty.Hard,
            Core.Mugen.ReplayAnalysis.ComboDifficulty.VeryHard => ComboDifficulty.VeryHard,
            Core.Mugen.ReplayAnalysis.ComboDifficulty.TOD => ComboDifficulty.TOD,
            _ => ComboDifficulty.Medium
        };
    }

    private static string ConvertToCsv(List<ComboEntry> combos)
    {
        var lines = new List<string> { "Name,Character,Difficulty,Damage,Hits,Meter" };
        lines.AddRange(combos.Select(c => 
            $"\"{c.Name}\",{c.CharacterName},{c.Difficulty},{c.Damage},{c.HitCount},{c.MeterRequired}"));
        return string.Join("\n", lines);
    }

    private static string ConvertToMarkdown(List<ComboEntry> combos)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("# Combo Database");
        sb.AppendLine();
        
        foreach (var combo in combos)
        {
            sb.AppendLine($"## {combo.Name}");
            sb.AppendLine($"- **Character:** {combo.CharacterName}");
            sb.AppendLine($"- **Damage:** {combo.Damage}");
            sb.AppendLine($"- **Hits:** {combo.HitCount}");
            sb.AppendLine($"- **Difficulty:** {combo.Difficulty}");
            sb.AppendLine($"- **Input:** {combo.InputNotation}");
            sb.AppendLine();
        }

        return sb.ToString();
    }
}
