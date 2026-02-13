using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Core.Common.Services;
using SaveState.Presentation.Services;
using System;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.ViewModels.Shell;

public partial class ConflictsResolutionOverlayViewModel : ObservableObject
{
    private readonly IOverlayService _overlayService;
    private readonly INotificationService _notificationService;
    private readonly ITimeProvider _timeProvider;

    [ObservableProperty]
    private ObservableCollection<FileConflictViewModel> _conflicts = new();

    [ObservableProperty]
    private int _conflictCount = 0;

    public ConflictsResolutionOverlayViewModel(
        IOverlayService overlayService,
        INotificationService notificationService,
        ITimeProvider timeProvider)
    {
        _overlayService = overlayService;
        _notificationService = notificationService;
        _timeProvider = timeProvider;
        LoadConflicts();
    }

    private void LoadConflicts()
    {
        Conflicts.Clear();

        // Simulate some conflicts
        Conflicts.Add(new FileConflictViewModel(
            "game_save_001.sav",
            _timeProvider.Now.AddMinutes(-5),
            "2.3 MB",
            _timeProvider.Now.AddMinutes(-2),
            "2.4 MB",
            this));

        Conflicts.Add(new FileConflictViewModel(
            "settings.json",
            _timeProvider.Now.AddHours(-1),
            "12 KB",
            _timeProvider.Now.AddMinutes(-30),
            "15 KB",
            this));

        ConflictCount = Conflicts.Count;
    }

    [RelayCommand]
    private void ResolveAllWithLocal()
    {
        foreach (var conflict in Conflicts.ToList())
        {
            conflict.UseLocal();
        }
        _notificationService.ShowSuccess($"Resolved {ConflictCount} conflicts with local versions");
        Close();
    }

    [RelayCommand]
    private void ResolveAllWithRemote()
    {
        foreach (var conflict in Conflicts.ToList())
        {
            conflict.UseRemote();
        }
        _notificationService.ShowSuccess($"Resolved {ConflictCount} conflicts with remote versions");
        Close();
    }

    [RelayCommand]
    private void Close()
    {
        _overlayService.HideConflictsResolutionOverlay();
    }

    public void RemoveConflict(FileConflictViewModel conflict)
    {
        Conflicts.Remove(conflict);
        ConflictCount = Conflicts.Count;

        if (ConflictCount == 0)
        {
            _notificationService.ShowSuccess("All conflicts resolved!");
            Close();
        }
    }
}

public partial class FileConflictViewModel : ObservableObject
{
    private readonly ConflictsResolutionOverlayViewModel _parent;

    public FileConflictViewModel(
        string fileName,
        DateTime localModified,
        string localSize,
        DateTime remoteModified,
        string remoteSize,
        ConflictsResolutionOverlayViewModel parent)
    {
        FileName = fileName;
        LocalModified = localModified;
        LocalSize = localSize;
        RemoteModified = remoteModified;
        RemoteSize = remoteSize;
        _parent = parent;
    }

    public string FileName { get; }
    public DateTime LocalModified { get; }
    public string LocalSize { get; }
    public DateTime RemoteModified { get; }
    public string RemoteSize { get; }

    public string LocalModifiedText => LocalModified.ToString("g");
    public string RemoteModifiedText => RemoteModified.ToString("g");

    [RelayCommand]
    public void UseLocal()
    {
        _parent.RemoveConflict(this);
    }

    [RelayCommand]
    public void UseRemote()
    {
        _parent.RemoveConflict(this);
    }

    [RelayCommand]
    public void KeepBoth()
    {
        _parent.RemoveConflict(this);
    }
}
