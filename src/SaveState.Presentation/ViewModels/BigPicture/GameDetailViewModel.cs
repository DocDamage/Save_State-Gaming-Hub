using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Core.Common.Services;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.ViewModels.BigPicture;

public partial class GameDetailViewModel : ObservableObject
{
    [ObservableProperty]
    private GameItemViewModel? selectedGame;

    [ObservableProperty]
    private ObservableCollection<ActivityViewModel> recentActivity = new();

    private readonly ITimeProvider _timeProvider;

    public GameDetailViewModel(ITimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
        LoadSampleActivity();
    }

    private void LoadSampleActivity()
    {
        RecentActivity.Add(new ActivityViewModel
        {
            Timestamp = _timeProvider.Now.AddDays(-1),
            Description = "Played for 2 hours"
        });

        RecentActivity.Add(new ActivityViewModel
        {
            Timestamp = _timeProvider.Now.AddDays(-3),
            Description = "Completed Chapter 5"
        });

        RecentActivity.Add(new ActivityViewModel
        {
            Timestamp = _timeProvider.Now.AddDays(-7),
            Description = "Started new game"
        });
    }

    [RelayCommand]
    private void LaunchGame()
    {
        if (SelectedGame != null)
        {
            // In real implementation, launch the game
            SelectedGame.LastPlayed = _timeProvider.Now;
            RecentActivity.Insert(0, new ActivityViewModel
            {
                Timestamp = _timeProvider.Now,
                Description = "Launched game"
            });
        }
    }

    [RelayCommand]
    private void AddToBacklog()
    {
        if (SelectedGame != null)
        {
            // In real implementation, add to backlog
            RecentActivity.Insert(0, new ActivityViewModel
            {
                Timestamp = _timeProvider.Now,
                Description = "Added to backlog"
            });
        }
    }

    [RelayCommand]
    private void ViewReviews()
    {
        // Navigate to reviews view
    }
}

public class ActivityViewModel
{
    public DateTime Timestamp { get; set; }
    public string Description { get; set; } = "";
}