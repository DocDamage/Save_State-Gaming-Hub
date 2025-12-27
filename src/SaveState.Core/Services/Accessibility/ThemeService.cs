using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace SaveState.Core.Services.Accessibility
{
    public class ThemeColors
    {
        public string Primary { get; set; } = "#8B5CF6";
        public string Secondary { get; set; } = "#EC4899";
        public string Accent { get; set; } = "#06B6D4";
        public string Background { get; set; } = "#0F0F1A";
        public string Surface { get; set; } = "#1A1A2E";
        public string SurfaceLight { get; set; } = "#2A2A4E";
        public string Text { get; set; } = "#FFFFFF";
        public string TextSecondary { get; set; } = "#A0A0B0";
        public string Success { get; set; } = "#10B981";
        public string Warning { get; set; } = "#F59E0B";
        public string Error { get; set; } = "#EF4444";
    }

    public class Theme
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsDark { get; set; } = true;
        public ThemeColors Colors { get; set; } = new();
        public string? BackgroundImage { get; set; }
        public double BackgroundBlur { get; set; } = 0;
        public double BackgroundOpacity { get; set; } = 0.3;
        public bool IsBuiltIn { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class ThemeService
    {
        private static ThemeService? _instance;
        private readonly string _themesPath;
        private readonly List<Theme> _themes = new();
        private Theme _currentTheme;

        public event EventHandler<Theme>? ThemeChanged;

        public static ThemeService Instance => _instance ??= new ThemeService();
        public Theme CurrentTheme => _currentTheme;
        public IReadOnlyList<Theme> AvailableThemes => _themes;

        private ThemeService()
        {
            _themesPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "SaveState2", "data", "themes");
            if (!Directory.Exists(_themesPath)) Directory.CreateDirectory(_themesPath);
            
            InitializeBuiltInThemes();
            LoadCustomThemes();
            _currentTheme = LoadSelectedTheme();
        }

        private void InitializeBuiltInThemes()
        {
            // Default dark theme (already used)
            _themes.Add(new Theme
            {
                Id = "default_dark",
                Name = "SaveState Dark",
                Description = "The default purple-accented dark theme",
                IsDark = true,
                IsBuiltIn = true,
                Colors = new ThemeColors()
            });

            // Neon theme
            _themes.Add(new Theme
            {
                Id = "neon",
                Name = "Neon Nights",
                Description = "Vibrant neon colors on deep black",
                IsDark = true,
                IsBuiltIn = true,
                Colors = new ThemeColors
                {
                    Primary = "#00FF88",
                    Secondary = "#FF00FF",
                    Accent = "#00FFFF",
                    Background = "#000000",
                    Surface = "#0A0A0A",
                    SurfaceLight = "#1A1A1A"
                }
            });

            // Midnight Blue
            _themes.Add(new Theme
            {
                Id = "midnight",
                Name = "Midnight Blue",
                Description = "Deep blue tones for late-night gaming",
                IsDark = true,
                IsBuiltIn = true,
                Colors = new ThemeColors
                {
                    Primary = "#3B82F6",
                    Secondary = "#6366F1",
                    Accent = "#22D3EE",
                    Background = "#0A1628",
                    Surface = "#132337",
                    SurfaceLight = "#1E3A52"
                }
            });

            // Sunset
            _themes.Add(new Theme
            {
                Id = "sunset",
                Name = "Sunset Vibes",
                Description = "Warm orange and pink gradient feel",
                IsDark = true,
                IsBuiltIn = true,
                Colors = new ThemeColors
                {
                    Primary = "#F97316",
                    Secondary = "#DB2777",
                    Accent = "#FBBF24",
                    Background = "#1A0A0A",
                    Surface = "#2A1515",
                    SurfaceLight = "#3D2020"
                }
            });

            // Forest
            _themes.Add(new Theme
            {
                Id = "forest",
                Name = "Forest Green",
                Description = "Natural green tones, easy on the eyes",
                IsDark = true,
                IsBuiltIn = true,
                Colors = new ThemeColors
                {
                    Primary = "#22C55E",
                    Secondary = "#84CC16",
                    Accent = "#14B8A6",
                    Background = "#0A1A0F",
                    Surface = "#152A1C",
                    SurfaceLight = "#1F3D28"
                }
            });

            // High Contrast
            _themes.Add(new Theme
            {
                Id = "high_contrast",
                Name = "High Contrast",
                Description = "Maximum contrast for visibility",
                IsDark = true,
                IsBuiltIn = true,
                Colors = new ThemeColors
                {
                    Primary = "#FFFF00",
                    Secondary = "#00FFFF",
                    Accent = "#FF00FF",
                    Background = "#000000",
                    Surface = "#000000",
                    SurfaceLight = "#333333",
                    Text = "#FFFFFF",
                    TextSecondary = "#CCCCCC"
                }
            });

            // Light theme
            _themes.Add(new Theme
            {
                Id = "light",
                Name = "Light Mode",
                Description = "Clean, bright theme for daytime",
                IsDark = false,
                IsBuiltIn = true,
                Colors = new ThemeColors
                {
                    Primary = "#7C3AED",
                    Secondary = "#DB2777",
                    Accent = "#0891B2",
                    Background = "#F8FAFC",
                    Surface = "#FFFFFF",
                    SurfaceLight = "#F1F5F9",
                    Text = "#1E293B",
                    TextSecondary = "#64748B"
                }
            });

            // Retro
            _themes.Add(new Theme
            {
                Id = "retro",
                Name = "Retro Gaming",
                Description = "Classic green terminal aesthetic",
                IsDark = true,
                IsBuiltIn = true,
                Colors = new ThemeColors
                {
                    Primary = "#00FF00",
                    Secondary = "#00CC00",
                    Accent = "#00FF00",
                    Background = "#0A0A0A",
                    Surface = "#0D1A0D",
                    SurfaceLight = "#152515",
                    Text = "#00FF00",
                    TextSecondary = "#00AA00"
                }
            });
        }

        public void ApplyTheme(string themeId)
        {
            var theme = _themes.Find(t => t.Id == themeId);
            if (theme == null) return;

            _currentTheme = theme;
            SaveSelectedTheme(themeId);
            ThemeChanged?.Invoke(this, theme);

            Console.WriteLine($"🎨 Theme applied: {theme.Name}");
        }

        public Theme CreateCustomTheme(string name, ThemeColors colors, string? backgroundImage = null)
        {
            var theme = new Theme
            {
                Id = $"custom_{Guid.NewGuid().ToString()[..8]}",
                Name = name,
                Colors = colors,
                BackgroundImage = backgroundImage,
                IsBuiltIn = false,
                CreatedAt = DateTime.UtcNow
            };

            _themes.Add(theme);
            SaveCustomTheme(theme);
            return theme;
        }

        public bool DeleteCustomTheme(string themeId)
        {
            var theme = _themes.Find(t => t.Id == themeId && !t.IsBuiltIn);
            if (theme == null) return false;

            _themes.Remove(theme);

            var path = Path.Combine(_themesPath, $"{themeId}.json");
            if (File.Exists(path)) File.Delete(path);

            return true;
        }

        public Theme? GetTheme(string id) => _themes.Find(t => t.Id == id);

        public List<Theme> GetBuiltInThemes() => _themes.FindAll(t => t.IsBuiltIn);

        public List<Theme> GetCustomThemes() => _themes.FindAll(t => !t.IsBuiltIn);

        private void SaveCustomTheme(Theme theme)
        {
            var path = Path.Combine(_themesPath, $"{theme.Id}.json");
            var json = JsonSerializer.Serialize(theme, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }

        private void LoadCustomThemes()
        {
            foreach (var file in Directory.GetFiles(_themesPath, "custom_*.json"))
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var theme = JsonSerializer.Deserialize<Theme>(json);
                    if (theme != null && !_themes.Exists(t => t.Id == theme.Id))
                    {
                        _themes.Add(theme);
                    }
                }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Operation failed: {ex.Message}"); }
            }
        }

        private void SaveSelectedTheme(string themeId)
        {
            var path = Path.Combine(_themesPath, "selected.txt");
            File.WriteAllText(path, themeId);
        }

        private Theme LoadSelectedTheme()
        {
            var path = Path.Combine(_themesPath, "selected.txt");
            if (File.Exists(path))
            {
                var themeId = File.ReadAllText(path).Trim();
                var theme = _themes.Find(t => t.Id == themeId);
                if (theme != null) return theme;
            }
            return _themes[0]; // Default
        }
    }
}
