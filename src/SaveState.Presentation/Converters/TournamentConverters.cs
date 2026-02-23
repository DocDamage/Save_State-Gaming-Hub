using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using SaveState.Core.Esports.Models;

namespace SaveState.Presentation.Converters;

public class TournamentStatusToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not TournamentStatus status)
            return new SolidColorBrush(Colors.Gray);

        return status switch
        {
            TournamentStatus.Draft => new SolidColorBrush(Colors.Gray),
            TournamentStatus.RegistrationOpen => new SolidColorBrush(Color.Parse("#4CAF50")),
            TournamentStatus.RegistrationClosed => new SolidColorBrush(Color.Parse("#FF9800")),
            TournamentStatus.InProgress => new SolidColorBrush(Color.Parse("#2196F3")),
            TournamentStatus.Paused => new SolidColorBrush(Color.Parse("#FFC107")),
            TournamentStatus.Completed => new SolidColorBrush(Color.Parse("#9E9E9E")),
            TournamentStatus.Cancelled => new SolidColorBrush(Color.Parse("#F44336")),
            _ => new SolidColorBrush(Colors.Gray)
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class TournamentStatusToBadgeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not TournamentStatus status)
            return "Unknown";

        return status switch
        {
            TournamentStatus.Draft => "⚪ Draft",
            TournamentStatus.RegistrationOpen => "🟢 Registration Open",
            TournamentStatus.RegistrationClosed => "🟡 Registration Closed",
            TournamentStatus.InProgress => "🔵 In Progress",
            TournamentStatus.Paused => "⏸️ Paused",
            TournamentStatus.Completed => "⚫ Completed",
            TournamentStatus.Cancelled => "🔴 Cancelled",
            _ => "Unknown"
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class TournamentFormatToStringConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not TournamentFormat format)
            return "Unknown";

        return format switch
        {
            TournamentFormat.SingleElimination => "🏆 Single Elimination",
            TournamentFormat.DoubleElimination => "🏆 Double Elimination",
            TournamentFormat.RoundRobin => "🔄 Round Robin",
            TournamentFormat.Swiss => "🎯 Swiss System",
            TournamentFormat.BattleRoyale => "👑 Battle Royale",
            TournamentFormat.League => "📊 League",
            _ => "Unknown"
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class TournamentFormatToIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not TournamentFormat format)
            return "❓";

        return format switch
        {
            TournamentFormat.SingleElimination => "🏆",
            TournamentFormat.DoubleElimination => "🏆",
            TournamentFormat.RoundRobin => "🔄",
            TournamentFormat.Swiss => "🎯",
            TournamentFormat.BattleRoyale => "👑",
            TournamentFormat.League => "📊",
            _ => "❓"
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class ParticipantStatusToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not ParticipantStatus status)
            return new SolidColorBrush(Colors.Gray);

        return status switch
        {
            ParticipantStatus.Registered => new SolidColorBrush(Color.Parse("#9E9E9E")),
            ParticipantStatus.CheckedIn => new SolidColorBrush(Color.Parse("#4CAF50")),
            ParticipantStatus.Competing => new SolidColorBrush(Color.Parse("#2196F3")),
            ParticipantStatus.Eliminated => new SolidColorBrush(Color.Parse("#F44336")),
            ParticipantStatus.Disqualified => new SolidColorBrush(Color.Parse("#9C27B0")),
            ParticipantStatus.Withdrawn => new SolidColorBrush(Color.Parse("#757575")),
            _ => new SolidColorBrush(Colors.Gray)
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class ParticipantStatusToBadgeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not ParticipantStatus status)
            return "Unknown";

        return status switch
        {
            ParticipantStatus.Registered => "⚪ Registered",
            ParticipantStatus.CheckedIn => "🟢 Checked In",
            ParticipantStatus.Competing => "🔵 Competing",
            ParticipantStatus.Eliminated => "🔴 Eliminated",
            ParticipantStatus.Disqualified => "🟣 Disqualified",
            ParticipantStatus.Withdrawn => "⚫ Withdrawn",
            _ => "Unknown"
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class MatchStatusToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not MatchStatus status)
            return new SolidColorBrush(Colors.Gray);

        return status switch
        {
            MatchStatus.Scheduled => new SolidColorBrush(Color.Parse("#9E9E9E")),
            MatchStatus.InProgress => new SolidColorBrush(Color.Parse("#2196F3")),
            MatchStatus.Completed => new SolidColorBrush(Color.Parse("#4CAF50")),
            MatchStatus.Disputed => new SolidColorBrush(Color.Parse("#F44336")),
            MatchStatus.Forfeited => new SolidColorBrush(Color.Parse("#FF9800")),
            MatchStatus.Cancelled => new SolidColorBrush(Color.Parse("#757575")),
            _ => new SolidColorBrush(Colors.Gray)
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class MatchStatusToBadgeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not MatchStatus status)
            return "Unknown";

        return status switch
        {
            MatchStatus.Scheduled => "⏰ Scheduled",
            MatchStatus.InProgress => "▶️ In Progress",
            MatchStatus.Completed => "✅ Completed",
            MatchStatus.Disputed => "⚠️ Disputed",
            MatchStatus.Forfeited => "🏳️ Forfeited",
            MatchStatus.Cancelled => "❌ Cancelled",
            _ => "Unknown"
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class BracketTypeToStringConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not BracketType type)
            return "Unknown";

        return type switch
        {
            BracketType.Winners => "Winners Bracket",
            BracketType.Losers => "Losers Bracket",
            BracketType.GrandFinals => "Grand Finals",
            _ => "Unknown"
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class TournamentDateDisplayConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 2 || values[0] is not DateTime startDate)
            return "Unknown";

        var endDate = values[1] as DateTime?;
        var now = DateTime.Now;

        if (startDate > now)
        {
            var timeUntil = startDate - now;
            if (timeUntil.TotalDays > 1)
                return $"Starts in {timeUntil.Days} days";
            if (timeUntil.TotalHours > 1)
                return $"Starts in {timeUntil.Hours} hours";
            return "Starts soon";
        }

        if (endDate.HasValue && endDate.Value < now)
            return "Completed";

        return "In Progress";
    }
}

