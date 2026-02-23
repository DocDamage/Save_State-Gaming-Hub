using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using SaveState.Presentation.Models.Achievements;

namespace SaveState.Presentation.Controls.Achievements;

/// <summary>
/// A control that displays an achievement badge with rarity styling and hover effects.
/// </summary>
public partial class AchievementBadge : UserControl
{
    #region Styled Properties

    /// <summary>
    /// Defines the <see cref="BadgeUrl"/> property.
    /// </summary>
    public static readonly StyledProperty<string?> BadgeUrlProperty =
        AvaloniaProperty.Register<AchievementBadge, string?>(nameof(BadgeUrl));

    /// <summary>
    /// Defines the <see cref="Rarity"/> property.
    /// </summary>
    public static readonly StyledProperty<AchievementRarity> RarityProperty =
        AvaloniaProperty.Register<AchievementBadge, AchievementRarity>(nameof(Rarity), AchievementRarity.Common);

    /// <summary>
    /// Defines the <see cref="Points"/> property.
    /// </summary>
    public static readonly StyledProperty<int> PointsProperty =
        AvaloniaProperty.Register<AchievementBadge, int>(nameof(Points), 0);

    /// <summary>
    /// Defines the <see cref="IsUnlocked"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> IsUnlockedProperty =
        AvaloniaProperty.Register<AchievementBadge, bool>(nameof(IsUnlocked), false);

    /// <summary>
    /// Defines the <see cref="IsHardcore"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> IsHardcoreProperty =
        AvaloniaProperty.Register<AchievementBadge, bool>(nameof(IsHardcore), false);

    /// <summary>
    /// Defines the <see cref="Title"/> property.
    /// </summary>
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<AchievementBadge, string>(nameof(Title), string.Empty);

    /// <summary>
    /// Defines the <see cref="Description"/> property.
    /// </summary>
    public static readonly StyledProperty<string> DescriptionProperty =
        AvaloniaProperty.Register<AchievementBadge, string>(nameof(Description), string.Empty);

    /// <summary>
    /// Defines the <see cref="AchievementId"/> property.
    /// </summary>
    public static readonly StyledProperty<int> AchievementIdProperty =
        AvaloniaProperty.Register<AchievementBadge, int>(nameof(AchievementId), 0);

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the URL to the badge image.
    /// </summary>
    public string? BadgeUrl
    {
        get => GetValue(BadgeUrlProperty);
        set => SetValue(BadgeUrlProperty, value);
    }

    /// <summary>
    /// Gets or sets the rarity of the achievement.
    /// </summary>
    public AchievementRarity Rarity
    {
        get => GetValue(RarityProperty);
        set => SetValue(RarityProperty, value);
    }

    /// <summary>
    /// Gets or sets the points value of the achievement.
    /// </summary>
    public int Points
    {
        get => GetValue(PointsProperty);
        set => SetValue(PointsProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the achievement is unlocked.
    /// </summary>
    public bool IsUnlocked
    {
        get => GetValue(IsUnlockedProperty);
        set => SetValue(IsUnlockedProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the achievement was unlocked in hardcore mode.
    /// </summary>
    public bool IsHardcore
    {
        get => GetValue(IsHardcoreProperty);
        set => SetValue(IsHardcoreProperty, value);
    }

    /// <summary>
    /// Gets or sets the title of the achievement.
    /// </summary>
    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>
    /// Gets or sets the description of the achievement.
    /// </summary>
    public string Description
    {
        get => GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    /// <summary>
    /// Gets or sets the achievement ID.
    /// </summary>
    public int AchievementId
    {
        get => GetValue(AchievementIdProperty);
        set => SetValue(AchievementIdProperty, value);
    }

    /// <summary>
    /// Gets the tooltip text for the badge.
    /// </summary>
    public string TooltipText =>
        $"{Title}\n{Description}\n{Rarity} - {Points} Points{(IsHardcore ? " (Hardcore)" : "")}";

    #endregion

    #region Events

    /// <summary>
    /// Event raised when the badge is clicked.
    /// </summary>
    public event EventHandler<AchievementBadgeClickEventArgs>? BadgeClicked;

    #endregion

    /// <summary>
    /// Initializes a new instance of the AchievementBadge class.
    /// </summary>
    public AchievementBadge()
    {
        InitializeComponent();

        // Set up click handler
        PointerPressed += OnPointerPressed;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <summary>
    /// Handles pointer pressed events.
    /// </summary>
    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BadgeClicked?.Invoke(this, new AchievementBadgeClickEventArgs
            {
                AchievementId = AchievementId,
                Title = Title,
                IsUnlocked = IsUnlocked,
                Rarity = Rarity
            });
        }
    }

    /// <summary>
    /// Updates the pseudo-classes based on property values.
    /// </summary>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsUnlockedProperty)
        {
            UpdatePseudoClasses();
        }
        else if (change.Property == IsHardcoreProperty)
        {
            UpdatePseudoClasses();
        }
    }

    private void UpdatePseudoClasses()
    {
        if (IsUnlocked)
        {
            PseudoClasses.Set(":unlocked", true);
        }
        else
        {
            PseudoClasses.Set(":unlocked", false);
        }
    }
}

/// <summary>
/// Event args for achievement badge click events.
/// </summary>
public class AchievementBadgeClickEventArgs : EventArgs
{
    /// <summary>
    /// The achievement ID.
    /// </summary>
    public int AchievementId { get; set; }

    /// <summary>
    /// The achievement title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Whether the achievement is unlocked.
    /// </summary>
    public bool IsUnlocked { get; set; }

    /// <summary>
    /// The rarity of the achievement.
    /// </summary>
    public AchievementRarity Rarity { get; set; }
}
