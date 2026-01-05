using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Presentation.Services;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.ViewModels.Shell;

public partial class CreateCollectionDialogViewModel : ObservableObject
{
    private readonly IOverlayService _overlayService;
    private readonly INotificationService _notificationService;

    [ObservableProperty]
    private string _collectionName = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string _selectedIcon = "📁";

    [ObservableProperty]
    private string _selectedColor = "#4A90E2";

    [ObservableProperty]
    private bool _isSmartCollection = false;

    [ObservableProperty]
    private ObservableCollection<string> _availableIcons = new()
    {
        "📁", "🎮", "🏆", "⭐", "🔥", "💎", "🎯", "🎨",
        "🎪", "🎭", "🎬", "🎵", "🎲", "🎰", "🎳", "🎺"
    };

    [ObservableProperty]
    private ObservableCollection<string> _availableColors = new()
    {
        "#4A90E2", "#E74C3C", "#2ECC71", "#F39C12",
        "#9B59B6", "#1ABC9C", "#34495E", "#E67E22"
    };

    [ObservableProperty]
    private ObservableCollection<CollectionRuleViewModel> _smartRules = new();

    public CreateCollectionDialogViewModel(
        IOverlayService overlayService,
        INotificationService notificationService)
    {
        _overlayService = overlayService;
        _notificationService = notificationService;
    }

    partial void OnIsSmartCollectionChanged(bool value)
    {
        if (value && SmartRules.Count == 0)
        {
            AddRule();
        }
    }

    [RelayCommand]
    private void AddRule()
    {
        SmartRules.Add(new CollectionRuleViewModel(
            "Platform",
            "equals",
            "PlayStation 2",
            this));
    }

    [RelayCommand]
    private void Create()
    {
        if (string.IsNullOrWhiteSpace(CollectionName))
        {
            _notificationService.ShowWarning("Please enter a collection name", "Create Collection");
            return;
        }

        var collectionType = IsSmartCollection ? "Smart Collection" : "Collection";
        _notificationService.ShowSuccess($"{collectionType} '{CollectionName}' created successfully", "Collections");
        Close();
    }

    [RelayCommand]
    private void Close()
    {
        _overlayService.HideCreateCollectionDialog();
    }

    public void RemoveRule(CollectionRuleViewModel rule)
    {
        SmartRules.Remove(rule);
    }
}

public partial class CollectionRuleViewModel : ObservableObject
{
    private readonly CreateCollectionDialogViewModel _parent;

    public CollectionRuleViewModel(
        string field,
        string operatorType,
        string value,
        CreateCollectionDialogViewModel parent)
    {
        Field = field;
        OperatorType = operatorType;
        Value = value;
        _parent = parent;
    }

    [ObservableProperty]
    private string field;

    [ObservableProperty]
    private string operatorType;

    [ObservableProperty]
    private string value;

    public ObservableCollection<string> AvailableFields { get; } = new()
    {
        "Platform", "Genre", "Developer", "Publisher",
        "Release Year", "Rating", "Play Time", "Last Played"
    };

    public ObservableCollection<string> AvailableOperators { get; } = new()
    {
        "equals", "not equals", "contains", "greater than", "less than"
    };

    [RelayCommand]
    private void Remove()
    {
        _parent.RemoveRule(this);
    }
}
