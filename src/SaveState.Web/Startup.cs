using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace SaveState.Web;

/// <summary>
/// Web dashboard ASP.NET Core startup configuration.
/// PHASE 7: REQUIRED - Web Dashboard Foundation (Session 4)
/// </summary>
public class Startup
{
    public IConfiguration Configuration { get; }

    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    /// <summary>
    /// Configures services for dependency injection.
    /// </summary>
    public void ConfigureServices(IServiceCollection services)
    {
        // Add controllers
        services.AddControllers();

        // Add Razor pages
        services.AddRazorPages();

        // Add Entity Framework Core
        services.AddDbContext<SaveStateDbContext>(options =>
            options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

        // Add authentication
        services.AddAuthentication("Bearer")
            .AddJwtBearer("Bearer", options =>
            {
                options.Authority = Configuration["Auth:Authority"];
                options.TokenValidationParameters = new()
                {
                    ValidateAudience = false
                };
            });

        // Add authorization
        services.AddAuthorization();

        // Add API versioning
        services.AddApiVersioning(options =>
        {
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.DefaultApiVersion = new ApiVersion(1, 0);
        });

        // Add CORS
        services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", builder =>
            {
                builder.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });
        });

        // Add SignalR for real-time features
        services.AddSignalR();

        // Add health checks
        services.AddHealthChecks();

        // Add logging
        services.AddLogging();
    }

    /// <summary>
    /// Configures the HTTP request pipeline.
    /// </summary>
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();

        app.UseCors("AllowAll");

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapRazorPages();
            endpoints.MapHealthChecks("/health");
            endpoints.MapHub<GameHub>("/hubs/game");
            endpoints.MapHub<SyncHub>("/hubs/sync");
        });
    }
}

/// <summary>
/// Program entry point for web dashboard.
/// </summary>
public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            });
}

/// <summary>
/// Real-time game hub for SignalR.
/// </summary>
public class GameHub : Hub
{
    /// <summary>
    /// Notifies clients when a game is launched.
    /// </summary>
    public async Task NotifyGameLaunchedAsync(string gameId, string gameName)
    {
        await Clients.All.SendAsync("GameLaunched", gameId, gameName);
    }

    /// <summary>
    /// Notifies clients of achievement unlocked.
    /// </summary>
    public async Task NotifyAchievementUnlockedAsync(string achievementId, string achievementName)
    {
        await Clients.All.SendAsync("AchievementUnlocked", achievementId, achievementName);
    }

    /// <summary>
    /// Notifies clients of save state created.
    /// </summary>
    public async Task NotifySaveStateCreatedAsync(string gameId, string slotNumber)
    {
        await Clients.All.SendAsync("SaveStateCreated", gameId, slotNumber);
    }
}

/// <summary>
/// Real-time sync hub for cloud synchronization.
/// </summary>
public class SyncHub : Hub
{
    /// <summary>
    /// Starts cloud sync operation.
    /// </summary>
    public async Task StartSyncAsync(string userId)
    {
        await Clients.User(userId).SendAsync("SyncStarted");
    }

    /// <summary>
    /// Reports sync progress.
    /// </summary>
    public async Task ReportProgressAsync(string userId, double progress)
    {
        await Clients.User(userId).SendAsync("SyncProgress", progress);
    }

    /// <summary>
    /// Completes sync operation.
    /// </summary>
    public async Task CompleteSyncAsync(string userId)
    {
        await Clients.User(userId).SendAsync("SyncCompleted");
    }
}

/// <summary>
/// Games controller for API endpoints.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class GamesController : ControllerBase
{
    private readonly IGameService _gameService;

    public GamesController(IGameService gameService)
    {
        _gameService = gameService;
    }

    /// <summary>
    /// Gets all games for the authenticated user.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<GameDto>>> GetGames()
    {
        var games = await _gameService.GetAllGamesAsync();
        return Ok(games);
    }

    /// <summary>
    /// Gets a specific game by ID.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<GameDto>> GetGame(string id)
    {
        var game = await _gameService.GetGameByIdAsync(id);
        if (game == null)
            return NotFound();

        return Ok(game);
    }

    /// <summary>
    /// Creates a new game entry.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<GameDto>> CreateGame([FromBody] CreateGameRequest request)
    {
        var game = await _gameService.CreateGameAsync(request.Title, request.PlatformId);
        return CreatedAtAction(nameof(GetGame), new { id = game.Id }, game);
    }

    /// <summary>
    /// Updates an existing game.
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateGame(string id, [FromBody] UpdateGameRequest request)
    {
        var result = await _gameService.UpdateGameAsync(id, request.Title);
        if (!result)
            return NotFound();

        return NoContent();
    }

    /// <summary>
    /// Deletes a game.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteGame(string id)
    {
        var result = await _gameService.DeleteGameAsync(id);
        if (!result)
            return NotFound();

        return NoContent();
    }
}

/// <summary>
/// Game service interface.
/// </summary>
public interface IGameService
{
    Task<IEnumerable<GameDto>> GetAllGamesAsync();
    Task<GameDto?> GetGameByIdAsync(string id);
    Task<GameDto> CreateGameAsync(string title, string platformId);
    Task<bool> UpdateGameAsync(string id, string title);
    Task<bool> DeleteGameAsync(string id);
}

/// <summary>
/// Game DTO.
/// </summary>
public record GameDto(
    string Id,
    string Title,
    string Platform,
    int PlayTime,
    int SessionCount);

/// <summary>
/// Create game request.
/// </summary>
public record CreateGameRequest(
    string Title,
    string PlatformId);

/// <summary>
/// Update game request.
/// </summary>
public record UpdateGameRequest(
    string Title);

/// <summary>
/// SaveState database context (placeholder).
/// </summary>
public class SaveStateDbContext
{
}
