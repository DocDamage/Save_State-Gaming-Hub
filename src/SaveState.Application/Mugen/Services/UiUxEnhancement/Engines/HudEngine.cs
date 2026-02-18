namespace SaveState.Application.Mugen.Services.UiUxEnhancement.Engines;

using Microsoft.Extensions.Logging;

/// <summary>
/// Engine responsible for generating and optimizing HUD elements and layouts.
/// </summary>
public class HudEngine
{
    private readonly ILogger<HudEngine>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="HudEngine"/> class.
    /// </summary>
    public HudEngine()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HudEngine"/> class with a logger.
    /// </summary>
    public HudEngine(ILogger<HudEngine> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Generates HUD elements based on enabled mechanics.
    /// </summary>
    /// <param name="enabledMechanics">List of enabled mechanic identifiers.</param>
    /// <returns>List of HUD elements.</returns>
    public IReadOnlyList<HudElement> GenerateHudElements(IEnumerable<string> enabledMechanics)
    {
        var elements = new List<HudElement>();
        var mechanicsList = enabledMechanics.ToList();

        _logger?.LogDebug("Generating HUD elements for {Count} mechanics", mechanicsList.Count);

        // Core HUD elements that are always present
        elements.Add(new HudElement
        {
            Id = "health_bar",
            Type = "bar",
            Position = new Mugen.Vector2(50, 30),
            Size = new Mugen.Vector2(200, 20),
            Label = "Health",
            Color = "#FF4444",
            UpdateFrequency = 30
        });

        elements.Add(new HudElement
        {
            Id = "power_meter",
            Type = "bar",
            Position = new Mugen.Vector2(50, 60),
            Size = new Mugen.Vector2(200, 15),
            Label = "Power",
            Color = "#4444FF",
            UpdateFrequency = 30
        });

        // Add mechanic-specific elements
        foreach (var mechanic in mechanicsList)
        {
            var mechanicElements = GenerateElementsForMechanic(mechanic);
            elements.AddRange(mechanicElements);
        }

        _logger?.LogInformation("Generated {Count} HUD elements", elements.Count);
        return elements;
    }

    /// <summary>
    /// Calculates the optimal layout for HUD elements based on screen resolution.
    /// </summary>
    /// <param name="screenResolution">The screen resolution.</param>
    /// <param name="elementCount">The number of elements to layout.</param>
    /// <returns>The optimized HUD layout.</returns>
    public HudLayout CalculateOptimalLayout(ScreenResolution screenResolution, int elementCount)
    {
        _logger?.LogDebug("Calculating optimal layout for {ElementCount} elements at {Width}x{Height}",
            elementCount, screenResolution.Width, screenResolution.Height);

        var elementPositions = new Dictionary<string, Mugen.Vector2>();
        var safeZones = CalculateSafeZones(screenResolution);

        // Calculate scaling factor based on resolution
        var baseWidth = 1920.0;
        var scalingFactor = screenResolution.Width / baseWidth;

        // Position elements in a grid layout
        var cols = Math.Min(3, Math.Max(1, elementCount / 2));
        var spacing = 250 * scalingFactor;
        var startX = safeZones.Left + 20;
        var startY = safeZones.Top + 20;

        for (var i = 0; i < elementCount; i++)
        {
            var col = i % cols;
            var row = i / cols;
            var x = startX + (col * spacing);
            var y = startY + (row * 50 * scalingFactor);
            elementPositions[$"element_{i}"] = new Mugen.Vector2(x, y);
        }

        var layout = new HudLayout
        {
            ScreenResolution = screenResolution,
            ElementPositions = elementPositions,
            SafeZones = safeZones,
            ScalingFactor = (float)scalingFactor,
            LayoutType = DetermineLayoutType(screenResolution, elementCount)
        };

        _logger?.LogInformation("Calculated {LayoutType} layout with scaling {ScalingFactor:F2}",
            layout.LayoutType, layout.ScalingFactor);

        return layout;
    }

    private IEnumerable<HudElement> GenerateElementsForMechanic(string mechanic)
    {
        var elements = new List<HudElement>();

        switch (mechanic.ToLowerInvariant())
        {
            case "parry":
                elements.Add(new HudElement
                {
                    Id = "parry_indicator",
                    Type = "indicator",
                    Position = new Mugen.Vector2(300, 30),
                    Size = new Mugen.Vector2(40, 40),
                    Label = "Parry",
                    Color = "#FFD700",
                    UpdateFrequency = 60
                });
                break;

            case "stance":
                elements.Add(new HudElement
                {
                    Id = "stance_display",
                    Type = "text",
                    Position = new Mugen.Vector2(400, 30),
                    Size = new Mugen.Vector2(100, 30),
                    Label = "Stance",
                    Color = "#00FF00",
                    UpdateFrequency = 30
                });
                break;

            case "combo":
                elements.Add(new HudElement
                {
                    Id = "combo_counter",
                    Type = "counter",
                    Position = new Mugen.Vector2(500, 30),
                    Size = new Mugen.Vector2(80, 40),
                    Label = "Combo",
                    Color = "#FF8800",
                    UpdateFrequency = 60
                });
                break;

            case "meter":
                elements.Add(new HudElement
                {
                    Id = "super_meter",
                    Type = "bar",
                    Position = new Mugen.Vector2(600, 60),
                    Size = new Mugen.Vector2(150, 15),
                    Label = "Super",
                    Color = "#FF00FF",
                    UpdateFrequency = 30
                });
                break;

            case "buff":
                elements.Add(new HudElement
                {
                    Id = "buff_status",
                    Type = "icon",
                    Position = new Mugen.Vector2(700, 30),
                    Size = new Mugen.Vector2(32, 32),
                    Label = "Buffs",
                    Color = "#00FFFF",
                    UpdateFrequency = 30
                });
                break;

            case "cooldown":
                elements.Add(new HudElement
                {
                    Id = "cooldown_timers",
                    Type = "timer",
                    Position = new Mugen.Vector2(750, 30),
                    Size = new Mugen.Vector2(100, 20),
                    Label = "Cooldowns",
                    Color = "#888888",
                    UpdateFrequency = 60
                });
                break;
        }

        return elements;
    }

    private SafeZones CalculateSafeZones(ScreenResolution resolution)
    {
        // Calculate safe zones as percentage of screen (5% margin)
        var marginPercent = 0.05;
        var horizontalMargin = (int)(resolution.Width * marginPercent);
        var verticalMargin = (int)(resolution.Height * marginPercent);

        return new SafeZones
        {
            Top = verticalMargin,
            Bottom = verticalMargin,
            Left = horizontalMargin,
            Right = horizontalMargin
        };
    }

    private static string DetermineLayoutType(ScreenResolution resolution, int elementCount)
    {
        var aspectRatio = (double)resolution.Width / resolution.Height;

        if (aspectRatio >= 2.0)
            return "ultrawide";
        if (aspectRatio >= 1.7)
            return elementCount > 8 ? "dense_widescreen" : "widescreen";
        if (aspectRatio >= 1.3)
            return "standard";
        return "compact";
    }
}
