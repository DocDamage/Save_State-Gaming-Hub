using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace SaveState.Core.Services.Input
{
    public enum HotkeyAction
    {
        // Navigation
        OpenLibrary,
        OpenSettings,
        OpenAiConfig,
        OpenBattle,
        OpenFusion,
        OpenMugen,
        
        // Features
        TakeScreenshot,
        StartRecording,
        StopRecording,
        QuickSave,
        QuickLoad,
        ToggleFullscreen,
        ToggleMute,
        
        // AI
        AskAi,
        GenerateImage,
        
        // Search
        GlobalSearch,
        SearchGames,
        
        // Quick actions
        LaunchLastGame,
        RandomGame,
        PauseGame
    }

    public class HotkeyBinding
    {
        public HotkeyAction Action { get; set; }
        public int KeyCode { get; set; }
        public bool Ctrl { get; set; }
        public bool Alt { get; set; }
        public bool Shift { get; set; }
        public bool Win { get; set; }
        public bool IsEnabled { get; set; } = true;

        public string DisplayString
        {
            get
            {
                var parts = new List<string>();
                if (Ctrl) parts.Add("Ctrl");
                if (Alt) parts.Add("Alt");
                if (Shift) parts.Add("Shift");
                if (Win) parts.Add("Win");
                parts.Add(GetKeyName(KeyCode));
                return string.Join("+", parts);
            }
        }

        private static string GetKeyName(int keyCode)
        {
            // Common key codes (Windows VK codes)
            return keyCode switch
            {
                0x41 => "A", 0x42 => "B", 0x43 => "C", 0x44 => "D", 0x45 => "E",
                0x46 => "F", 0x47 => "G", 0x48 => "H", 0x49 => "I", 0x4A => "J",
                0x4B => "K", 0x4C => "L", 0x4D => "M", 0x4E => "N", 0x4F => "O",
                0x50 => "P", 0x51 => "Q", 0x52 => "R", 0x53 => "S", 0x54 => "T",
                0x55 => "U", 0x56 => "V", 0x57 => "W", 0x58 => "X", 0x59 => "Y",
                0x5A => "Z",
                0x70 => "F1", 0x71 => "F2", 0x72 => "F3", 0x73 => "F4",
                0x74 => "F5", 0x75 => "F6", 0x76 => "F7", 0x77 => "F8",
                0x78 => "F9", 0x79 => "F10", 0x7A => "F11", 0x7B => "F12",
                0x2C => "PrintScreen", 0x91 => "ScrollLock", 0x13 => "Pause",
                _ => $"Key{keyCode}"
            };
        }
    }

    public class HotkeyService : IDisposable
    {
        private static HotkeyService? _instance;
        private readonly string _configPath;
        private readonly Dictionary<HotkeyAction, HotkeyBinding> _bindings = new();
        private readonly Dictionary<HotkeyAction, Action> _handlers = new();
        private bool _isListening;

        public event EventHandler<HotkeyAction>? HotkeyTriggered;

        public static HotkeyService Instance => _instance ??= new HotkeyService();

        private HotkeyService()
        {
            _configPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "SaveState2", "data", "hotkeys.json");
            
            InitializeDefaultBindings();
            LoadBindings();
        }

        private void InitializeDefaultBindings()
        {
            // Navigation shortcuts
            SetDefaultBinding(HotkeyAction.OpenLibrary, 0x4C, ctrl: true); // Ctrl+L
            SetDefaultBinding(HotkeyAction.OpenSettings, 0xBC, ctrl: true); // Ctrl+,
            SetDefaultBinding(HotkeyAction.OpenAiConfig, 0x49, ctrl: true, shift: true); // Ctrl+Shift+I
            SetDefaultBinding(HotkeyAction.OpenBattle, 0x42, ctrl: true); // Ctrl+B
            SetDefaultBinding(HotkeyAction.OpenFusion, 0x46, ctrl: true); // Ctrl+F
            SetDefaultBinding(HotkeyAction.OpenMugen, 0x4D, ctrl: true); // Ctrl+M

            // Features
            SetDefaultBinding(HotkeyAction.TakeScreenshot, 0x2C); // PrintScreen
            SetDefaultBinding(HotkeyAction.StartRecording, 0x75, ctrl: true); // Ctrl+F6
            SetDefaultBinding(HotkeyAction.StopRecording, 0x76, ctrl: true); // Ctrl+F7
            SetDefaultBinding(HotkeyAction.QuickSave, 0x53, ctrl: true); // Ctrl+S
            SetDefaultBinding(HotkeyAction.QuickLoad, 0x4F, ctrl: true); // Ctrl+O
            SetDefaultBinding(HotkeyAction.ToggleFullscreen, 0x0D, alt: true); // Alt+Enter
            SetDefaultBinding(HotkeyAction.ToggleMute, 0x4D, ctrl: true, shift: true); // Ctrl+Shift+M

            // AI
            SetDefaultBinding(HotkeyAction.AskAi, 0x20, ctrl: true); // Ctrl+Space
            SetDefaultBinding(HotkeyAction.GenerateImage, 0x47, ctrl: true, shift: true); // Ctrl+Shift+G

            // Search
            SetDefaultBinding(HotkeyAction.GlobalSearch, 0x50, ctrl: true); // Ctrl+P
            SetDefaultBinding(HotkeyAction.SearchGames, 0x46, ctrl: true, shift: true); // Ctrl+Shift+F

            // Quick actions
            SetDefaultBinding(HotkeyAction.LaunchLastGame, 0x4C, ctrl: true, shift: true); // Ctrl+Shift+L
            SetDefaultBinding(HotkeyAction.RandomGame, 0x52, ctrl: true, shift: true); // Ctrl+Shift+R
        }

        private void SetDefaultBinding(HotkeyAction action, int keyCode, 
            bool ctrl = false, bool alt = false, bool shift = false, bool win = false)
        {
            if (!_bindings.ContainsKey(action))
            {
                _bindings[action] = new HotkeyBinding
                {
                    Action = action,
                    KeyCode = keyCode,
                    Ctrl = ctrl,
                    Alt = alt,
                    Shift = shift,
                    Win = win
                };
            }
        }

        public void RegisterHandler(HotkeyAction action, Action handler)
        {
            _handlers[action] = handler;
        }

        public void UnregisterHandler(HotkeyAction action)
        {
            _handlers.Remove(action);
        }

        public HotkeyBinding? GetBinding(HotkeyAction action)
        {
            return _bindings.GetValueOrDefault(action);
        }

        public List<HotkeyBinding> GetAllBindings()
        {
            return _bindings.Values.ToList();
        }

        public void SetBinding(HotkeyAction action, int keyCode, 
            bool ctrl = false, bool alt = false, bool shift = false, bool win = false)
        {
            _bindings[action] = new HotkeyBinding
            {
                Action = action,
                KeyCode = keyCode,
                Ctrl = ctrl,
                Alt = alt,
                Shift = shift,
                Win = win
            };
            SaveBindings();
        }

        public void ResetToDefaults()
        {
            _bindings.Clear();
            InitializeDefaultBindings();
            SaveBindings();
        }

        public void EnableBinding(HotkeyAction action, bool enabled)
        {
            if (_bindings.TryGetValue(action, out var binding))
            {
                binding.IsEnabled = enabled;
                SaveBindings();
            }
        }

        public void StartListening()
        {
            if (_isListening) return;
            _isListening = true;

            // In production: Register global hotkeys using platform APIs
            // Windows: RegisterHotKey API
            // Cross-platform: Consider using a library like SharpHook

            Console.WriteLine("⌨️ Hotkey listening started");
        }

        public void StopListening()
        {
            if (!_isListening) return;
            _isListening = false;

            // In production: Unregister global hotkeys
            Console.WriteLine("⌨️ Hotkey listening stopped");
        }

        // Called when a hotkey is detected (by platform-specific hook)
        public void OnHotkeyPressed(int keyCode, bool ctrl, bool alt, bool shift, bool win)
        {
            var binding = _bindings.Values.FirstOrDefault(b =>
                b.IsEnabled &&
                b.KeyCode == keyCode &&
                b.Ctrl == ctrl &&
                b.Alt == alt &&
                b.Shift == shift &&
                b.Win == win);

            if (binding != null)
            {
                ExecuteAction(binding.Action);
            }
        }

        private void ExecuteAction(HotkeyAction action)
        {
            Console.WriteLine($"⌨️ Hotkey: {action}");
            HotkeyTriggered?.Invoke(this, action);

            if (_handlers.TryGetValue(action, out var handler))
            {
                handler.Invoke();
            }
        }

        public string GetActionDescription(HotkeyAction action)
        {
            return action switch
            {
                HotkeyAction.OpenLibrary => "Open Game Library",
                HotkeyAction.OpenSettings => "Open Settings",
                HotkeyAction.OpenAiConfig => "Open AI Configuration",
                HotkeyAction.OpenBattle => "Open Battle Mode",
                HotkeyAction.OpenFusion => "Open Character Fusion",
                HotkeyAction.OpenMugen => "Open MUGEN Player",
                HotkeyAction.TakeScreenshot => "Take Screenshot",
                HotkeyAction.StartRecording => "Start Recording",
                HotkeyAction.StopRecording => "Stop Recording",
                HotkeyAction.QuickSave => "Quick Save",
                HotkeyAction.QuickLoad => "Quick Load",
                HotkeyAction.ToggleFullscreen => "Toggle Fullscreen",
                HotkeyAction.ToggleMute => "Toggle Mute",
                HotkeyAction.AskAi => "Ask AI Assistant",
                HotkeyAction.GenerateImage => "Generate AI Image",
                HotkeyAction.GlobalSearch => "Global Search",
                HotkeyAction.SearchGames => "Search Games",
                HotkeyAction.LaunchLastGame => "Launch Last Game",
                HotkeyAction.RandomGame => "Play Random Game",
                HotkeyAction.PauseGame => "Pause Game",
                _ => action.ToString()
            };
        }

        private void LoadBindings()
        {
            if (File.Exists(_configPath))
            {
                try
                {
                    var json = File.ReadAllText(_configPath);
                    var loaded = JsonSerializer.Deserialize<List<HotkeyBinding>>(json);
                    if (loaded != null)
                    {
                        foreach (var binding in loaded)
                        {
                            _bindings[binding.Action] = binding;
                        }
                    }
                }
                catch { }
            }
        }

        private void SaveBindings()
        {
            var dir = Path.GetDirectoryName(_configPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(_bindings.Values.ToList(), 
                new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_configPath, json);
        }

        public void Dispose()
        {
            StopListening();
        }
    }
}
