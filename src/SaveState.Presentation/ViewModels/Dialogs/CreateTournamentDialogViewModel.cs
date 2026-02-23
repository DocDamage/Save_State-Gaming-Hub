using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Core.Common.Services;
using SaveState.Core.TournamentManagement.Models;
using SaveState.Presentation.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;

namespace SaveState.Presentation.ViewModels.Dialogs;

/// <summary>
/// ViewModel for the create tournament dialog.
/// </summary>
public partial class CreateTournamentDialogViewModel : ObservableObject
{
    private readonly ITimeProvider _timeProvider;

    // Validation constants
    private const int MaxNameLength = 100;
    private const int MaxDescriptionLength = 2000;
    private static readonly Regex InvalidCharsPattern = new Regex(@"[<>\x00-\x08\x0B\x0C\x0E-\x1F]", RegexOptions.Compiled);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNameValid))]
    [NotifyPropertyChangedFor(nameof(CanCreate))]
    [NotifyPropertyChangedFor(nameof(ValidationError))]
    private string _name = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDescriptionValid))]
    [NotifyPropertyChangedFor(nameof(CanCreate))]
    private string _description = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsGameSelected))]
    [NotifyPropertyChangedFor(nameof(CanCreate))]
    private GameSelectionItem? _selectedGame;

    [ObservableProperty]
    private TournamentFormat _selectedFormat = TournamentFormat.SingleElimination;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsStartDateValid))]
    [NotifyPropertyChangedFor(nameof(CanCreate))]
    [NotifyPropertyChangedFor(nameof(ValidationError))]
    private DateTime _startDate;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsStartTimeValid))]
    private TimeSpan _startTime;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRegistrationDeadlineValid))]
    [NotifyPropertyChangedFor(nameof(CanCreate))]
    [NotifyPropertyChangedFor(nameof(ValidationError))]
    private DateTime _registrationDeadline;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMaxParticipantsValid))]
    [NotifyPropertyChangedFor(nameof(CanCreate))]
    private int _maxParticipants = 16;

    [ObservableProperty]
    private int _bestOf = 3;

    [ObservableProperty]
    private TimeSpan? _timeLimit;

    [ObservableProperty]
    private bool _allowDraws;

    [ObservableProperty]
    private int _maxRounds;

    [ObservableProperty]
    private bool _hasPrizePool;

    [ObservableProperty]
    private decimal _prizePoolAmount;

    [ObservableProperty]
    private string _streamUrl = string.Empty;

    [ObservableProperty]
    private string _validationError = string.Empty;

    [ObservableProperty]
    private ObservableCollection<GameSelectionItem> _availableGames = new();

    [ObservableProperty]
    private bool _isLoadingGames;

    /// <summary>
    /// Initializes a new instance with pre-populated games.
    /// </summary>
    public CreateTournamentDialogViewModel(
        ITimeProvider timeProvider,
        IEnumerable<GameSelectionItem>? availableGames = null)
    {
        _timeProvider = timeProvider;

        // Initialize dates
        StartDate = timeProvider.Today.AddDays(7);
        StartTime = new TimeSpan(19, 0, 0); // 7 PM default
        RegistrationDeadline = StartDate.AddDays(-1);

        // Load games if provided
        if (availableGames != null)
        {
            foreach (var game in availableGames.OrderBy(g => g.Name))
            {
                AvailableGames.Add(game);
            }
        }
        else
        {
            // Add some default games for demonstration
            AddDefaultGames();
        }
    }

    private void AddDefaultGames()
    {
        var defaultGames = new[]
        {
            new GameSelectionItem { Id = "game-1", Name = "Super Smash Bros. Ultimate" },
            new GameSelectionItem { Id = "game-2", Name = "Street Fighter 6" },
            new GameSelectionItem { Id = "game-3", Name = "Tekken 8" },
            new GameSelectionItem { Id = "game-4", Name = "Mortal Kombat 1" },
            new GameSelectionItem { Id = "game-5", Name = "Guilty Gear Strive" },
            new GameSelectionItem { Id = "game-6", Name = "The King of Fighters XV" },
            new GameSelectionItem { Id = "game-7", Name = "MUGEN" },
            new GameSelectionItem { Id = "game-8", Name = "IKEMEN GO" }
        };

        foreach (var game in defaultGames)
        {
            AvailableGames.Add(game);
        }
    }

    /// <summary>
    /// Parameterless constructor for design-time support.
    /// </summary>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public CreateTournamentDialogViewModel()
