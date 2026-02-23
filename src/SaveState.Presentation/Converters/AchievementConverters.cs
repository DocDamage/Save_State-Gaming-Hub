using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using SaveState.Presentation.Models.Achievements;

namespace SaveState.Presentation.Converters;

public class RarityToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not AchievementRarity rarity)
            return new SolidColorBrush(Colors.Gray);

        return rarity switch
        {
            AchievementRarity.Common => new SolidColorBrush(Color.Parse("#9E9E9E")),
            AchievementRarity.Uncommon => new SolidColorBrush(Color.Parse("#4CAF50")),
            AchievementRarity.Rare => new SolidColorBrush(Color.Parse("#2196F3")),
            AchievementRarity.Epic => new SolidColorBrush(Color.Parse("#9C27B0")),
            AchievementRarity.Legendary => new SolidColorBrush(Color.Parse("#FF9800")),
            _ => new SolidColorBrush(Colors.Gray)
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class RarityToIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not AchievementRarity rarity)
            return "🥉";

        return rarity switch
        {
            AchievementRarity.Common => "🥉",
            AchievementRarity.Uncommon => "🥈",
            AchievementRarity.Rare => "🥇",
            AchievementRarity.Epic => "💎",
            AchievementRarity.Legendary => "👑",
            _ => "🥉"
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class PointsToDisplayConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not int points)
            return "0";

        return points >= 1000 ? $"{points / 1000.0:F1}k" : points.ToString();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
