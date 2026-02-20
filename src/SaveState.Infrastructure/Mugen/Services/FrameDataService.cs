using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Mugen.CharacterFrameAnalysis;
using SaveState.Core.Mugen.CharacterFrameAnalysis.Services;
using SaveState.Infrastructure.Persistence;

namespace SaveState.Infrastructure.Mugen.Services;

/// <summary>
/// Implementation of frame data service with caching and persistence.
/// </summary>
public class FrameDataService : IFrameDataService
{
    private readonly SaveStateDbContext _dbContext;
    private readonly IMemoryCache _cache;
    private readonly ILogger<FrameDataService> _logger;
    private readonly FrameDataAnalyzer _analyzer;
    private readonly ITimeProvider _timeProvider;
    private readonly TimeSpan _cacheDuration = TimeSpan.FromHours(1);

    public FrameDataService(
        SaveStateDbContext dbContext,
        IMemoryCache cache,
        ILogger<FrameDataService> logger,
        ITimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _cache = cache;
        _logger = logger;
        _analyzer = new FrameDataAnalyzer();
        _timeProvider = timeProvider;
    }

    public async Task<Result<CharacterFrameData>> LoadFrameDataAsync(string characterPath, CancellationToken ct = default)
    {
        try
        {
            if (!Directory.Exists(characterPath))
            {
                return Result<CharacterFrameData>.Failure($"Character path not found: {characterPath}", ErrorType.NotFound);
            }

            _logger.LogInformation("Loading frame data for {Path}", characterPath);
            
            var frameData = _analyzer.ParseCharacterFrameData(characterPath);
            
            // Cache it
            var cacheKey = $"framedata:{frameData.CharacterName}";
            _cache.Set(cacheKey, frameData, _cacheDuration);
            
            // Save to database for persistence
            await SaveFrameDataAsync(frameData, ct);
            
            return Result<CharacterFrameData>.Success(frameData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load frame data for {Path}", characterPath);
            return Result<CharacterFrameData>.Failure($"Failed to load frame data: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<CharacterFrameData>> GetFrameDataAsync(string characterName, CancellationToken ct = default)
    {
        var cacheKey = $"framedata:{characterName}";
        
        // Try cache first
        if (_cache.TryGetValue(cacheKey, out CharacterFrameData? cached))
        {
            return Result<CharacterFrameData>.Success(cached!);
        }
        
        // Try database
        var entity = await _dbContext.CharacterFrameData
            .AsNoTracking()
            .Include(c => c.Moves)
            .FirstOrDefaultAsync(c => c.CharacterName == characterName, ct);
        
        if (entity != null)
        {
            var frameData = MapToDomain(entity);
            _cache.Set(cacheKey, frameData, _cacheDuration);
            return Result<CharacterFrameData>.Success(frameData);
        }
        
        return Result<CharacterFrameData>.Failure($"Frame data not found for {characterName}", ErrorType.NotFound);
    }

    public async Task<Result> SaveFrameDataAsync(CharacterFrameData frameData, CancellationToken ct = default)
    {
        try
        {
            var existing = await _dbContext.CharacterFrameData
                .FirstOrDefaultAsync(c => c.CharacterName == frameData.CharacterName, ct);
            
            if (existing != null)
            {
                // Update existing
                existing.LastUpdated = _timeProvider.UtcNow;
                existing.Health = frameData.Health;
                existing.WalkSpeed = frameData.WalkSpeed;
                existing.BackWalkSpeed = frameData.BackWalkSpeed;
                existing.DashDistance = frameData.DashDistance;
                existing.JumpHeight = frameData.JumpHeight;
                existing.PreJumpFrames = frameData.PreJumpFrames;
                
                // Update moves
                _dbContext.MoveFrameData.RemoveRange(existing.Moves);
                existing.Moves = frameData.AllMoves.Select(MapToEntity).ToList();
            }
            else
            {
                // Create new
                var entity = new CharacterFrameDataEntity
                {
                    CharacterName = frameData.CharacterName,
                    Version = frameData.Version,
                    LastUpdated = _timeProvider.UtcNow,
                    Health = frameData.Health,
                    WalkSpeed = frameData.WalkSpeed,
                    BackWalkSpeed = frameData.BackWalkSpeed,
                    DashDistance = frameData.DashDistance,
                    JumpHeight = frameData.JumpHeight,
                    PreJumpFrames = frameData.PreJumpFrames,
                    Moves = frameData.AllMoves.Select(MapToEntity).ToList()
                };
                
                _dbContext.CharacterFrameData.Add(entity);
            }
            
            await _dbContext.SaveChangesAsync(ct);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save frame data for {Character}", frameData.CharacterName);
            return Result.Failure($"Failed to save frame data: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<MatchupAnalysis>> AnalyzeMatchupAsync(string char1Name, string char2Name, CancellationToken ct = default)
    {
        var char1Result = await GetFrameDataAsync(char1Name, ct);
        if (char1Result.IsFailure) return Result<MatchupAnalysis>.Failure(char1Result.Error!, char1Result.ErrorType);
        
        var char2Result = await GetFrameDataAsync(char2Name, ct);
        if (char2Result.IsFailure) return Result<MatchupAnalysis>.Failure(char2Result.Error!, char2Result.ErrorType);
        
        var analysis = _analyzer.AnalyzeMatchup(char1Result.Value, char2Result.Value);
        return Result<MatchupAnalysis>.Success(analysis);
    }

    public async Task<Result<List<PunishableMove>>> GetPunishableMovesAsync(string characterName, int playerSpeed = 5, CancellationToken ct = default)
    {
        var frameDataResult = await GetFrameDataAsync(characterName, ct);
        if (frameDataResult.IsFailure) return Result<List<PunishableMove>>.Failure(frameDataResult.Error!, frameDataResult.ErrorType);
        
        var punishable = _analyzer.FindPunishableMoves(frameDataResult.Value, playerSpeed);
        return Result<List<PunishableMove>>.Success(punishable);
    }

    public async Task<Result<MoveComparison>> CompareMovesAsync(
        string char1Name, string move1Name, 
        string char2Name, string move2Name, 
        CancellationToken ct = default)
    {
        var char1Result = await GetFrameDataAsync(char1Name, ct);
        if (char1Result.IsFailure) return Result<MoveComparison>.Failure(char1Result.Error!, char1Result.ErrorType);
        
        var char2Result = await GetFrameDataAsync(char2Name, ct);
        if (char2Result.IsFailure) return Result<MoveComparison>.Failure(char2Result.Error!, char2Result.ErrorType);
        
        var move1 = char1Result.Value.AllMoves.FirstOrDefault(m => 
            m.MoveName.Equals(move1Name, StringComparison.OrdinalIgnoreCase));
        var move2 = char2Result.Value.AllMoves.FirstOrDefault(m => 
            m.MoveName.Equals(move2Name, StringComparison.OrdinalIgnoreCase));
        
        if (move1 == null)
            return Result<MoveComparison>.Failure($"Move {move1Name} not found for {char1Name}", ErrorType.NotFound);
        if (move2 == null)
            return Result<MoveComparison>.Failure($"Move {move2Name} not found for {char2Name}", ErrorType.NotFound);
        
        var comparison = new MoveComparison
        {
            Move1 = move1,
            Move2 = move2
        };
        
        // Generate advantages
        if (move1.StartupFrames < move2.StartupFrames)
            comparison.Advantages1.Add($"Faster startup ({move1.StartupFrames}f vs {move2.StartupFrames}f)");
        if (move1.Damage > move2.Damage)
            comparison.Advantages1.Add($"More damage ({move1.Damage} vs {move2.Damage})");
        if (move1.BlockAdvantage > move2.BlockAdvantage)
            comparison.Advantages1.Add($"Better on block ({move1.BlockAdvantage:+#;-#;0} vs {move2.BlockAdvantage:+#;-#;0})");
            
        if (move2.StartupFrames < move1.StartupFrames)
            comparison.Advantages2.Add($"Faster startup ({move2.StartupFrames}f vs {move1.StartupFrames}f)");
        if (move2.Damage > move1.Damage)
            comparison.Advantages2.Add($"More damage ({move2.Damage} vs {move1.Damage})");
        if (move2.BlockAdvantage > move1.BlockAdvantage)
            comparison.Advantages2.Add($"Better on block ({move2.BlockAdvantage:+#;-#;0} vs {move1.BlockAdvantage:+#;-#;0})");
        
        return Result<MoveComparison>.Success(comparison);
    }

    public async Task<Result<List<string>>> GetCharactersWithFrameDataAsync(CancellationToken ct = default)
    {
        var characters = await _dbContext.CharacterFrameData
            .AsNoTracking()
            .Select(c => c.CharacterName)
            .ToListAsync(ct);
        
        return Result<List<string>>.Success(characters);
    }

    public async Task<Result<CharacterFrameData>> RefreshFrameDataAsync(string characterPath, CancellationToken ct = default)
    {
        var characterName = Path.GetFileName(characterPath);
        var cacheKey = $"framedata:{characterName}";
        _cache.Remove(cacheKey);
        
        return await LoadFrameDataAsync(characterPath, ct);
    }

    #region Mapping

    private CharacterFrameData MapToDomain(CharacterFrameDataEntity entity)
    {
        var frameData = new CharacterFrameData
        {
            CharacterName = entity.CharacterName,
            Version = entity.Version,
            LastUpdated = entity.LastUpdated,
            Health = entity.Health,
            WalkSpeed = entity.WalkSpeed,
            BackWalkSpeed = entity.BackWalkSpeed,
            DashDistance = entity.DashDistance,
            JumpHeight = entity.JumpHeight,
            PreJumpFrames = entity.PreJumpFrames
        };

        foreach (var move in entity.Moves)
        {
            var domainMove = new MoveFrameData
            {
                MoveName = move.MoveName,
                Command = move.Command,
                StartupFrames = move.StartupFrames,
                ActiveFrames = move.ActiveFrames,
                RecoveryFrames = move.RecoveryFrames,
                HitAdvantage = move.HitAdvantage,
                BlockAdvantage = move.BlockAdvantage,
                Damage = move.Damage,
                ChipDamage = move.ChipDamage,
                MeterGain = move.MeterGain,
                HitLevel = move.HitLevel,
                IsAirborne = move.IsAirborne,
                IsInvincible = move.IsInvincible,
                InvincibilityFrames = move.InvincibilityFrames,
                Armor = move.Armor,
                ArmorHits = move.ArmorHits,
                IsProjectile = move.IsProjectile,
                IsThrow = move.IsThrow,
                IsOverhead = move.IsOverhead,
                CausesKnockdown = move.CausesKnockdown,
                IsCancelable = move.IsCancelable,
                CancelWindow = move.CancelWindow,
                Notes = move.Notes
            };

            // Categorize moves
            if (move.MoveType == MoveType.StandingNormal)
                frameData.StandingNormals.Add(domainMove);
            else if (move.MoveType == MoveType.CrouchingNormal)
                frameData.CrouchingNormals.Add(domainMove);
            else if (move.MoveType == MoveType.AirNormal)
                frameData.AirNormals.Add(domainMove);
            else if (move.MoveType == MoveType.CommandMove)
                frameData.CommandMoves.Add(domainMove);
            else if (move.MoveType == MoveType.SpecialMove)
                frameData.SpecialMoves.Add(domainMove);
            else if (move.MoveType == MoveType.SuperMove)
                frameData.SuperMoves.Add(domainMove);
            else if (move.MoveType == MoveType.Throw)
                frameData.Throws.Add(domainMove);
        }

        return frameData;
    }

    private MoveFrameDataEntity MapToEntity(MoveFrameData domain)
    {
        return new MoveFrameDataEntity
        {
            MoveName = domain.MoveName,
            Command = domain.Command,
            StartupFrames = domain.StartupFrames,
            ActiveFrames = domain.ActiveFrames,
            RecoveryFrames = domain.RecoveryFrames,
            HitAdvantage = domain.HitAdvantage,
            BlockAdvantage = domain.BlockAdvantage,
            Damage = domain.Damage,
            ChipDamage = domain.ChipDamage,
            MeterGain = domain.MeterGain,
            HitLevel = domain.HitLevel,
            IsAirborne = domain.IsAirborne,
            IsInvincible = domain.IsInvincible,
            InvincibilityFrames = domain.InvincibilityFrames,
            Armor = domain.Armor,
            ArmorHits = domain.ArmorHits,
            IsProjectile = domain.IsProjectile,
            IsThrow = domain.IsThrow,
            IsOverhead = domain.IsOverhead,
            CausesKnockdown = domain.CausesKnockdown,
            IsCancelable = domain.IsCancelable,
            CancelWindow = domain.CancelWindow,
            Notes = domain.Notes
        };
    }

    #endregion
}
