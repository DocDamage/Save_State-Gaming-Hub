using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using SaveState.Core.Interfaces;
using SaveState.Core.Services.Ai;
using Serilog;

namespace SaveState.Core.Services.Audio
{
    public enum TtsVoice
    {
        Male,
        Female,
        Robot,
        Dramatic,
        Whisper
    }

    public class TtsRequest
    {
        public string Text { get; set; } = string.Empty;
        public TtsVoice Voice { get; set; } = TtsVoice.Male;
        public float Speed { get; set; } = 1.0f;
        public float Pitch { get; set; } = 1.0f;
    }

    public class TtsService
    {
        private readonly ILogger _logger = Log.ForContext<TtsService>();
        private readonly IAppConfiguration _config;
        private readonly HttpClient _httpClient;
        private string _apiUrl;
        private bool _isAvailable;

        public bool IsAvailable => _isAvailable;

        public TtsService(IAppConfiguration config, HttpClient httpClient)
        {
            _config = config;
            _httpClient = httpClient;
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
            _apiUrl = _config.GetApiEndpoint("TTS", "http://localhost:5002");
        }

        public async Task<bool> CheckConnectionAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_apiUrl}/api/health");
                _isAvailable = response.IsSuccessStatusCode;
                return _isAvailable;
            }
            catch
            {
                _isAvailable = false;
                return false;
            }
        }

        public void SetApiUrl(string url)
        {
            _apiUrl = url.TrimEnd('/');
        }

        public async Task<byte[]?> SynthesizeAsync(TtsRequest request)
        {
            if (!_isAvailable && !await CheckConnectionAsync())
            {
                return null;
            }

            try
            {
                var payload = new
                {
                    text = request.Text,
                    voice = GetVoiceName(request.Voice),
                    speed = request.Speed,
                    pitch = request.Pitch
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_apiUrl}/api/tts", content);
                
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsByteArrayAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "TTS synthesis error");
            }

            return null;
        }

        public async Task<byte[]?> SpeakAsync(string text, TtsVoice voice = TtsVoice.Male)
        {
            return await SynthesizeAsync(new TtsRequest
            {
                Text = text,
                Voice = voice
            });
        }

        // Use LLM to generate commentary then speak it
        public async Task<byte[]?> GenerateAndSpeakAsync(string prompt, ILlmService llmService, TtsVoice voice = TtsVoice.Dramatic)
        {
            if (llmService == null || !llmService.IsAvailable)
                return null;

            var text = await llmService.CompleteAsync(prompt, "You are a dramatic announcer. Keep responses under 20 words.");
            
            if (string.IsNullOrEmpty(text))
                return null;

            return await SpeakAsync(text, voice);
        }

        private string GetVoiceName(TtsVoice voice)
        {
            return voice switch
            {
                TtsVoice.Male => "en-US-GuyNeural",
                TtsVoice.Female => "en-US-JennyNeural",
                TtsVoice.Robot => "en-US-AIGenerate1Neural",
                TtsVoice.Dramatic => "en-US-ChristopherNeural",
                TtsVoice.Whisper => "en-US-AriaNeural",
                _ => "en-US-GuyNeural"
            };
        }

        public List<string> GetAvailableVoices()
        {
            return new List<string>
            {
                "Male (Guy)",
                "Female (Jenny)",
                "Robot (AI)",
                "Dramatic (Christopher)",
                "Whisper (Aria)"
            };
        }

        public string GetConnectionInfo()
        {
            return _isAvailable 
                ? $"✅ Connected to TTS at {_apiUrl}" 
                : $"❌ TTS not available. Run a TTS server on {_apiUrl}";
        }
    }
}
