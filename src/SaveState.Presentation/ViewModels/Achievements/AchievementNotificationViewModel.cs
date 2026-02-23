using CommunityToolkit.Mvvm.ComponentModel;
using SaveState.Presentation.Models.Achievements;

namespace SaveState.Presentation.ViewModels.Achievements;

/// <summary>
/// ViewModel for a single achievement notification item.
/// </summary>
public partial class AchievementNotificationViewModel : ObservableObject
{
    /// <summary>
    /// The underlying achievement notification model.
    /// </summary>
    [ObservableProperty]
    private AchievementNotification _notification;

    /// <summary>
    /// The opacity of the notification for fade animations.
    /// </summary>
    [ObservableProperty]
    private double _opacity = 1.0;

    /// <summary>
    /// Display name for the achievement rarity.
    /// </summary>
    public string RarityDisplayName => Notification.Rarity switch
    {
        AchievementRarity.Common => "Common",
        AchievementRarity.Uncommon => "Uncommon",
        AchievementRarity.Rare => "Rare",
        AchievementRarity.Epic => "Epic",
        AchievementRarity.Legendary => "Legendary",
        _ => "Unknown"
    };

    /// <summary>
    /// The badge URL for the achievement icon.
    /// </summary>
    public string? BadgeUrl => Notification.BadgeUrl;

    /// <summary>
    /// Creates a new achievement notification view model.
    /// </summary>
    /// <param name="notification">The achievement notification to wrap.</param>
    public AchievementNotificationViewModel(AchievementNotification notification)
    {
        _notification = notification;
    }
}
