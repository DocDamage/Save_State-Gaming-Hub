using System.Text;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Mugen.Services;
using SaveState.Core.Mugen.ValueObjects;

namespace SaveState.Infrastructure.Mugen.ContentCreation;

/// <summary>
/// Implementation of content creation and modding tools for MUGEN.
/// </summary>
public class ContentCreationService : IContentCreationService
{
    private readonly ILogger<ContentCreationService> _logger;
    private readonly IMugenTemplateRepository _templateRepository;
    private readonly IMugenValidationService _validationService;

    public ContentCreationService(
        ILogger<ContentCreationService> logger,
        IMugenTemplateRepository templateRepository,
        IMugenValidationService validationService)
    {
        _logger = logger;
        _templateRepository = templateRepository;
        _validationService = validationService;
    }

    /// <inheritdoc />
    public async Task<Result<CharacterCreationResult>> CreateCharacterAsync(
        CharacterCreationRequest request, 
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Creating character: {CharacterName}", request.CharacterName);

            var warnings = new List<string>();
            var generatedFiles = new List<string>();

            // Validate character name
            if (string.IsNullOrWhiteSpace(request.CharacterName))
            {
                return Result<CharacterCreationResult>.Failure(
                    "Character name cannot be empty", 
                    ErrorType.Validation);
            }

            // Sanitize character name for file system
            var safeName = SanitizeFileName(request.CharacterName);
            var charDir = Path.Combine("chars", safeName);

            // Ensure directory exists
            Directory.CreateDirectory(charDir);

            // Generate .def file
            var defPath = Path.Combine(charDir, $"{safeName}.def");
            await GenerateCharacterDefFileAsync(defPath, request, warnings);
            generatedFiles.Add(defPath);

            // Generate .cmd file (commands)
            var cmdPath = Path.Combine(charDir, $"{safeName}.cmd");
            await GenerateCharacterCmdFileAsync(cmdPath, request);
            generatedFiles.Add(cmdPath);

            // Generate .cns file (constants)
            var cnsPath = Path.Combine(charDir, $"{safeName}.cns");
            await GenerateCharacterCnsFileAsync(cnsPath, request);
            generatedFiles.Add(cnsPath);

            // Generate AI script if requested
            if (request.Options.IncludeAi)
            {
                var aiPath = Path.Combine(charDir, $"{safeName}-AI.cmd");
                await GenerateAiScriptAsync(aiPath, request, AiDifficulty.Medium);
                generatedFiles.Add(aiPath);
            }

            // Generate palettes if requested
            if (request.Options.GeneratePalettes)
            {
                var palPath = Path.Combine(charDir, $"{safeName}.act");
                await GenerateDefaultPaletteAsync(palPath);
                generatedFiles.Add(palPath);
            }

            _logger.LogInformation("Character {CharacterName} created successfully with {FileCount} files", 
                request.CharacterName, generatedFiles.Count);

            return Result<CharacterCreationResult>.Success(new CharacterCreationResult(
                request.CharacterName,
                charDir,
                generatedFiles,
                warnings,
                true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create character: {CharacterName}", request.CharacterName);
            return Result<CharacterCreationResult>.Failure(
                $"Character creation failed: {ex.Message}", 
                ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result> ModifyCharacterMovesetAsync(
        string characterName, 
        MovesetModification modification, 
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Modifying moveset for: {CharacterName}", characterName);

            var safeName = SanitizeFileName(characterName);
            var charDir = Path.Combine("chars", safeName);

            if (!Directory.Exists(charDir))
            {
                return Result.Failure($"Character directory not found: {charDir}", ErrorType.NotFound);
            }

            // Backup original if requested
            if (modification.BackupOriginal)
            {
                var backupDir = Path.Combine(charDir, "backup_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss"));
                Directory.CreateDirectory(backupDir);
                
                foreach (var file in Directory.GetFiles(charDir, "*.cmd"))
                {
                    File.Copy(file, Path.Combine(backupDir, Path.GetFileName(file)), true);
                }
            }

            var cmdPath = Path.Combine(charDir, $"{safeName}.cmd");
            
            // Read existing commands
            var existingCommands = File.Exists(cmdPath) 
                ? await File.ReadAllTextAsync(cmdPath, ct)
                : string.Empty;

            // Generate new command entries
            var newCommands = new StringBuilder(existingCommands);
            
            foreach (var move in modification.NewMoves)
            {
                newCommands.AppendLine();
                newCommands.AppendLine($"; {move.Name}");
                newCommands.AppendLine($"[Command]");
                newCommands.AppendLine($"name = \"{move.Name}\"");
                newCommands.AppendLine($"command = {move.Input}");
                newCommands.AppendLine($"time = 15");
            }

            await File.WriteAllTextAsync(cmdPath, newCommands.ToString(), ct);

            _logger.LogInformation("Moveset modified for {CharacterName}: +{NewMoves} moves", 
                characterName, modification.NewMoves.Count);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to modify moveset for: {CharacterName}", characterName);
            return Result.Failure($"Moveset modification failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<StageCreationResult>> CreateStageAsync(
        StageCreationRequest request, 
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Creating stage: {StageName}", request.StageName);

            var warnings = new List<string>();
            var generatedFiles = new List<string>();

            var safeName = SanitizeFileName(request.StageName);
            var stageDir = Path.Combine("stages", safeName);
            Directory.CreateDirectory(stageDir);

            // Generate .def file
            var defPath = Path.Combine(stageDir, $"{safeName}.def");
            await GenerateStageDefFileAsync(defPath, request);
            generatedFiles.Add(defPath);

            // Generate stage config
            if (request.Options.IncludeLighting)
            {
                var lightingPath = Path.Combine(stageDir, $"{safeName}-lighting.def");
                await GenerateLightingConfigAsync(lightingPath, request);
                generatedFiles.Add(lightingPath);
            }

            _logger.LogInformation("Stage {StageName} created with {FileCount} files", 
                request.StageName, generatedFiles.Count);

            return Result<StageCreationResult>.Success(new StageCreationResult(
                request.StageName,
                stageDir,
                generatedFiles,
                warnings,
                true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create stage: {StageName}", request.StageName);
            return Result<StageCreationResult>.Failure(
                $"Stage creation failed: {ex.Message}", 
                ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<AiScriptResult>> GenerateAiScriptAsync(
        string characterName, 
        AiDifficulty difficulty, 
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Generating AI script for {CharacterName} at {Difficulty} level", 
                characterName, difficulty);

            var behaviors = GenerateAiBehaviors(difficulty);
            var scriptContent = BuildAiScript(characterName, difficulty, behaviors);

            return Result<AiScriptResult>.Success(new AiScriptResult(
                characterName,
                scriptContent,
                difficulty,
                behaviors,
                true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate AI script for: {CharacterName}", characterName);
            return Result<AiScriptResult>.Failure(
                $"AI script generation failed: {ex.Message}", 
                ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<ValidationResult>> ValidateContentAsync(
        ContentValidationRequest request, 
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Validating content: {FilePath}", request.FilePath);

            var errorList = new List<string>();
            var warningList = new List<string>();

            if (!File.Exists(request.FilePath))
            {
                return Result<ValidationResult>.Failure(
                    $"File not found: {request.FilePath}", 
                    ErrorType.NotFound);
            }

            // Basic validation based on content type
            switch (request.ContentType.ToLower())
            {
                case "character":
                    await ValidateCharacterAsync(request.FilePath, errorList, warningList);
                    break;
                case "stage":
                    await ValidateStageAsync(request.FilePath, errorList, warningList);
                    break;
                default:
                    warningList.Add($"Unknown content type: {request.ContentType}");
                    break;
            }

            var isValid = errorList.Count == 0;

            // Convert to proper types
            var validationErrors = errorList.Select((e, i) => new ValidationError($"ERR{i:000}", e)).ToList();
            var validationWarnings = warningList.Select((w, i) => new ValidationWarning($"WRN{i:000}", w)).ToList();

            return Result<ValidationResult>.Success(new ValidationResult(
                isValid,
                validationErrors,
                validationWarnings,
                new List<string>()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Validation failed for: {FilePath}", request.FilePath);
            return Result<ValidationResult>.Failure(
                $"Validation failed: {ex.Message}", 
                ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<PackageResult>> PackageContentAsync(
        ContentPackageRequest request, 
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Packaging content: {PackageName} v{Version}", 
                request.PackageName, request.Version);

            // Create package directory
            var packageDir = Path.Combine("packages", $"{request.PackageName}_{request.Version}");
            Directory.CreateDirectory(packageDir);

            var includedFiles = new List<string>();
            
            // Copy all content files
            foreach (var file in request.ContentFiles)
            {
                if (File.Exists(file))
                {
                    var destPath = Path.Combine(packageDir, Path.GetFileName(file));
                    File.Copy(file, destPath, true);
                    includedFiles.Add(destPath);
                }
            }

            // Generate package manifest
            var manifestPath = Path.Combine(packageDir, "manifest.json");
            await GeneratePackageManifestAsync(manifestPath, request);
            includedFiles.Add(manifestPath);

            // Create zip archive
            var zipPath = $"{packageDir}.zip";
            System.IO.Compression.ZipFile.CreateFromDirectory(packageDir, zipPath);

            // Calculate checksum
            var checksum = await CalculateFileChecksumAsync(zipPath);

            var fileInfo = new FileInfo(zipPath);

            _logger.LogInformation("Package created: {ZipPath} ({Size} bytes)", zipPath, fileInfo.Length);

            return Result<PackageResult>.Success(new PackageResult(
                zipPath,
                fileInfo.Length,
                includedFiles,
                checksum,
                true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Packaging failed for: {PackageName}", request.PackageName);
            return Result<PackageResult>.Failure(
                $"Packaging failed: {ex.Message}", 
                ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<MergeResult>> MergeContentAsync(
        ContentMergeRequest request, 
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Merging content: {MergeType}", request.MergeType);

            var mergedFiles = new List<string>();
            var conflicts = new List<MergeConflict>();
            var warnings = new List<string>();

            Directory.CreateDirectory(request.OutputPath);

            // Backup sources if requested
            if (request.Options.BackupSources)
            {
                var backupDir = Path.Combine(request.OutputPath, "backup_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss"));
                Directory.CreateDirectory(backupDir);
            }

            // Process each source file
            foreach (var sourceFile in request.SourceFiles)
            {
                if (!File.Exists(sourceFile))
                {
                    warnings.Add($"Source file not found: {sourceFile}");
                    continue;
                }

                var fileName = Path.GetFileName(sourceFile);
                var destPath = Path.Combine(request.OutputPath, fileName);

                // Check for conflicts
                if (File.Exists(destPath) && request.Options.ResolveConflicts)
                {
                    var resolution = ResolveConflict(sourceFile, destPath, request.Options);
                    
                    if (resolution == "skip")
                    {
                        conflicts.Add(new MergeConflict(
                            fileName,
                            "FileExists",
                            new[] { sourceFile, destPath },
                            "Skipped"));
                        continue;
                    }
                }

                File.Copy(sourceFile, destPath, true);
                mergedFiles.Add(destPath);
            }

            return Result<MergeResult>.Success(new MergeResult(
                request.OutputPath,
                mergedFiles,
                conflicts,
                warnings,
                true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Merge failed");
            return Result<MergeResult>.Failure(
                $"Merge failed: {ex.Message}", 
                ErrorType.Internal);
        }
    }

    #region Private Helper Methods

    private string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Join("_", name.Split(invalid, StringSplitOptions.RemoveEmptyEntries)).Trim();
    }

    private async Task GenerateCharacterDefFileAsync(string path, CharacterCreationRequest request, List<string> warnings)
    {
        var content = new StringBuilder();
        content.AppendLine($"; {request.CharacterName} Character Definition");
        content.AppendLine($"; Created by {request.Options.Author}");
        content.AppendLine($"; {request.Options.Description}");
        content.AppendLine();
        content.AppendLine("[Info]");
        content.AppendLine($"name = \"{request.CharacterName}\"");
        content.AppendLine($"displayname = \"{request.CharacterName}\"");
        content.AppendLine($"versiondate = {DateTime.UtcNow:yyyy-MM-dd}");
        content.AppendLine($"mugenversion = 1.1");
        content.AppendLine($"author = \"{request.Options.Author}\"");
        content.AppendLine($"pal.defaults = 1,2,3,4");
        content.AppendLine();
        content.AppendLine("[Files]");
        content.AppendLine($"cmd = {SanitizeFileName(request.CharacterName)}.cmd");
        content.AppendLine($"cns = {SanitizeFileName(request.CharacterName)}.cns");
        content.AppendLine($"st = {SanitizeFileName(request.CharacterName)}.cns");
        content.AppendLine($"stcommon = common1.cns");
        content.AppendLine($"sprite = {SanitizeFileName(request.CharacterName)}.sff");
        content.AppendLine($"anim = {SanitizeFileName(request.CharacterName)}.air");
        content.AppendLine($"sound = {SanitizeFileName(request.CharacterName)}.snd");
        content.AppendLine($"pal1 = {SanitizeFileName(request.CharacterName)}.act");

        await File.WriteAllTextAsync(path, content.ToString());
    }

    private async Task GenerateCharacterCmdFileAsync(string path, CharacterCreationRequest request)
    {
        var content = new StringBuilder();
        content.AppendLine($"; {request.CharacterName} Command Definitions");
        content.AppendLine();
        content.AppendLine("[Remap]");
        content.AppendLine("x = x");
        content.AppendLine("y = y");
        content.AppendLine("z = z");
        content.AppendLine("a = a");
        content.AppendLine("b = b");
        content.AppendLine("c = c");
        content.AppendLine("s = s");
        content.AppendLine();
        content.AppendLine("[Defaults]");
        content.AppendLine("command.time = 15");
        content.AppendLine("command.buffer.time = 1");
        content.AppendLine();
        content.AppendLine("[Command]");
        content.AppendLine("name = \"QCF_P\"");
        content.AppendLine("command = ~D, DF, F, x");
        content.AppendLine("time = 15");
        content.AppendLine();
        content.AppendLine("[Command]");
        content.AppendLine("name = \"QCB_K\"");
        content.AppendLine("command = ~D, DB, B, a");
        content.AppendLine("time = 15");

        // Add custom moves
        foreach (var move in request.Moves)
        {
            content.AppendLine();
            content.AppendLine($"[Command]");
            content.AppendLine($"name = \"{move.Name}\"");
            content.AppendLine($"command = {move.Input}");
            content.AppendLine($"time = 15");
        }

        await File.WriteAllTextAsync(path, content.ToString());
    }

    private async Task GenerateCharacterCnsFileAsync(string path, CharacterCreationRequest request)
    {
        var content = new StringBuilder();
        content.AppendLine($"; {request.CharacterName} Constants");
        content.AppendLine();
        content.AppendLine("[Data]");
        content.AppendLine($"life = {request.Stats.Health}");
        content.AppendLine($"power = {request.Stats.Power}");
        content.AppendLine($"attack = {request.Stats.Attack}");
        content.AppendLine($"defence = {request.Stats.Defense}");
        content.AppendLine("fall.defence_up = 50");
        content.AppendLine("liedown.time = 60");
        content.AppendLine("airjuggle = 15");
        content.AppendLine("sparkno = 2");
        content.AppendLine("guard.sparkno = 40");
        content.AppendLine("KO.echo = 0");
        content.AppendLine("volume = 0");
        content.AppendLine("IntPersistIndex = 60");
        content.AppendLine("FloatPersistIndex = 40");
        content.AppendLine();
        content.AppendLine("[Size]");
        content.AppendLine("xscale = 1");
        content.AppendLine("yscale = 1");
        content.AppendLine("ground.back = 15");
        content.AppendLine("ground.front = 16");
        content.AppendLine("air.back = 12");
        content.AppendLine("air.front = 12");
        content.AppendLine("height = 60");
        content.AppendLine("attack.dist = 160");
        content.AppendLine("proj.attack.dist = 90");
        content.AppendLine("proj.doscale = 0");
        content.AppendLine("head.pos = -5, -90");
        content.AppendLine("mid.pos = -5, -60");
        content.AppendLine("shadowoffset = 0");
        content.AppendLine("draw.offset = 0,0");
        content.AppendLine();
        content.AppendLine("[Velocity]");
        content.AppendLine($"walk.fwd = {request.Stats.Speed:F2}");
        content.AppendLine($"walk.back = {-request.Stats.Speed * 0.75m:F2}");
        content.AppendLine($"run.fwd = {request.Stats.Speed * 2:F2}, 0");
        content.AppendLine($"run.back = {-request.Stats.Speed * 1.5m:F2}, 0");
        content.AppendLine($"jump.neu = 0, {-request.Stats.JumpHeight:F2}");
        content.AppendLine($"jump.back = {-request.Stats.Speed:F2}, {-request.Stats.JumpHeight:F2}");
        content.AppendLine($"jump.fwd = {request.Stats.Speed:F2}, {-request.Stats.JumpHeight:F2}");
        content.AppendLine("runjump.back = -2.55,-8.1");
        content.AppendLine("runjump.neu = 0,-8.1");
        content.AppendLine("runjump.fwd = 2.4,-8.1");
        content.AppendLine("airjump.neu = 0,-8.1");
        content.AppendLine("airjump.back = -2.55");
        content.AppendLine("airjump.fwd = 2.4,-8.1");
        content.AppendLine();
        content.AppendLine("[Movement]");
        content.AppendLine("airjump.num = 0");
        content.AppendLine("airjump.height = 35");
        content.AppendLine("yaccel = 0.44");
        content.AppendLine("stand.friction = 0.85");
        content.AppendLine("crouch.friction = 0.82");
        content.AppendLine("stand.friction.threshold = 2");
        content.AppendLine("crouch.friction.threshold = 0.05");

        await File.WriteAllTextAsync(path, content.ToString());
    }

    private async Task GenerateAiScriptAsync(string path, CharacterCreationRequest request, AiDifficulty difficulty)
    {
        var content = new StringBuilder();
        content.AppendLine($"; {request.CharacterName} AI Script");
        content.AppendLine($"; Difficulty: {difficulty}");
        content.AppendLine();
        content.AppendLine("[StateDef -1]");
        content.AppendLine();
        content.AppendLine("; AI Activation");
        content.AppendLine("[State -1, AI]");
        content.AppendLine("type = VarSet");
        content.AppendLine("triggerall = !Var(59)");
        content.AppendLine("trigger1 = !IsHelper");
        content.AppendLine("v = 59");
        content.AppendLine("value = 1");
        content.AppendLine();
        content.AppendLine("; Basic attack when close");
        content.AppendLine("[State -1, BasicAttack]");
        content.AppendLine("type = ChangeState");
        content.AppendLine("value = 200");
        content.AppendLine("triggerall = Var(59)");
        content.AppendLine("triggerall = P2bodydist X < 50");
        content.AppendLine("triggerall = P2life > 0");
        content.AppendLine("triggerall = Random < 500");
        content.AppendLine("trigger1 = Statetype = S");
        content.AppendLine("trigger1 = Ctrl");

        await File.WriteAllTextAsync(path, content.ToString());
    }

    private async Task GenerateDefaultPaletteAsync(string path)
    {
        // Create a simple ACT palette file (256 colors, 3 bytes each)
        var palette = new byte[768];
        
        // Fill with a basic gradient
        for (int i = 0; i < 256; i++)
        {
            palette[i * 3] = (byte)i;     // R
            palette[i * 3 + 1] = (byte)i; // G
            palette[i * 3 + 2] = (byte)i; // B
        }

        await File.WriteAllBytesAsync(path, palette);
    }

    private async Task GenerateStageDefFileAsync(string path, StageCreationRequest request)
    {
        var content = new StringBuilder();
        content.AppendLine($"; {request.StageName} Stage Definition");
        content.AppendLine($"; Author: {request.Options.Author}");
        content.AppendLine($"; {request.Options.Description}");
        content.AppendLine();
        content.AppendLine("[Info]");
        content.AppendLine($"name = \"{request.StageName}\"");
        content.AppendLine("displayname = \"Battle Stage\"");
        content.AppendLine($"versiondate = {DateTime.UtcNow:yyyy-MM-dd}");
        content.AppendLine("mugenversion = 1.1");
        content.AppendLine($"author = \"{request.Options.Author}\"");
        content.AppendLine("bgmusic = sound.mp3");
        content.AppendLine("bgclearcolor = 0,0,0");
        content.AppendLine();
        content.AppendLine("[Camera]");
        content.AppendLine("startx = 0");
        content.AppendLine("starty = 0");
        content.AppendLine("boundleft = -224");
        content.AppendLine("boundright = 224");
        content.AppendLine("boundhigh = -112");
        content.AppendLine("boundlow = 0");
        content.AppendLine("verticalfollow = 0.2");
        content.AppendLine("floortension = 0");
        content.AppendLine("tension = 50");
        content.AppendLine();
        content.AppendLine("[PlayerInfo]");
        content.AppendLine($"p1startx = {request.Bounds.Left + 80}");
        content.AppendLine("p1starty = 0");
        content.AppendLine("p1startz = 0");
        content.AppendLine("p1facing = 1");
        content.AppendLine($"p2startx = {request.Bounds.Right - 80}");
        content.AppendLine("p2starty = 0");
        content.AppendLine("p2startz = 0");
        content.AppendLine("p2facing = -1");
        content.AppendLine("leftbound = -2000");
        content.AppendLine("rightbound = 2000");

        await File.WriteAllTextAsync(path, content.ToString());
    }

    private async Task GenerateLightingConfigAsync(string path, StageCreationRequest request)
    {
        var content = new StringBuilder();
        content.AppendLine("; Lighting Configuration");
        content.AppendLine("[Lighting]");
        content.AppendLine("ambient = 128,128,128");
        content.AppendLine("intensity = 256");
        content.AppendLine("direction = 0,0,-1");
        content.AppendLine("color = 255,255,255");
        content.AppendLine("specular = 128,128,128");

        await File.WriteAllTextAsync(path, content.ToString());
    }

    private async Task GeneratePackageManifestAsync(string path, ContentPackageRequest request)
    {
        var manifest = $@"{{
  ""name"": ""{request.PackageName}"",
  ""version"": ""{request.Version}"",
  ""author"": ""{request.Metadata.Author}"",
  ""description"": ""{request.Metadata.Description}"",
  ""tags"": [{string.Join(", ", request.Metadata.Tags.Select(t => $@"""{t}"""))}],
  ""created"": ""{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}"",
  ""files"": [{string.Join(", ", request.ContentFiles.Select(f => $@"""{Path.GetFileName(f)}"""))}]
}}";

        await File.WriteAllTextAsync(path, manifest);
    }

    private List<string> GenerateAiBehaviors(AiDifficulty difficulty)
    {
        var behaviors = new List<string> { "approach", "retreat", "attack" };
        
        switch (difficulty)
        {
            case AiDifficulty.VeryEasy:
                behaviors.AddRange(new[] { "random_jump", "rare_block" });
                break;
            case AiDifficulty.Easy:
                behaviors.AddRange(new[] { "basic_combo", "occasional_block" });
                break;
            case AiDifficulty.Medium:
                behaviors.AddRange(new[] { "combo_strings", "block_prediction", "special_moves" });
                break;
            case AiDifficulty.Hard:
                behaviors.AddRange(new[] { "advanced_combos", "tick_throws", "frame_traps", "punish" });
                break;
            case AiDifficulty.VeryHard:
            case AiDifficulty.Expert:
                behaviors.AddRange(new[] { "optimize_damage", "mixups", "okizeme", "resource_management", "reaction_punish" });
                break;
        }

        return behaviors;
    }

    private string BuildAiScript(string characterName, AiDifficulty difficulty, List<string> behaviors)
    {
        var script = new StringBuilder();
        script.AppendLine($"; AI Script for {characterName}");
        script.AppendLine($"; Difficulty: {difficulty}");
        script.AppendLine($"; Behaviors: {string.Join(", ", behaviors)}");
        script.AppendLine();
        script.AppendLine("[StateDef -1]");
        script.AppendLine("; AI Activation");
        script.AppendLine("[State -1, AI]");
        script.AppendLine("type = VarSet");
        script.AppendLine("v = 59");
        script.AppendLine("value = 1");

        return script.ToString();
    }

    private async Task ValidateCharacterAsync(string filePath, List<string> errors, List<string> warnings)
    {
        try
        {
            var content = await File.ReadAllTextAsync(filePath);
            
            if (!content.Contains("[Info]"))
                errors.Add("Missing [Info] section");
            
            if (!content.Contains("[Files]"))
                errors.Add("Missing [Files] section");

            if (!content.Contains("name ="))
                warnings.Add("Character name not specified");
        }
        catch (Exception ex)
            {
            errors.Add($"Failed to read file: {ex.Message}");
        }
    }

    private async Task ValidateStageAsync(string filePath, List<string> errors, List<string> warnings)
    {
        try
        {
            var content = await File.ReadAllTextAsync(filePath);
            
            if (!content.Contains("[Info]"))
                errors.Add("Missing [Info] section");
            
            if (!content.Contains("[Camera]"))
                errors.Add("Missing [Camera] section");
        }
        catch (Exception ex)
        {
            errors.Add($"Failed to read file: {ex.Message}");
        }
    }

    private string ResolveConflict(string sourceFile, string destFile, MergeOptions options)
    {
        return options.ConflictResolutionStrategy.ToLower() switch
        {
            "overwrite" => "overwrite",
            "skip" => "skip",
            "rename" => "rename",
            _ => "skip"
        };
    }

    private async Task<string> CalculateFileChecksumAsync(string filePath)
    {
        using var md5 = System.Security.Cryptography.MD5.Create();
        await using var stream = File.OpenRead(filePath);
        var hash = await md5.ComputeHashAsync(stream);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    #endregion
}
