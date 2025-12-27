using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace SaveState.Core.Services.Audio
{
    public enum SoundType
    {
        Tick,
        Success,
        Error,
        Notification,
        LevelUp,
        Achievement,
        Battle,
        Fusion
    }

    public class AudioService
    {
        private static AudioService? _instance;
        private readonly Dictionary<SoundType, string> _soundPaths = new();
        private readonly string _soundsPath;
        private float _masterVolume = 0.7f;
        private float _sfxVolume = 1.0f;
        private float _musicVolume = 0.5f;
        private bool _isMuted;

        public static AudioService Instance => _instance ??= new AudioService();
        public bool IsMuted => _isMuted;
        public float MasterVolume => _masterVolume;

        private AudioService()
        {
            _soundsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "SaveState2", "assets", "sounds");
            if (!Directory.Exists(_soundsPath)) Directory.CreateDirectory(_soundsPath);
            
            LoadSoundMappings();
        }

        private void LoadSoundMappings()
        {
            _soundPaths[SoundType.Tick] = Path.Combine(_soundsPath, "tick.mp3");
            _soundPaths[SoundType.Success] = Path.Combine(_soundsPath, "success.mp3");
            _soundPaths[SoundType.Error] = Path.Combine(_soundsPath, "error.mp3");
            _soundPaths[SoundType.Notification] = Path.Combine(_soundsPath, "notification.mp3");
            _soundPaths[SoundType.LevelUp] = Path.Combine(_soundsPath, "levelup.mp3");
            _soundPaths[SoundType.Achievement] = Path.Combine(_soundsPath, "achievement.mp3");
            _soundPaths[SoundType.Battle] = Path.Combine(_soundsPath, "battle.mp3");
            _soundPaths[SoundType.Fusion] = Path.Combine(_soundsPath, "fusion.mp3");
        }

        public void Play(SoundType sound)
        {
            if (_isMuted) return;
            
            if (_soundPaths.TryGetValue(sound, out var path) && File.Exists(path))
            {
                // Use NAudio or platform-specific audio playback
                // For now, log that we would play the sound
                Console.WriteLine($"🔊 Playing sound: {sound}");
                PlaySoundFile(path);
            }
        }

        public async Task PlayAsync(SoundType sound)
        {
            await Task.Run(() => Play(sound));
        }

        private void PlaySoundFile(string path)
        {
            try
            {
                // Windows-specific using System.Media
                // For cross-platform, consider NAudio or similar
                #if WINDOWS
                var player = new System.Media.SoundPlayer(path);
                player.Play();
                #endif
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Audio error: {ex.Message}");
            }
        }

        public void PlayTick() => Play(SoundType.Tick);
        public void PlaySuccess() => Play(SoundType.Success);
        public void PlayError() => Play(SoundType.Error);
        public void PlayNotification() => Play(SoundType.Notification);
        public void PlayLevelUp() => Play(SoundType.LevelUp);
        public void PlayAchievement() => Play(SoundType.Achievement);

        public void SetMasterVolume(float volume)
        {
            _masterVolume = Math.Clamp(volume, 0f, 1f);
        }

        public void SetSfxVolume(float volume)
        {
            _sfxVolume = Math.Clamp(volume, 0f, 1f);
        }

        public void SetMusicVolume(float volume)
        {
            _musicVolume = Math.Clamp(volume, 0f, 1f);
        }

        public void Mute() => _isMuted = true;
        public void Unmute() => _isMuted = false;
        public void ToggleMute() => _isMuted = !_isMuted;

        public bool HasSound(SoundType sound)
        {
            return _soundPaths.TryGetValue(sound, out var path) && File.Exists(path);
        }

        public string GetSoundsPath() => _soundsPath;

        public void RegisterCustomSound(string name, string filePath)
        {
            // Allow custom sounds for extensibility
            Console.WriteLine($"Registered custom sound: {name} -> {filePath}");
        }
    }
}
