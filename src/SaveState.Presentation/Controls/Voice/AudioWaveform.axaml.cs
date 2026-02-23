using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using SaveState.Presentation.Models.Voice;

namespace SaveState.Presentation.Controls.Voice;

/// <summary>
/// Custom control for visualizing audio waveforms.
/// </summary>
public partial class AudioWaveform : UserControl
{
    #region Styled Properties

    /// <summary>
    /// Defines the <see cref="AudioData"/> property.
    /// </summary>
    public static readonly StyledProperty<ObservableCollection<float>> AudioDataProperty =
        AvaloniaProperty.Register<AudioWaveform, ObservableCollection<float>>(
            nameof(AudioData),
            new ObservableCollection<float>());

    /// <summary>
    /// Defines the <see cref="AudioLevel"/> property.
    /// </summary>
    public static readonly StyledProperty<float> AudioLevelProperty =
        AvaloniaProperty.Register<AudioWaveform, float>(
            nameof(AudioLevel),
            0.0f);

    /// <summary>
    /// Defines the <see cref="State"/> property.
    /// </summary>
    public static readonly StyledProperty<VoiceVisualizerState> StateProperty =
        AvaloniaProperty.Register<AudioWaveform, VoiceVisualizerState>(
            nameof(State),
            VoiceVisualizerState.Idle);

    /// <summary>
    /// Defines the <see cref="ShowFrequencyBands"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> ShowFrequencyBandsProperty =
        AvaloniaProperty.Register<AudioWaveform, bool>(
            nameof(ShowFrequencyBands),
            true);

    /// <summary>
    /// Defines the <see cref="BarCount"/> property.
    /// </summary>
    public static readonly StyledProperty<int> BarCountProperty =
        AvaloniaProperty.Register<AudioWaveform, int>(
            nameof(BarCount),
            16);

    /// <summary>
    /// Defines the <see cref="BarColor"/> property.
    /// </summary>
    public static readonly StyledProperty<IBrush> BarColorProperty =
        AvaloniaProperty.Register<AudioWaveform, IBrush>(
            nameof(BarColor),
            new SolidColorBrush(Colors.DodgerBlue));

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the audio frequency data.
    /// </summary>
    public ObservableCollection<float> AudioData
    {
        get => GetValue(AudioDataProperty);
        set => SetValue(AudioDataProperty, value);
    }

    /// <summary>
    /// Gets or sets the overall audio level (0-100).
    /// </summary>
    public float AudioLevel
    {
        get => GetValue(AudioLevelProperty);
        set => SetValue(AudioLevelProperty, value);
    }

    /// <summary>
    /// Gets or sets the visualizer state.
    /// </summary>
    public VoiceVisualizerState State
    {
        get => GetValue(StateProperty);
        set => SetValue(StateProperty, value);
    }

    /// <summary>
    /// Gets or sets whether to show frequency bands.
    /// </summary>
    public bool ShowFrequencyBands
    {
        get => GetValue(ShowFrequencyBandsProperty);
        set => SetValue(ShowFrequencyBandsProperty, value);
    }

    /// <summary>
    /// Gets or sets the number of bars to display.
    /// </summary>
    public int BarCount
    {
        get => GetValue(BarCountProperty);
        set => SetValue(BarCountProperty, value);
    }

    /// <summary>
    /// Gets or sets the bar color.
    /// </summary>
    public IBrush BarColor
    {
        get => GetValue(BarColorProperty);
        set => SetValue(BarColorProperty, value);
    }

    #endregion

    /// <summary>
    /// Creates a new audio waveform control.
    /// </summary>
    public AudioWaveform()
    {
        InitializeComponent();
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == StateProperty)
        {
            OnStateChanged((VoiceVisualizerState)change.NewValue!);
        }
        else if (change.Property == AudioLevelProperty)
        {
            OnAudioLevelChanged((float)change.NewValue!);
        }
    }

    private void OnStateChanged(VoiceVisualizerState newState)
    {
        // Update bar color based on state
        BarColor = newState switch
        {
            VoiceVisualizerState.Idle => new SolidColorBrush(Colors.Gray),
            VoiceVisualizerState.Listening => new SolidColorBrush(Colors.DodgerBlue),
            VoiceVisualizerState.Processing => new SolidColorBrush(Colors.Gold),
            VoiceVisualizerState.Executing => new SolidColorBrush(Colors.Orange),
            VoiceVisualizerState.Success => new SolidColorBrush(Colors.Green),
            VoiceVisualizerState.Error => new SolidColorBrush(Colors.Red),
            VoiceVisualizerState.Muted => new SolidColorBrush(Colors.DimGray),
            _ => new SolidColorBrush(Colors.Gray)
        };
    }

    private void OnAudioLevelChanged(float level)
    {
        // Ensure we have enough data points
        while (AudioData.Count < BarCount)
        {
            AudioData.Add(0.1f);
        }

        // Update existing data with smooth transitions
        for (int i = 0; i < AudioData.Count && i < BarCount; i++)
        {
            // Create a wave pattern based on the audio level
            var wavePosition = i / (float)(BarCount - 1);
            var waveFactor = 1.0f - Math.Abs(wavePosition * 2 - 1) * 0.3f;

            // Add some randomness for natural movement
            var random = Random.Shared.NextDouble() * 0.3 + 0.7;

            var targetValue = (level / 100f) * waveFactor * (float)random;

            // Smooth transition
            var currentValue = AudioData[i];
            var smoothedValue = currentValue + (targetValue - currentValue) * 0.3f;

            AudioData[i] = Math.Clamp(smoothedValue, 0.05f, 1.0f);
        }
    }

    /// <summary>
    /// Updates the waveform with new audio data.
    /// </summary>
    public void UpdateWaveform(float[] frequencyBands, float overallLevel)
    {
        AudioLevel = overallLevel * 100;

        AudioData.Clear();
        foreach (var band in frequencyBands)
        {
            AudioData.Add(Math.Clamp(band, 0.05f, 1.0f));
        }
    }

    /// <summary>
    /// Generates random waveform data for testing/demo purposes.
    /// </summary>
    public void GenerateDemoData()
    {
        var random = Random.Shared;
        AudioData.Clear();

        for (int i = 0; i < BarCount; i++)
        {
            var value = (float)(random.NextDouble() * 0.8 + 0.2);
            AudioData.Add(value);
        }

        AudioLevel = (float)(random.NextDouble() * 50 + 50);
    }
}