public class ParticipantCountDisplayConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 2 || values[0] is not int registered || values[1] is not int max)
            return "0 / ?";

        var percentage = (double)registered / max * 100;
        var color = percentage switch
        {
            >= 100 => "🔴",
            >= 80 => "🟡",
            _ => "🟢"
        };

        return $"{color} {registered} / {max}";
    }
}

public class ResultTypeToStringConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not MatchResultType type)
            return "";

        return type switch
        {
            MatchResultType.Normal => "",
            MatchResultType.Forfeit => "(W/O)",
            MatchResultType.Disqualification => "(DQ)",
            MatchResultType.Draw => "(Draw)",
            _ => ""
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class RoundNameConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not int round)
            return $"Round {value}";

        return round switch
        {
            1 => "Round 1",
            2 => "Round 2",
            3 => "Round 3",
            4 => "Round 4",
            5 => "Quarterfinals",
            6 => "Semifinals",
            7 => "Finals",
            _ => $"Round {round}"
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class PlacementSuffixConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not int placement)
            return $"{value}th";

        var lastDigit = placement % 10;
        var lastTwoDigits = placement % 100;

        if (lastTwoDigits is >= 11 and <= 13)
            return $"{placement}th";

        return lastDigit switch
        {
            1 => $"{placement}st",
            2 => $"{placement}nd",
            3 => $"{placement}rd",
            _ => $"{placement}th"
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class PrizePoolDisplayConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not decimal amount)
            return "$0";

        return amount switch
        {
            >= 1_000_000 => $"${amount / 1_000_000:F1}M",
            >= 1_000 => $"${amount / 1_000:F1}K",
            _ => $"${amount:N0}"
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
