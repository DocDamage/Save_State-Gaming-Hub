using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Core.Services.EmulatorEnhancements;
using System.Collections.ObjectModel;

namespace SaveState.UI.ViewModels;

public partial class TimeCapsuleViewModel : ViewModelBase
{
    private readonly TimeCapsuleService _capsuleService;

    [ObservableProperty]
    private ObservableCollection<TimeCapsule> _allCapsules = new();

    [ObservableProperty]
    private TimeCapsule? _selectedCapsule;

    [ObservableProperty]
    private string _newTitle = string.Empty;

    [ObservableProperty]
    private string _newDescription = string.Empty;

    [ObservableProperty]
    private string _creatorName = "Player";

    [ObservableProperty]
    private int _unlockDelayHours = 24;

    [ObservableProperty]
    private string? _challengeType;

    [ObservableProperty]
    private string _newComment = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public string[] ChallengeTypes { get; } = { null!, "speedrun", "no-damage", "collectibles" };

    public IRelayCommand CreateCapsuleCommand { get; }
    public IRelayCommand<string> TryUnlockCommand { get; }
    public IRelayCommand<string> AddReactionCommand { get; }
    public IRelayCommand AddCommentCommand { get; }
    public IRelayCommand<string> DeleteCapsuleCommand { get; }
    public IRelayCommand RefreshCommand { get; }

    public TimeCapsuleViewModel()
    {
        _capsuleService = new TimeCapsuleService();

        CreateCapsuleCommand = new RelayCommand(CreateCapsule, CanCreateCapsule);
        TryUnlockCommand = new RelayCommand<string>(TryUnlock);
        AddReactionCommand = new RelayCommand<string>(AddReaction);
        AddCommentCommand = new RelayCommand(AddComment, () => SelectedCapsule != null && !string.IsNullOrWhiteSpace(NewComment));
        DeleteCapsuleCommand = new RelayCommand<string>(DeleteCapsule);
        RefreshCommand = new RelayCommand(RefreshCapsules);

        RefreshCapsules();
    }

    private bool CanCreateCapsule() => !string.IsNullOrWhiteSpace(NewTitle);

    private void CreateCapsule()
    {
        var capsule = _capsuleService.CreateCapsule(
            "demo-game",
            NewTitle,
            NewDescription,
            CreatorName,
            new byte[] { 0x00, 0x01, 0x02 }, // Demo save data
            TimeSpan.FromHours(UnlockDelayHours),
            ChallengeType
        );

        AllCapsules.Insert(0, capsule);
        NewTitle = string.Empty;
        NewDescription = string.Empty;
        StatusMessage = $"Created capsule: {capsule.Title} (unlocks in {UnlockDelayHours}h)";
    }

    private void TryUnlock(string? id)
    {
        if (string.IsNullOrEmpty(id)) return;
        
        if (_capsuleService.TryUnlock(id))
        {
            RefreshCapsules();
            StatusMessage = "Capsule unlocked!";
        }
        else
        {
            var remaining = _capsuleService.GetTimeUntilUnlock(id);
            StatusMessage = remaining.HasValue 
                ? $"Cannot unlock yet. {remaining.Value.Hours}h {remaining.Value.Minutes}m remaining" 
                : "Already unlocked";
        }
    }

    private void AddReaction(string? emoji)
    {
        if (SelectedCapsule == null || string.IsNullOrEmpty(emoji)) return;
        _capsuleService.AddReaction(SelectedCapsule.Id, "current-user", emoji);
        RefreshCapsules();
        StatusMessage = $"Added reaction: {emoji}";
    }

    private void AddComment()
    {
        if (SelectedCapsule == null || string.IsNullOrWhiteSpace(NewComment)) return;
        _capsuleService.AddComment(SelectedCapsule.Id, NewComment);
        NewComment = string.Empty;
        RefreshCapsules();
        StatusMessage = "Comment added";
    }

    private void DeleteCapsule(string? id)
    {
        if (string.IsNullOrEmpty(id)) return;
        _capsuleService.DeleteCapsule(id);
        var capsule = AllCapsules.FirstOrDefault(c => c.Id == id);
        if (capsule != null) AllCapsules.Remove(capsule);
        StatusMessage = "Capsule deleted";
    }

    private void RefreshCapsules()
    {
        AllCapsules.Clear();
        foreach (var capsule in _capsuleService.GetAllCapsules().OrderByDescending(c => c.CreatedAt))
        {
            AllCapsules.Add(capsule);
        }
        StatusMessage = $"Loaded {AllCapsules.Count} capsules";
    }

    partial void OnNewTitleChanged(string value) => CreateCapsuleCommand.NotifyCanExecuteChanged();
    partial void OnNewCommentChanged(string value) => AddCommentCommand.NotifyCanExecuteChanged();
    partial void OnSelectedCapsuleChanged(TimeCapsule? value) => AddCommentCommand.NotifyCanExecuteChanged();
}