#pragma warning restore CS8618
    {
        // Design-time only
    }

    /// <summary>
    /// Available tournament formats.
    /// </summary>
    public IEnumerable<TournamentFormat> AvailableFormats => Enum.GetValues<TournamentFormat>();

    /// <summary>
    /// Available best-of options.
    /// </summary>
    public int[] BestOfOptions => new[] { 1, 3, 5, 7 };

    /// <summary>
    /// Gets whether the name is valid.
    /// </summary>
    public bool IsNameValid =>
        !string.IsNullOrWhiteSpace(Name) &&
        Name.Length <= MaxNameLength &&
        !InvalidCharsPattern.IsMatch(Name);

    /// <summary>
    /// Gets whether the description is valid.
    /// </summary>
    public bool IsDescriptionValid =>
        Description.Length <= MaxDescriptionLength &&
        !InvalidCharsPattern.IsMatch(Description);

    /// <summary>
    /// Gets whether a game is selected.
    /// </summary>
    public bool IsGameSelected => SelectedGame != null;

    /// <summary>
    /// Gets whether the start date is valid.
    /// </summary>
    public bool IsStartDateValid => StartDate.Date >= _timeProvider.Today;

    /// <summary>
    /// Gets whether the start time is valid.
    /// </summary>
    public bool IsStartTimeValid
    {
        get
        {
            var startDateTime = StartDate.Date + StartTime;
            return startDateTime > _timeProvider.Now;
        }
    }

    /// <summary>
    /// Gets whether the registration deadline is valid.
    /// </summary>
    public bool IsRegistrationDeadlineValid
    {
        get
        {
            var tournamentStart = StartDate.Date + StartTime;
            return RegistrationDeadline < tournamentStart && RegistrationDeadline > _timeProvider.Now;
        }
    }

    /// <summary>
    /// Gets whether max participants is valid.
    /// </summary>
    public bool IsMaxParticipantsValid => MaxParticipants >= 2 && MaxParticipants <= 512;

    /// <summary>
    /// Gets whether there are validation errors.
    /// </summary>
    public bool HasValidationErrors =>
        !IsNameValid || !IsDescriptionValid || !IsGameSelected ||
        !IsStartDateValid || !IsStartTimeValid || !IsRegistrationDeadlineValid ||
        !IsMaxParticipantsValid;

    /// <summary>
    /// Gets whether the create button should be enabled.
    /// </summary>
    public bool CanCreate =>
        !string.IsNullOrWhiteSpace(Name) &&
        IsGameSelected &&
        !HasValidationErrors;

    /// <summary>
    /// Gets the combined tournament start date/time.
    /// </summary>
    public DateTime TournamentStart => StartDate.Date + StartTime;



    partial void OnNameChanged(string value)
    {
        if (value?.Length > MaxNameLength)
        {
            Name = value[..MaxNameLength];
            return;
        }

        UpdateValidationError();
    }

    partial void OnDescriptionChanged(string value)
    {
        if (value?.Length > MaxDescriptionLength)
        {
            Description = value[..MaxDescriptionLength];
        }
    }

    partial void OnStartDateChanged(DateTime value)
    {
        UpdateValidationError();
    }

    partial void OnStartTimeChanged(TimeSpan value)
    {
        UpdateValidationError();
    }

    partial void OnRegistrationDeadlineChanged(DateTime value)
    {
        UpdateValidationError();
    }

    partial void OnMaxParticipantsChanged(int value)
    {
        // Ensure power of 2 for elimination brackets
        if (SelectedFormat is TournamentFormat.SingleElimination or TournamentFormat.DoubleElimination)
        {
            // Round to nearest power of 2
            var power = (int)Math.Round(Math.Log2(value));
            power = Math.Clamp(power, 1, 9); // 2 to 512
            var rounded = (int)Math.Pow(2, power);
            if (rounded != value)
            {
                MaxParticipants = rounded;
                return;
            }
        }
        UpdateValidationError();
    }

    partial void OnSelectedFormatChanged(TournamentFormat value)
    {
        // Adjust max participants based on format
        if (value is TournamentFormat.SingleElimination or TournamentFormat.DoubleElimination)
        {
            // Ensure power of 2
            var power = (int)Math.Round(Math.Log2(MaxParticipants));
            power = Math.Clamp(power, 1, 9);
            MaxParticipants = (int)Math.Pow(2, power);
        }
    }

    private void UpdateValidationError()
    {
        if (!IsNameValid)
        {
            if (string.IsNullOrWhiteSpace(Name))
                ValidationError = "Tournament name is required.";
            else if (Name.Length > MaxNameLength)
                ValidationError = $"Name must not exceed {MaxNameLength} characters.";
            else
                ValidationError = "Name contains invalid characters.";
        }
        else if (!IsStartDateValid)
        {
            ValidationError = "Start date cannot be in the past.";
        }
        else if (!IsStartTimeValid)
        {
            ValidationError = "Start time must be in the future.";
        }
        else if (!IsRegistrationDeadlineValid)
        {
            ValidationError = "Registration deadline must be before tournament start and in the future.";
        }
        else if (!IsMaxParticipantsValid)
        {
            ValidationError = "Participants must be between 2 and 512.";
        }
        else
        {
            ValidationError = string.Empty;
        }

        OnPropertyChanged(nameof(CanCreate));
    }

    [RelayCommand]
    private void Create()
    {
        if (!CanCreate) return;

        var prizePool = HasPrizePool && PrizePoolAmount > 0
            ? new PrizePool
            {
                TotalAmount = PrizePoolAmount,
                Currency = "USD",
                DistributionType = PrizeDistributionType.Standard
            }
            : null;

        var result = new CreateTournamentResult(
            Name: Name.Trim(),
            Description: Description.Trim(),
            GameId: SelectedGame!.Id,
            Format: SelectedFormat,
            RegistrationStart: _timeProvider.UtcNow,
            RegistrationEnd: RegistrationDeadline,
            TournamentStart: TournamentStart,
            MaxParticipants: MaxParticipants,
            Rules: new TournamentRules
            {
                BestOf = BestOf,
                MatchTimeLimit = TimeLimit,
                AllowDraws = AllowDraws,
                MaxRounds = MaxRounds
            },
            PrizePool: prizePool,
            StreamUrl: string.IsNullOrWhiteSpace(StreamUrl) ? null : StreamUrl.Trim());

        CloseDialog(result);
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseDialog(null);
    }

    private void CloseDialog(CreateTournamentResult? result)
    {
        var lifetime = Avalonia.Application.Current?.ApplicationLifetime;
        if (lifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = desktop.Windows.FirstOrDefault(w => w.DataContext == this);
            window?.Close(result);
        }
    }
}

/// <summary>
/// Game selection item for the dropdown.
/// </summary>
public class GameSelectionItem
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? CoverImage { get; set; }

    public override string ToString() => Name;
}
