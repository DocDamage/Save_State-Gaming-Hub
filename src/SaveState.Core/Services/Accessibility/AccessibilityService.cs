using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace SaveState.Core.Services.Accessibility
{
    public enum AccessibilityFeature
    {
        HighContrast,
        LargeText,
        ReducedMotion,
        ScreenReader,
        ColorBlindMode,
        KeyboardNav,
        CustomCursor,
        AudioCues,
        Subtitles,
        DyslexiaFont
    }

    public enum ColorBlindType
    {
        None,
        Protanopia,     // Red-blind
        Deuteranopia,   // Green-blind
        Tritanopia,     // Blue-blind
        Monochromacy    // Total color blindness
    }

    public class AccessibilitySettings
    {
        // Visual
        public bool HighContrastMode { get; set; }
        public double TextScale { get; set; } = 1.0;
        public bool ReducedMotion { get; set; }
        public ColorBlindType ColorBlindMode { get; set; } = ColorBlindType.None;
        public bool UseDyslexiaFont { get; set; }
        public bool ShowFocusIndicators { get; set; } = true;
        public bool LargeClickTargets { get; set; }
        
        // Audio
        public bool AudioCues { get; set; } = true;
        public bool ShowSubtitles { get; set; }
        public double SubtitleSize { get; set; } = 1.0;
        
        // Input
        public bool KeyboardNavigation { get; set; } = true;
        public bool StickyKeys { get; set; }
        public int KeyRepeatDelay { get; set; } = 500;
        public int KeyRepeatRate { get; set; } = 50;
        public bool MouseKeys { get; set; }
        
        // Screen reader
        public bool ScreenReaderSupport { get; set; }
        public bool AnnounceNotifications { get; set; } = true;
        
        // Timing
        public bool ExtendedTimeouts { get; set; }
        public int ToastDuration { get; set; } = 5000;
        public bool PauseAnimationsOnFocus { get; set; }
    }

    public class AccessibilityService
    {
        private static AccessibilityService? _instance;
        private readonly string _settingsPath;
        private AccessibilitySettings _settings;

        public event EventHandler<AccessibilitySettings>? SettingsChanged;

        public static AccessibilityService Instance => _instance ??= new AccessibilityService();
        public AccessibilitySettings Settings => _settings;

        private AccessibilityService()
        {
            _settingsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "SaveState2", "data", "accessibility.json");
            _settings = LoadSettings();
        }

        public void EnableFeature(AccessibilityFeature feature)
        {
            switch (feature)
            {
                case AccessibilityFeature.HighContrast:
                    _settings.HighContrastMode = true;
                    break;
                case AccessibilityFeature.LargeText:
                    _settings.TextScale = 1.5;
                    break;
                case AccessibilityFeature.ReducedMotion:
                    _settings.ReducedMotion = true;
                    break;
                case AccessibilityFeature.ScreenReader:
                    _settings.ScreenReaderSupport = true;
                    break;
                case AccessibilityFeature.KeyboardNav:
                    _settings.KeyboardNavigation = true;
                    break;
                case AccessibilityFeature.AudioCues:
                    _settings.AudioCues = true;
                    break;
                case AccessibilityFeature.Subtitles:
                    _settings.ShowSubtitles = true;
                    break;
                case AccessibilityFeature.DyslexiaFont:
                    _settings.UseDyslexiaFont = true;
                    break;
            }
            SaveAndNotify();
        }

        public void DisableFeature(AccessibilityFeature feature)
        {
            switch (feature)
            {
                case AccessibilityFeature.HighContrast:
                    _settings.HighContrastMode = false;
                    break;
                case AccessibilityFeature.LargeText:
                    _settings.TextScale = 1.0;
                    break;
                case AccessibilityFeature.ReducedMotion:
                    _settings.ReducedMotion = false;
                    break;
                case AccessibilityFeature.ScreenReader:
                    _settings.ScreenReaderSupport = false;
                    break;
                case AccessibilityFeature.KeyboardNav:
                    _settings.KeyboardNavigation = false;
                    break;
                case AccessibilityFeature.AudioCues:
                    _settings.AudioCues = false;
                    break;
                case AccessibilityFeature.Subtitles:
                    _settings.ShowSubtitles = false;
                    break;
                case AccessibilityFeature.DyslexiaFont:
                    _settings.UseDyslexiaFont = false;
                    break;
            }
            SaveAndNotify();
        }

        public bool IsFeatureEnabled(AccessibilityFeature feature)
        {
            return feature switch
            {
                AccessibilityFeature.HighContrast => _settings.HighContrastMode,
                AccessibilityFeature.LargeText => _settings.TextScale > 1.2,
                AccessibilityFeature.ReducedMotion => _settings.ReducedMotion,
                AccessibilityFeature.ScreenReader => _settings.ScreenReaderSupport,
                AccessibilityFeature.KeyboardNav => _settings.KeyboardNavigation,
                AccessibilityFeature.AudioCues => _settings.AudioCues,
                AccessibilityFeature.Subtitles => _settings.ShowSubtitles,
                AccessibilityFeature.DyslexiaFont => _settings.UseDyslexiaFont,
                _ => false
            };
        }

        public void SetTextScale(double scale)
        {
            _settings.TextScale = Math.Clamp(scale, 0.75, 2.0);
            SaveAndNotify();
        }

        public void SetColorBlindMode(ColorBlindType type)
        {
            _settings.ColorBlindMode = type;
            SaveAndNotify();
        }

        public void SetSubtitleSize(double size)
        {
            _settings.SubtitleSize = Math.Clamp(size, 0.75, 2.0);
            SaveAndNotify();
        }

        public void UpdateSettings(AccessibilitySettings newSettings)
        {
            _settings = newSettings;
            SaveAndNotify();
        }

        public void ResetToDefaults()
        {
            _settings = new AccessibilitySettings();
            SaveAndNotify();
        }

        // Get CSS-like filter for colorblind simulation
        public string GetColorBlindFilter()
        {
            return _settings.ColorBlindMode switch
            {
                ColorBlindType.Protanopia => "sepia(100%) saturate(1000%) hue-rotate(-50deg)",
                ColorBlindType.Deuteranopia => "sepia(100%) saturate(1000%) hue-rotate(-30deg)",
                ColorBlindType.Tritanopia => "sepia(100%) saturate(1000%) hue-rotate(180deg)",
                ColorBlindType.Monochromacy => "grayscale(100%)",
                _ => "none"
            };
        }

        // Get animation duration multiplier
        public double GetAnimationMultiplier()
        {
            return _settings.ReducedMotion ? 0 : 1.0;
        }

        // Announce to screen reader (placeholder)
        public void Announce(string message, bool interrupt = false)
        {
            if (!_settings.ScreenReaderSupport) return;

            // In production: Use platform accessibility APIs
            // Windows: UI Automation / MSAA
            // macOS: NSAccessibility
            // Linux: AT-SPI

            Console.WriteLine($"[Screen Reader] {message}");
        }

        public void AnnounceNotification(string title, string message)
        {
            if (_settings.AnnounceNotifications)
            {
                Announce($"Notification: {title}. {message}");
            }
        }

        public List<string> GetActiveFeatures()
        {
            var features = new List<string>();
            
            if (_settings.HighContrastMode) features.Add("High Contrast");
            if (_settings.TextScale > 1.2) features.Add($"Large Text ({_settings.TextScale:F1}x)");
            if (_settings.ReducedMotion) features.Add("Reduced Motion");
            if (_settings.ScreenReaderSupport) features.Add("Screen Reader");
            if (_settings.ColorBlindMode != ColorBlindType.None) 
                features.Add($"Color Blind ({_settings.ColorBlindMode})");
            if (_settings.UseDyslexiaFont) features.Add("Dyslexia Font");
            if (_settings.ShowSubtitles) features.Add("Subtitles");
            if (_settings.AudioCues) features.Add("Audio Cues");

            return features;
        }

        private void SaveAndNotify()
        {
            SaveSettings();
            SettingsChanged?.Invoke(this, _settings);
        }

        private AccessibilitySettings LoadSettings()
        {
            if (File.Exists(_settingsPath))
            {
                try
                {
                    var json = File.ReadAllText(_settingsPath);
                    return JsonSerializer.Deserialize<AccessibilitySettings>(json) 
                        ?? new AccessibilitySettings();
                }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Operation failed: {ex.Message}"); }
            }
            return new AccessibilitySettings();
        }

        private void SaveSettings()
        {
            var dir = Path.GetDirectoryName(_settingsPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(_settings, 
                new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsPath, json);
        }
    }
}
