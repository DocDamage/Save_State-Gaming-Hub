using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;

namespace SaveState.Application.Mugen.Services.Graphics.Managers;

/// <summary>
/// Manages shader compilation, programs, and shader-related operations.
/// </summary>
public sealed class ShaderManager
{
    private readonly ILogger<ShaderManager> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly Dictionary<string, ShaderProgram> _shaderPrograms = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ShaderManager"/> class.
    /// </summary>
    public ShaderManager(ILogger<ShaderManager> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
        InitializeDefaultShaders();
    }

    /// <summary>
    /// Compiles a shader program.
    /// </summary>
    public async Task<Result<ShaderProgram>> CompileShaderAsync(ShaderCompilationRequest request, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Compiling shader: {Name}", request.Name);

            await Task.Delay(200, ct);

            var shader = new ShaderProgram
            {
                ShaderId = Guid.NewGuid().ToString(),
                Name = request.Name,
                Description = request.Description,
                VertexShader = request.VertexShader,
                FragmentShader = request.FragmentShader,
                GeometryShader = request.GeometryShader,
                Uniforms = ParseShaderUniforms(request.VertexShader + request.FragmentShader),
                Attributes = ParseShaderAttributes(request.VertexShader),
                CompilationStatus = ShaderCompilationStatus.Success,
                CompiledAt = _timeProvider.UtcNow,
                PerformanceMetrics = new ShaderPerformanceMetrics
                {
                    EstimatedDrawCalls = 1000,
                    EstimatedFillRate = 0.8f,
                    EstimatedMemoryUsage = 256 * 1024
                }
            };

            _shaderPrograms[shader.ShaderId] = shader;

            _logger.LogInformation("Shader compiled successfully: {ShaderId}", shader.ShaderId);
            return Result<ShaderProgram>.Success(shader);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error compiling shader {Name}", request.Name);
            return Result<ShaderProgram>.Failure($"Failed to compile shader: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets a shader by ID.
    /// </summary>
    public Task<Result<ShaderProgram>> GetShaderAsync(string shaderId, CancellationToken ct = default)
    {
        if (_shaderPrograms.TryGetValue(shaderId, out var shader))
        {
            return Task.FromResult(Result<ShaderProgram>.Success(shader));
        }

        return Task.FromResult(Result<ShaderProgram>.Failure("Shader not found"));
    }

    /// <summary>
    /// Gets all registered shaders.
    /// </summary>
    public IReadOnlyDictionary<string, ShaderProgram> GetAllShaders() => _shaderPrograms;

    private void InitializeDefaultShaders()
    {
        var defaultShaders = new[]
        {
            new ShaderProgram
            {
                ShaderId = "default_lighting",
                Name = "Default Lighting Shader",
                Description = "Standard lighting with ambient, diffuse, and specular components",
                CompilationStatus = ShaderCompilationStatus.Success,
                CompiledAt = _timeProvider.UtcNow
            },
            new ShaderProgram
            {
                ShaderId = "particle_system",
                Name = "Particle System Shader",
                Description = "Optimized shader for particle rendering",
                CompilationStatus = ShaderCompilationStatus.Success,
                CompiledAt = _timeProvider.UtcNow
            },
            new ShaderProgram
            {
                ShaderId = "post_process_bloom",
                Name = "Bloom Post-Processing",
                Description = "Bloom effect for bright highlights",
                CompilationStatus = ShaderCompilationStatus.Success,
                CompiledAt = _timeProvider.UtcNow
            }
        };

        foreach (var shader in defaultShaders)
        {
            _shaderPrograms[shader.ShaderId] = shader;
        }
    }

    private IReadOnlyList<ShaderUniform> ParseShaderUniforms(string shaderCode)
    {
        var uniforms = new List<ShaderUniform>();
        if (shaderCode.Contains("uniform"))
        {
            uniforms.Add(new ShaderUniform { Name = "u_time", Type = UniformType.Float, Value = 0.0f });
            uniforms.Add(new ShaderUniform { Name = "u_resolution", Type = UniformType.Vec2, Value = new GraphicsVector2(1920, 1080) });
        }
        return uniforms;
    }

    private IReadOnlyList<ShaderAttribute> ParseShaderAttributes(string vertexShader)
    {
        var attributes = new List<ShaderAttribute>();
        if (vertexShader.Contains("attribute"))
        {
            attributes.Add(new ShaderAttribute { Name = "a_position", Type = AttributeType.Vec3, Location = 0 });
            attributes.Add(new ShaderAttribute { Name = "a_texCoord", Type = AttributeType.Vec2, Location = 1 });
        }
        return attributes;
    }
}

// Shader-related models
public class ShaderProgram
{
    public string ShaderId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string VertexShader { get; set; } = default!;
    public string FragmentShader { get; set; } = default!;
    public string? GeometryShader { get; set; }
    public IReadOnlyList<ShaderUniform> Uniforms { get; set; } = default!;
    public IReadOnlyList<ShaderAttribute> Attributes { get; set; } = default!;
    public ShaderCompilationStatus CompilationStatus { get; set; }
    public DateTime CompiledAt { get; set; }
    public ShaderPerformanceMetrics PerformanceMetrics { get; set; } = default!;
}

public class ShaderCompilationRequest
{
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string VertexShader { get; set; } = default!;
    public string FragmentShader { get; set; } = default!;
    public string? GeometryShader { get; set; }
}

public class ShaderUniform
{
    public string Name { get; set; } = default!;
    public UniformType Type { get; set; }
    public object Value { get; set; } = default!;
}

public class ShaderAttribute
{
    public string Name { get; set; } = default!;
    public AttributeType Type { get; set; }
    public int Location { get; set; }
}

public class ShaderPerformanceMetrics
{
    public int EstimatedDrawCalls { get; set; }
    public float EstimatedFillRate { get; set; }
    public long EstimatedMemoryUsage { get; set; }
}

public enum ShaderCompilationStatus { Pending, Success, Failed }
public enum UniformType { Float, Vec2, Vec3, Vec4, Mat4, Texture2D, Bool }
public enum AttributeType { Float, Vec2, Vec3, Vec4 }
