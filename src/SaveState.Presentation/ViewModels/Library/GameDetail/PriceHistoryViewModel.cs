using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Core.Common.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace SaveState.Presentation.ViewModels.Library.GameDetail;

public partial class PriceHistoryViewModel : ObservableObject
{
    private readonly ITimeProvider _timeProvider;

    [ObservableProperty]
    private string _gameTitle = string.Empty;

    [ObservableProperty]
    private ObservableCollection<PricePoint> _history = new();

    [ObservableProperty]
    private string _currentPrice = "$0.00";

    [ObservableProperty]
    private string _lowestPrice = "$0.00";

    [ObservableProperty]
    private string _highestPrice = "$0.00";

    public PriceHistoryViewModel(ITimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public void Initialize(string gameTitle)
    {
        GameTitle = gameTitle;
        GenerateMockData();
    }

    private void GenerateMockData()
    {
        var rng = new Random();
        var basePrice = 59.99;
        var points = new ObservableCollection<PricePoint>();

        DateTime start = _timeProvider.Now.AddMonths(-6);
        double current = basePrice;

        for (int i = 0; i < 30; i++)
        {
            var date = start.AddDays(i * 6);
            if (rng.NextDouble() < 0.2)
            {
                current = basePrice * (0.5 + rng.NextDouble() * 0.4); // Sale
            }
            else
            {
                current = basePrice;
            }

            points.Add(new PricePoint(date, current));
        }

        History = points;
        CurrentPrice = $"${points.Last().Price:F2}";
        LowestPrice = $"${points.Min(p => p.Price):F2}";
        HighestPrice = $"${points.Max(p => p.Price):F2}";
    }

    public Action? RequestClose { get; set; }

    [RelayCommand]
    private void Close()
    {
        RequestClose?.Invoke();
    }
}

public record PricePoint(DateTime Date, double Price);
