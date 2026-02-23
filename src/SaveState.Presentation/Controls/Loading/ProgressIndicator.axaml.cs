using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using System.Globalization;

namespace SaveState.Presentation.Controls.Loading;

/// <summary>
/// A multi-step progress indicator with support for both continuous and step-based progress.
/// 
/// Usage:
/// <code>
/// &lt;local:ProgressIndicator 
///     CurrentStep="2" 
///     TotalSteps="5"
///     StepLabels="Download,Extract,Install,Configure,Launch"
///     Message="Installing game..." /&gt;
/// </code>
/// </summary>
public partial class ProgressIndicator : UserControl
{
    /// <summary>
    /// Defines the <see cref="CurrentStep"/> property.
    /// </summary>
    public static readonly StyledProperty<int> CurrentStepProperty =
        AvaloniaProperty.Register<ProgressIndicator, int>(
            nameof(CurrentStep),
            0);

    /// <summary>
    /// Defines the <see cref="TotalSteps"/> property.
    /// </summary>
    public static readonly StyledProperty<int> TotalStepsProperty =
        AvaloniaProperty.Register<ProgressIndicator, int>(
            nameof(TotalSteps),
            4);

    /// <summary>
    /// Defines the <see cref="ProgressPercentage"/> property.
    /// </summary>
    public static readonly StyledProperty<double> ProgressPercentageProperty =
        AvaloniaProperty.Register<ProgressIndicator, double>(
            nameof(ProgressPercentage),
            0.0);

    /// <summary>
    /// Defines the <see cref="Message"/> property.
    /// </summary>
    public static readonly StyledProperty<string?> MessageProperty =
        AvaloniaProperty.Register<ProgressIndicator, string?>(
            nameof(Message));

    /// <summary>
    /// Defines the <see cref="StepLabels"/> property.
    /// </summary>
    public static readonly StyledProperty<string?> StepLabelsProperty =
        AvaloniaProperty.Register<ProgressIndicator, string?>(
            nameof(StepLabels));

    /// <summary>
    /// Defines the <see cref="IsIndeterminate"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> IsIndeterminateProperty =
        AvaloniaProperty.Register<ProgressIndicator, bool>(
            nameof(IsIndeterminate),
            false);

    /// <summary>
    /// Defines the <see cref="ShowPercentage"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> ShowPercentageProperty =
        AvaloniaProperty.Register<ProgressIndicator, bool>(
            nameof(ShowPercentage),
            true);

    /// <summary>
    /// Defines the <see cref="ShowSteps"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> ShowStepsProperty =
        AvaloniaProperty.Register<ProgressIndicator, bool>(
            nameof(ShowSteps),
            true);

    /// <summary>
    /// Defines the <see cref="TimeRemaining"/> property.
    /// </summary>
    public static readonly StyledProperty<string?> TimeRemainingProperty =
        AvaloniaProperty.Register<ProgressIndicator, string?>(
            nameof(TimeRemaining));

    /// <summary>
    /// Defines the <see cref="ShowTimeRemaining"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> ShowTimeRemainingProperty =
        AvaloniaProperty.Register<ProgressIndicator, bool>(
            nameof(ShowTimeRemaining),
            false);

    /// <summary>
    /// Gets or sets the current step index (0-based).
    /// </summary>
    public int CurrentStep
    {
        get => GetValue(CurrentStepProperty);
        set => SetValue(CurrentStepProperty, value);
    }

    /// <summary>
    /// Gets or sets the total number of steps.
    /// </summary>
    public int TotalSteps
    {
        get => GetValue(TotalStepsProperty);
        set => SetValue(TotalStepsProperty, value);
    }

    /// <summary>
    /// Gets or sets the progress percentage (0-100).
    /// </summary>
    public double ProgressPercentage
    {
        get => GetValue(ProgressPercentageProperty);
        set => SetValue(ProgressPercentageProperty, value);
    }

