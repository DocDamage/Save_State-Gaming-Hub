using CommunityToolkit.Mvvm.ComponentModel;
using SaveState.Presentation.Services;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.ViewModels.Shell;

/// <summary>
/// View model for the overlay container.
/// </summary>
public partial class OverlayContainerViewModel : ObservableObject
{
    private readonly IOverlayService _overlayService;

    private bool _isLoading;
    private string _loadingMessage = "Loading...";

    public OverlayContainerViewModel(IOverlayService overlayService)
    {
        _overlayService = overlayService;

        // Subscribe to overlay service events
        _overlayService.OverlayChanged += OnOverlayChanged;

        // Initialize child view models
        CommandPaletteViewModel = new CommandPaletteViewModel(_overlayService);
        QuickSearchViewModel = new QuickSearchViewModel(_overlayService);
        AiAssistantViewModel = new AiAssistantViewModel(_overlayService);
        PerformanceHudViewModel = new PerformanceHudViewModel(_overlayService);
        VoiceIndicatorViewModel = new VoiceIndicatorViewModel();

        // Initialize toasts collection
        Toasts = new ObservableCollection<ToastViewModel>();
    }

    /// <summary>
    /// Gets whether the command palette is visible.
    /// </summary>
    public bool ShowCommandPalette => _overlayService.ShowCommandPalette;

    /// <summary>
    /// Gets whether the quick search is visible.
    /// </summary>
    public bool ShowQuickSearch => _overlayService.ShowQuickSearch;

    /// <summary>
    /// Gets whether the AI assistant is visible.
    /// </summary>
    public bool ShowAiAssistant => _overlayService.ShowAiAssistant;

    /// <summary>
    /// Gets whether the performance HUD is visible.
    /// </summary>
    public bool ShowPerformanceHud => _overlayService.ShowPerformanceHud;

    /// <summary>
    /// Gets whether the voice indicator is active.
    /// </summary>
    public bool IsVoiceActive => _overlayService.IsVoiceActive;

    /// <summary>
    /// Gets whether the dimming overlay should be shown.
    /// </summary>
    public bool ShowDim => _overlayService.ShowDim;

    /// <summary>
    /// Gets whether there are any toasts to display.
    /// </summary>
    public bool HasToasts => Toasts.Count > 0;

    /// <summary>
    /// Gets whether a loading indicator should be shown.
    /// </summary>
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    /// <summary>
    /// Gets the loading message.
    /// </summary>
    public string LoadingMessage
    {
        get => _loadingMessage;
        set => SetProperty(ref _loadingMessage, value);
    }

    /// <summary>
    /// Gets the command palette view model.
    /// </summary>
    public CommandPaletteViewModel CommandPaletteViewModel { get; }

    /// <summary>
    /// Gets the quick search view model.
    /// </summary>
    public QuickSearchViewModel QuickSearchViewModel { get; }

    /// <summary>
    /// Gets the AI assistant view model.
    /// </summary>
    public AiAssistantViewModel AiAssistantViewModel { get; }

    /// <summary>
    /// Gets the performance HUD view model.
    /// </summary>
    public PerformanceHudViewModel PerformanceHudViewModel { get; }

    /// <summary>
    /// Gets the voice indicator view model.
    /// </summary>
    public VoiceIndicatorViewModel VoiceIndicatorViewModel { get; }

    /// <summary>
    /// Gets the collection of toast notifications.
    /// </summary>
    public ObservableCollection<ToastViewModel> Toasts { get; }

    /// <summary>
    /// Shows a loading indicator with the specified message.
    /// </summary>
    /// <param name="message">The loading message.</param>
    public void ShowLoading(string message = "Loading...")
    {
        LoadingMessage = message;
        IsLoading = true;
    }

    /// <summary>
    /// Hides the loading indicator.
    /// </summary>
    public void HideLoading()
    {
        IsLoading = false;
    }

    /// <summary>
    /// Adds a toast notification.
    /// </summary>
    /// <param name="title">The toast title.</param>
    /// <param name="message">The toast message.</param>
    /// <param name="duration">How long to show the toast (in seconds).</param>
    public void AddToast(string title, string message, int duration = 5)
    {
        var toast = new ToastViewModel(title, message, duration, this);
        Toasts.Add(toast);
        OnPropertyChanged(nameof(HasToasts));
    }

    /// <summary>
    /// Removes a toast notification.
    /// </summary>
    /// <param name="toast">The toast to remove.</param>
    public void RemoveToast(ToastViewModel toast)
    {
        Toasts.Remove(toast);
        OnPropertyChanged(nameof(HasToasts));
    }

    /// <summary>
    /// Closes modal overlays (those that dim the background).
    /// </summary>
    public void CloseModalOverlays()
    {
        _overlayService.HideCommandPaletteOverlay();
        _overlayService.HideQuickSearchOverlay();
        // Note: AI Assistant is not modal, so it stays open
    }

    private void OnOverlayChanged(object? sender, OverlayChangedEventArgs e)
    {
        // Notify property changes for overlay visibility
        switch (e.OverlayName)
        {
            case "CommandPalette":
                OnPropertyChanged(nameof(ShowCommandPalette));
                OnPropertyChanged(nameof(ShowDim));
                break;
            case "QuickSearch":
                OnPropertyChanged(nameof(ShowQuickSearch));
                OnPropertyChanged(nameof(ShowDim));
                break;
            case "AiAssistant":
                OnPropertyChanged(nameof(ShowAiAssistant));
                break;
            case "PerformanceHud":
                OnPropertyChanged(nameof(ShowPerformanceHud));
                break;
            case "VoiceIndicator":
                OnPropertyChanged(nameof(IsVoiceActive));
                break;
        }
    }
}

/// <summary>
/// View model for toast notifications.
/// </summary>
public class ToastViewModel : ObservableObject
{
    private readonly OverlayContainerViewModel _parent;

    public ToastViewModel(string title, string message, int durationSeconds, OverlayContainerViewModel parent)
    {
        _parent = parent;
        Title = title;
        Message = message;
        Timestamp = DateTime.Now;

        // Auto-remove after duration
        Task.Delay(TimeSpan.FromSeconds(durationSeconds)).ContinueWith(_ =>
        {
            Avalonia.Threading.Dispatcher.UIThread.Invoke(() => Close());
        });
    }

    /// <summary>
    /// Gets the toast title.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Gets the toast message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets the timestamp when the toast was created.
    /// </summary>
    public DateTime Timestamp { get; }

    /// <summary>
    /// Command to close the toast.
    /// </summary>
    public CommunityToolkit.Mvvm.Input.RelayCommand CloseCommand => new(Close);

    private void Close()
    {
        _parent.RemoveToast(this);
    }
}