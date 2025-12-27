using System.Speech.Recognition;
using SaveState.Core.Interfaces;
using Serilog;

namespace SaveState.Core.Services;

public class VoiceService : IVoiceService, IDisposable
{
    private readonly ILogger _logger = Log.ForContext<VoiceService>();
    private SpeechRecognitionEngine? _recognizer;
    private bool _isListening;

    public event EventHandler<string>? SpeechRecognized;
    public event EventHandler<bool>? ListeningStateChanged;

    public bool IsListening => _isListening;

    public VoiceService()
    {
        InitializeRecognizer();
    }

    private void InitializeRecognizer()
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                _recognizer = new SpeechRecognitionEngine(System.Globalization.CultureInfo.CurrentCulture);
                _recognizer.LoadGrammar(new DictationGrammar());
                
                // Add some custom commands for better accuracy on common terms
                var commands = new Choices();
                commands.Add("Scan", "Hack", "Health", "Ammo", "Attach", "Next Scan", "Cheat", "Value", "Write", "Freeze");
                var cmdGrammar = new Grammar(new GrammarBuilder(commands));
                cmdGrammar.Name = "Commands";
                _recognizer.LoadGrammar(cmdGrammar);

                _recognizer.SpeechRecognized += Recognizer_SpeechRecognized;
                _recognizer.SetInputToDefaultAudioDevice();
            }
            else
            {
                _logger.Warning("Voice recognition is only supported on Windows.");
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to initialize speech recognition");
        }
    }

    private void Recognizer_SpeechRecognized(object? sender, SpeechRecognizedEventArgs e)
    {
        if (e.Result.Confidence > 0.6f)
        {
            SpeechRecognized?.Invoke(this, e.Result.Text);
        }
    }

    public void StartListening()
    {
        if (_recognizer == null || _isListening) return;

        try
        {
            _recognizer.RecognizeAsync(RecognizeMode.Multiple);
            _isListening = true;
            ListeningStateChanged?.Invoke(this, true);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to start listening");
        }
    }

    public void StopListening()
    {
        if (_recognizer == null || !_isListening) return;

        try
        {
            _recognizer.RecognizeAsyncStop();
            _isListening = false;
            ListeningStateChanged?.Invoke(this, false);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to stop listening");
        }
    }

    public void ToggleListening()
    {
        if (_isListening)
            StopListening();
        else
            StartListening();
    }

    public void Dispose()
    {
        if (_recognizer != null)
        {
            _recognizer.Dispose();
            _recognizer = null;
        }
    }
}
