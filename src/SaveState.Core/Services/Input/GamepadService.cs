using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace SaveState.Core.Services.Input
{
    public enum GamepadButton
    {
        A, B, X, Y,
        LeftBumper, RightBumper,
        LeftTrigger, RightTrigger,
        LeftStick, RightStick,
        DPadUp, DPadDown, DPadLeft, DPadRight,
        Start, Select, Guide
    }

    public class GamepadState
    {
        public bool IsConnected { get; set; }
        public int Index { get; set; }
        public string Name { get; set; } = string.Empty;
        public Dictionary<GamepadButton, bool> Buttons { get; set; } = new();
        public float LeftStickX { get; set; }
        public float LeftStickY { get; set; }
        public float RightStickX { get; set; }
        public float RightStickY { get; set; }
        public float LeftTrigger { get; set; }
        public float RightTrigger { get; set; }
    }

    public class GamepadService : IDisposable
    {
        private static GamepadService? _instance;
        private readonly ILogger _logger = Log.ForContext<GamepadService>();
        private readonly List<GamepadState> _gamepads = new();
        private readonly Dictionary<GamepadButton, Action> _buttonMappings = new();
        private CancellationTokenSource? _pollCts;
        private bool _isPolling;

        public event EventHandler<(int index, GamepadButton button)>? ButtonPressed;
        public event EventHandler<(int index, GamepadButton button)>? ButtonReleased;
        public event EventHandler<int>? GamepadConnected;
        public event EventHandler<int>? GamepadDisconnected;

        public static GamepadService Instance => _instance ??= new GamepadService();
        public bool IsPolling => _isPolling;
        public IReadOnlyList<GamepadState> Gamepads => _gamepads;

        private GamepadService()
        {
            InitializeDefaultMappings();
        }

        private void InitializeDefaultMappings()
        {
            // Default UI navigation mappings
            _buttonMappings[GamepadButton.A] = () => _logger.Debug("Gamepad: Select/Confirm");
            _buttonMappings[GamepadButton.B] = () => _logger.Debug("Gamepad: Back/Cancel");
            _buttonMappings[GamepadButton.DPadUp] = () => _logger.Debug("Gamepad: Navigate Up");
            _buttonMappings[GamepadButton.DPadDown] = () => _logger.Debug("Gamepad: Navigate Down");
            _buttonMappings[GamepadButton.DPadLeft] = () => _logger.Debug("Gamepad: Navigate Left");
            _buttonMappings[GamepadButton.DPadRight] = () => _logger.Debug("Gamepad: Navigate Right");
            _buttonMappings[GamepadButton.Start] = () => _logger.Debug("Gamepad: Open Menu");
            _buttonMappings[GamepadButton.Guide] = () => _logger.Debug("Gamepad: Quick Access");
        }

        public void StartPolling(int intervalMs = 16)
        {
            if (_isPolling) return;

            _pollCts = new CancellationTokenSource();
            _isPolling = true;

            Task.Run(async () =>
            {
                while (!_pollCts.Token.IsCancellationRequested)
                {
                    PollGamepads();
                    await Task.Delay(intervalMs, _pollCts.Token);
                }
            }, _pollCts.Token);

            _logger.Debug("Gamepad polling started");
        }

        public void StopPolling()
        {
            _pollCts?.Cancel();
            _isPolling = false;
            _logger.Debug("Gamepad polling stopped");
        }

        private void PollGamepads()
        {
            // In production: Use XInput on Windows, or cross-platform gamepad library
            // This is a stub implementation

            // Simulate checking for up to 4 gamepads
            for (int i = 0; i < 4; i++)
            {
                var connected = CheckGamepadConnected(i);
                var existing = _gamepads.FirstOrDefault(g => g.Index == i);

                if (connected && existing == null)
                {
                    var state = new GamepadState
                    {
                        Index = i,
                        IsConnected = true,
                        Name = $"Controller {i + 1}"
                    };
                    _gamepads.Add(state);
                    GamepadConnected?.Invoke(this, i);
                }
                else if (!connected && existing != null)
                {
                    _gamepads.Remove(existing);
                    GamepadDisconnected?.Invoke(this, i);
                }
                else if (connected && existing != null)
                {
                    // Update state and check for button changes
                    var previousButtons = new Dictionary<GamepadButton, bool>(existing.Buttons);
                    UpdateGamepadState(existing);

                    // Fire events for button changes
                    foreach (var button in existing.Buttons.Keys)
                    {
                        var wasPressed = previousButtons.GetValueOrDefault(button);
                        var isPressed = existing.Buttons[button];

                        if (isPressed && !wasPressed)
                        {
                            ButtonPressed?.Invoke(this, (i, button));
                            ExecuteMapping(button);
                        }
                        else if (!isPressed && wasPressed)
                        {
                            ButtonReleased?.Invoke(this, (i, button));
                        }
                    }
                }
            }
        }

        private bool CheckGamepadConnected(int index)
        {
            // Stub: In production, check actual gamepad connection
            // XInput: XInputGetState would return ERROR_SUCCESS if connected
            return false;
        }

        private void UpdateGamepadState(GamepadState state)
        {
            // Stub: In production, read actual button/stick states
            // This would use XInput on Windows or SDL2 cross-platform
        }

        public void SetButtonMapping(GamepadButton button, Action action)
        {
            _buttonMappings[button] = action;
        }

        public void ClearButtonMapping(GamepadButton button)
        {
            _buttonMappings.Remove(button);
        }

        private void ExecuteMapping(GamepadButton button)
        {
            if (_buttonMappings.TryGetValue(button, out var action))
            {
                action.Invoke();
            }
        }

        public GamepadState? GetGamepad(int index)
        {
            return _gamepads.FirstOrDefault(g => g.Index == index);
        }

        public bool IsButtonPressed(int index, GamepadButton button)
        {
            var state = GetGamepad(index);
            return state?.Buttons.GetValueOrDefault(button) ?? false;
        }

        public (float x, float y) GetLeftStick(int index)
        {
            var state = GetGamepad(index);
            return state != null ? (state.LeftStickX, state.LeftStickY) : (0, 0);
        }

        public (float x, float y) GetRightStick(int index)
        {
            var state = GetGamepad(index);
            return state != null ? (state.RightStickX, state.RightStickY) : (0, 0);
        }

        public float GetLeftTrigger(int index)
        {
            return GetGamepad(index)?.LeftTrigger ?? 0;
        }

        public float GetRightTrigger(int index)
        {
            return GetGamepad(index)?.RightTrigger ?? 0;
        }

        public void Vibrate(int index, float leftMotor, float rightMotor, int durationMs = 200)
        {
            // In production: Use XInputSetState or equivalent
            _logger.Debug("Vibrate controller {Index}: L={LeftMotor:F2}, R={RightMotor:F2}", index, leftMotor, rightMotor);
        }

        public List<string> GetConnectedGamepadNames()
        {
            return _gamepads.Select(g => g.Name).ToList();
        }

        public int GetConnectedCount() => _gamepads.Count;

        public void Dispose()
        {
            StopPolling();
            _pollCts?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
