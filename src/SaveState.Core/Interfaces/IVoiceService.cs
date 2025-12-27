namespace SaveState.Core.Interfaces;

public interface IVoiceService
{
    event EventHandler<string> SpeechRecognized;
    event EventHandler<bool> ListeningStateChanged;
    
    bool IsListening { get; }
    void StartListening();
    void StopListening();
    void ToggleListening();
}
