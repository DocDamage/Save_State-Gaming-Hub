using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Core.Analytics.Models.GamerProfile;
using SaveState.Core.Analytics.Services;
using SaveState.Presentation.Services;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.ViewModels.Dialogs;

/// <summary>
/// ViewModel for the Gamer Type Quiz dialog.
/// Guides users through questions to determine their gamer archetype.
/// </summary>
public partial class GamerTypeQuizViewModel : ObservableObject, IDialogViewModel<GamerTypeQuizResult?>
{
    private readonly IGamerDnaService _gamerDnaService;
    private readonly INotificationService _notificationService;
    private readonly ILogger _logger;

    [ObservableProperty]
    private bool _isLoading = true;

    [ObservableProperty]
    private ObservableCollection<GamerTypeQuizQuestion> _questions = new();

    [ObservableProperty]
    private int _currentQuestionIndex;

    [ObservableProperty]
    private GamerTypeQuizQuestion? _currentQuestion;

    [ObservableProperty]
    private int _totalQuestions;

    [ObservableProperty]
    private double _progressPercentage;

    [ObservableProperty]
    private bool _showResults;

    [ObservableProperty]
    private GamerTypeQuizResult? _quizResult;

    [ObservableProperty]
    private string _resultTitle = string.Empty;

    [ObservableProperty]
    private string _resultDescription = string.Empty;

    [ObservableProperty]
    private string _resultIcon = "🎮";

    [ObservableProperty]
    private string _resultColor = "#808080";

    [ObservableProperty]
    private ObservableCollection<ArchetypeResultViewModel> _allResults = new();

    [ObservableProperty]
    private ObservableCollection<string> _recommendedGenres = new();

    private readonly List<(GamerArchetype Archetype, int Weight)> _answers = new();

    public GamerTypeQuizViewModel(
        IGamerDnaService gamerDnaService,
        INotificationService notificationService,
        ILogger logger)
    {
        _gamerDnaService = gamerDnaService;
        _notificationService = notificationService;
        _logger = logger;

        _ = LoadQuestionsAsync();
    }

    private async Task LoadQuestionsAsync()
    {
        IsLoading = true;

        try
        {
            var result = await _gamerDnaService.GetQuizQuestionsAsync();

            if (result.IsFailure)
            {
                _notificationService.ShowError($"Failed to load quiz: {result.Error}");
                return;
            }

            Questions = new ObservableCollection<GamerTypeQuizQuestion>(result.Value!);
            TotalQuestions = Questions.Count;
            CurrentQuestionIndex = 0;
            UpdateCurrentQuestion();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading quiz questions");
            _notificationService.ShowError("Failed to load quiz questions");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void SelectAnswer(GamerTypeQuizAnswer answer)
    {
        _answers.Add((answer.Archetype, answer.Weight));

        if (CurrentQuestionIndex < TotalQuestions - 1)
        {
            CurrentQuestionIndex++;
            UpdateCurrentQuestion();
        }
        else
        {
            _ = CalculateResultsAsync();
        }
    }

    [RelayCommand]
    private void PreviousQuestion()
    {
        if (CurrentQuestionIndex > 0)
        {
            // Remove last answer
            if (_answers.Count > 0)
            {
                _answers.RemoveAt(_answers.Count - 1);
            }

            CurrentQuestionIndex--;
            UpdateCurrentQuestion();
        }
    }

    [RelayCommand]
    private void RestartQuiz()
    {
        _answers.Clear();
        CurrentQuestionIndex = 0;
        ShowResults = false;
        QuizResult = null;
        UpdateCurrentQuestion();
    }

    [RelayCommand]
    private void CloseDialog()
    {
        CloseRequested?.Invoke(this, QuizResult);
    }

    private void UpdateCurrentQuestion()
    {
        CurrentQuestion = Questions.Count > CurrentQuestionIndex
            ? Questions[CurrentQuestionIndex]
            : null;

        ProgressPercentage = TotalQuestions > 0
            ? (double)CurrentQuestionIndex / TotalQuestions * 100
            : 0;
    }

    private async Task CalculateResultsAsync()
    {
        IsLoading = true;

        try
        {
            var result = await _gamerDnaService.ProcessQuizAnswersAsync(_answers);

            if (result.IsFailure)
            {
                _notificationService.ShowError($"Failed to calculate results: {result.Error}");
                return;
            }

            QuizResult = result.Value;
            ShowResults = true;

            // Update result display
            ResultTitle = QuizResult.PrimaryArchetype.GetDisplayName();
            ResultDescription = QuizResult.Description;
            ResultIcon = QuizResult.PrimaryArchetype.GetIcon();
            ResultColor = QuizResult.PrimaryArchetype.GetPrimaryColor();

            RecommendedGenres = new ObservableCollection<string>(QuizResult.RecommendedGenres);

            // Create sorted list of all archetype scores
            AllResults = new ObservableCollection<ArchetypeResultViewModel>(
                QuizResult.ArchetypeScores
                    .OrderByDescending(s => s.Value)
                    .Select(s => new ArchetypeResultViewModel
                    {
                        Archetype = s.Key,
                        Name = s.Key.GetDisplayName(),
                        Score = s.Value,
                        Percentage = (double)s.Value / QuizResult.ArchetypeScores.Values.Max() * 100,
                        Icon = s.Key.GetIcon(),
                        Color = s.Key.GetPrimaryColor(),
                        IsPrimary = s.Key == QuizResult.PrimaryArchetype
                    }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating quiz results");
            _notificationService.ShowError("Failed to calculate results");
        }
        finally
        {
            IsLoading = false;
        }
    }

    public event EventHandler<GamerTypeQuizResult?>? CloseRequested;
    public string Title => "Discover Your Gamer Type";
}

/// <summary>
/// ViewModel for displaying a single archetype result in the quiz.
/// </summary>
public partial class ArchetypeResultViewModel : ObservableObject
{
    [ObservableProperty]
    private GamerArchetype _archetype;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private int _score;

    [ObservableProperty]
    private double _percentage;

    [ObservableProperty]
    private string _icon = "🎮";

    [ObservableProperty]
    private string _color = "#808080";

    [ObservableProperty]
    private bool _isPrimary;
}

/// <summary>
/// Interface for dialog ViewModels with a result type.
/// </summary>
public interface IDialogViewModel<TResult>
{
    string Title { get; }
    event EventHandler<TResult>? CloseRequested;
}