    /// <summary>
    /// Gets or sets the progress message.
    /// </summary>
    public string? Message
    {
        get => GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    /// <summary>
    /// Gets or sets comma-separated step labels.
    /// </summary>
    public string? StepLabels
    {
        get => GetValue(StepLabelsProperty);
        set => SetValue(StepLabelsProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the progress is indeterminate.
    /// </summary>
    public bool IsIndeterminate
    {
        get => GetValue(IsIndeterminateProperty);
        set => SetValue(IsIndeterminateProperty, value);
    }

    /// <summary>
    /// Gets or sets whether to show the percentage.
    /// </summary>
    public bool ShowPercentage
    {
        get => GetValue(ShowPercentageProperty);
        set => SetValue(ShowPercentageProperty, value);
    }

    /// <summary>
    /// Gets or sets whether to show step indicators.
    /// </summary>
    public bool ShowSteps
    {
        get => GetValue(ShowStepsProperty);
        set => SetValue(ShowStepsProperty, value);
    }

    /// <summary>
    /// Gets or sets the time remaining text.
    /// </summary>
    public string? TimeRemaining
    {
        get => GetValue(TimeRemainingProperty);
        set => SetValue(TimeRemainingProperty, value);
    }

    /// <summary>
    /// Gets or sets whether to show time remaining.
    /// </summary>
    public bool ShowTimeRemaining
    {
        get => GetValue(ShowTimeRemainingProperty);
        set => SetValue(ShowTimeRemainingProperty, value);
    }

    public ProgressIndicator()
    {
        InitializeComponent();
        UpdateStepsDisplay();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == CurrentStepProperty ||
            change.Property == TotalStepsProperty ||
            change.Property == StepLabelsProperty)
        {
            UpdateStepsDisplay();
            UpdateProgressFromStep();
        }
        else if (change.Property == ProgressPercentageProperty)
        {
            UpdatePercentageDisplay();
        }
        else if (change.Property == IsIndeterminateProperty)
        {
            PseudoClasses.Set(":indeterminate", IsIndeterminate);
        }
    }

    private void UpdateStepsDisplay()
    {
        if (StepsDotsGrid is null || StepsLabelsGrid is null) return;

        StepsDotsGrid.Children.Clear();
        StepsDotsGrid.ColumnDefinitions.Clear();
        StepsLabelsGrid.Children.Clear();
        StepsLabelsGrid.ColumnDefinitions.Clear();

        var labels = StepLabels?.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim()).ToArray() ?? Array.Empty<string>();

        for (int i = 0; i < TotalSteps; i++)
        {
            StepsDotsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            StepsLabelsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

            // Create step dot
            var dot = new Ellipse
            {
                Classes = { "step-dot" },
                HorizontalAlignment = HorizontalAlignment.Center,
                RenderTransform = new ScaleTransform(1, 1)
            };

            if (i < CurrentStep)
            {
                dot.Classes.Add("completed");
            }
            else if (i == CurrentStep)
            {
                dot.Classes.Add("current");
            }

            Grid.SetColumn(dot, i);
            StepsDotsGrid.Children.Add(dot);

            // Create connecting line (except for last item)
            if (i < TotalSteps - 1)
            {
                var line = new Rectangle
                {
                    Height = 2,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(6, 0),
                    Fill = i < CurrentStep
                        ? new SolidColorBrush(Color.Parse("#4CAF50"))
                        : new SolidColorBrush(Color.Parse("#E0E0E0"))
                };
                Grid.SetColumn(line, i);
                Grid.SetColumnSpan(line, 2);
                StepsDotsGrid.Children.Add(line);
            }

            // Create label
            if (i < labels.Length)
            {
                var label = new TextBlock
                {
                    Text = labels[i],
                    Classes = { "step-label" },
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                if (i == CurrentStep)
                {
                    label.Classes.Add("active");
                }

                Grid.SetColumn(label, i);
                StepsLabelsGrid.Children.Add(label);
            }
        }
    }

    private void UpdateProgressFromStep()
    {
        if (TotalSteps > 0)
        {
            ProgressPercentage = ((double)CurrentStep / (TotalSteps - 1)) * 100;
        }
    }

    private void UpdatePercentageDisplay()
    {
        if (PercentageTextBlock is not null && ShowPercentage)
        {
            PercentageTextBlock.Text = $"{ProgressPercentage:F0}%";
        }
    }

    /// <summary>
    /// Moves to the next step.
    /// </summary>
    public void NextStep()
    {
        if (CurrentStep < TotalSteps - 1)
        {
            CurrentStep++;
        }
    }

    /// <summary>
    /// Moves to the previous step.
    /// </summary>
    public void PreviousStep()
    {
        if (CurrentStep > 0)
        {
            CurrentStep--;
        }
    }

    /// <summary>
    /// Resets to the first step.
    /// </summary>
    public void Reset()
    {
        CurrentStep = 0;
    }
}
