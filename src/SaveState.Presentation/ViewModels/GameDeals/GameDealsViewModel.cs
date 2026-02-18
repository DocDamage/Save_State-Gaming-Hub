// Copyright (c) 2026 SaveStateReborn. All rights reserved.

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Core.GameDeals;

namespace SaveState.Presentation.ViewModels.GameDeals;

/// <summary>
/// ViewModel for the Game Deals page.
/// </summary>
public sealed partial class GameDealsViewModel : ObservableObject
{
    private readonly IGameDealsService _dealsService;
    private readonly ILogger<GameDealsViewModel> _logger;

    [ObservableProperty]
    private ObservableCollection<GameDealViewModel> _deals = new();

    [ObservableProperty]
    private ObservableCollection<PriceAlertViewModel> _myAlerts = new();

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private decimal? _minDiscountPercent;

    [ObservableProperty]
    private decimal? _maxPrice;

    [ObservableProperty]
    private bool _onlyHistoricalLows;

    [ObservableProperty]
    private string _selectedSortOrder = "Discount";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private GameDealViewModel? _selectedDeal;

    [ObservableProperty]
    private DealStatisticsViewModel? _selectedGameStats;

    public ObservableCollection<string> SortOptions { get; } = new()
    {
        "Discount",
        "Price (Low to High)",
        "Price (High to Low)",
        "Title",
        "Newest"
    };

    public GameDealsViewModel(
        IGameDealsService dealsService,
        ILogger<GameDealsViewModel> logger)
    {
        _dealsService = dealsService ?? throw new ArgumentNullException(nameof(dealsService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Load data on initialization
        _ = LoadDealsAsync();
        _ = LoadMyAlertsAsync();
    }

    [RelayCommand]
    private async Task LoadDealsAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            var filter = new DealFilterOptions
            {
                SearchQuery = SearchQuery,
                MinDiscountPercent = MinDiscountPercent,
                MaxPrice = MaxPrice,
                OnlyHistoricalLows = OnlyHistoricalLows,
                SortOrder = MapSortOrder(SelectedSortOrder)
            };

            var result = await _dealsService.GetDealsAsync(filter);

            if (result.IsSuccess && result.Value != null)
            {
                Deals.Clear();
                foreach (var deal in result.Value)
                {
                    Deals.Add(new GameDealViewModel(deal));
                }
            }
            else
            {
                ErrorMessage = result.Error ?? "Failed to load deals";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load deals");
            ErrorMessage = "An error occurred while loading deals";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task LoadMyAlertsAsync()
    {
        try
        {
            // Use current user ID (would come from auth context)
            var userId = Guid.NewGuid(); // Placeholder
            var result = await _dealsService.GetUserPriceAlertsAsync(userId);

            if (result.IsSuccess && result.Value != null)
            {
                MyAlerts.Clear();
                foreach (var alert in result.Value)
                {
                    MyAlerts.Add(new PriceAlertViewModel(alert));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load price alerts");
        }
    }

    [RelayCommand]
    private async Task SearchDealsAsync()
    {
        await LoadDealsAsync();
    }

    [RelayCommand]
    private async Task RefreshDealsAsync()
    {
        try
        {
            IsLoading = true;
            var result = await _dealsService.RefreshDealsAsync();

            if (result.IsSuccess)
            {
                await LoadDealsAsync();
            }
            else
            {
                ErrorMessage = result.Error ?? "Failed to refresh deals";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh deals");
            ErrorMessage = "An error occurred while refreshing";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ShowDealDetailsAsync(GameDealViewModel? deal)
    {
        if (deal == null) return;

        SelectedDeal = deal;

        try
        {
            var statsResult = await _dealsService.GetDealStatisticsAsync(deal.Title);
            if (statsResult.IsSuccess)
            {
                SelectedGameStats = new DealStatisticsViewModel(statsResult.Value);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load deal statistics");
        }
    }

    [RelayCommand]
    private async Task CreatePriceAlertAsync(GameDealViewModel? deal)
    {
        if (deal == null) return;

        try
        {
            var userId = Guid.NewGuid(); // Placeholder
            var targetPrice = deal.CurrentPrice * 0.8m; // 20% below current price

            var result = await _dealsService.CreatePriceAlertAsync(
                userId, deal.Title, targetPrice, 20);

            if (result.IsSuccess)
            {
                await LoadMyAlertsAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create price alert");
        }
    }

    [RelayCommand]
    private async Task DeleteAlertAsync(PriceAlertViewModel? alert)
    {
        if (alert == null) return;

        try
        {
            var result = await _dealsService.DeletePriceAlertAsync(alert.Id);
            if (result.IsSuccess)
            {
                MyAlerts.Remove(alert);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete price alert");
        }
    }

    [RelayCommand]
    private void ClearFilters()
    {
        SearchQuery = string.Empty;
        MinDiscountPercent = null;
        MaxPrice = null;
        OnlyHistoricalLows = false;
        SelectedSortOrder = "Discount";
        _ = LoadDealsAsync();
    }

    private DealSortOrder MapSortOrder(string sortOption)
    {
        return sortOption switch
        {
            "Discount" => DealSortOrder.DiscountPercent,
            "Price (Low to High)" => DealSortOrder.Price,
            "Price (High to Low)" => DealSortOrder.Price,
            "Title" => DealSortOrder.Title,
            "Newest" => DealSortOrder.Newest,
            _ => DealSortOrder.DiscountPercent
        };
    }

    partial void OnSearchQueryChanged(string value) => _ = SearchDealsAsync();
    partial void OnMinDiscountPercentChanged(decimal? value) => _ = LoadDealsAsync();
    partial void OnMaxPriceChanged(decimal? value) => _ = LoadDealsAsync();
    partial void OnOnlyHistoricalLowsChanged(bool value) => _ = LoadDealsAsync();
    partial void OnSelectedSortOrderChanged(string value) => _ = LoadDealsAsync();
}

/// <summary>
/// ViewModel for a game deal.
/// </summary>
public sealed class GameDealViewModel : ObservableObject
{
    private readonly GameDeal _deal;

    public GameDealViewModel(GameDeal deal)
    {
        _deal = deal ?? throw new ArgumentNullException(nameof(deal));
    }

    public string Id => _deal.Id;
    public string Title => _deal.Title;
    public string? ImageUrl => _deal.ImageUrl;
    public decimal CurrentPrice => _deal.CurrentPrice;
    public decimal? RegularPrice => _deal.RegularPrice;
    public decimal? DiscountPercent => _deal.DiscountPercent;
    public string FormattedPrice => _deal.FormattedPrice;
    public string? FormattedRegularPrice => _deal.FormattedRegularPrice;
    public string? FormattedDiscount => _deal.FormattedDiscount;
    public string StoreName => _deal.Store.Name;
    public string StoreColor => _deal.Store.Color;
    public bool IsHistoricalLow => _deal.IsHistoricalLow;
    public string? StoreUrl => _deal.StoreUrl;
    public int? MetacriticScore => _deal.MetacriticScore;
    public decimal? Savings => _deal.Savings;

    public string SavingsText => Savings.HasValue ? $"Save ${Savings.Value:F2}" : string.Empty;

    public string BadgeText
    {
        get
        {
            if (IsHistoricalLow) return "🔥 HISTORICAL LOW";
            if (DiscountPercent >= 75) return "🔥 MEGA DEAL";
            if (DiscountPercent >= 50) return "⭐ GREAT DEAL";
            return string.Empty;
        }
    }

    public bool ShowBadge => !string.IsNullOrEmpty(BadgeText);
}

/// <summary>
/// ViewModel for a price alert.
/// </summary>
public sealed class PriceAlertViewModel : ObservableObject
{
    private readonly PriceAlert _alert;

    public PriceAlertViewModel(PriceAlert alert)
    {
        _alert = alert ?? throw new ArgumentNullException(nameof(alert));
    }

    public Guid Id => _alert.Id;
    public string GameTitle => _alert.GameTitle;
    public decimal? TargetPrice => _alert.TargetPrice;
    public decimal? TargetDiscountPercent => _alert.TargetDiscountPercent;
    public bool AlertOnHistoricalLow => _alert.AlertOnHistoricalLow;
    public bool IsActive => _alert.IsActive;
    public DateTime CreatedAt => _alert.CreatedAt;

    public string TargetText
    {
        get
        {
            if (TargetPrice.HasValue) return $"Target: ${TargetPrice.Value:F2}";
            if (TargetDiscountPercent.HasValue) return $"Target: {TargetDiscountPercent.Value:F0}% off";
            if (AlertOnHistoricalLow) return "Alert on historical low";
            return "No target set";
        }
    }
}

/// <summary>
/// ViewModel for deal statistics.
/// </summary>
public sealed class DealStatisticsViewModel : ObservableObject
{
    private readonly DealStatistics _stats;

    public DealStatisticsViewModel(DealStatistics stats)
    {
        _stats = stats ?? throw new ArgumentNullException(nameof(stats));
    }

    public string GameTitle => _stats.GameTitle;
    public decimal CurrentLowestPrice => _stats.CurrentLowestPrice;
    public decimal HistoricalLowestPrice => _stats.HistoricalLowestPrice;
    public decimal AveragePrice => _stats.AveragePrice;
    public int SaleCount => _stats.SaleCount;
    public PriceTrend Trend => _stats.Trend;
    public int? DaysSinceLastSale => _stats.DaysSinceLastSale;
    public string? BestTimeToBuyRecommendation => _stats.BestTimeToBuyRecommendation;

    public string TrendIcon => Trend switch
    {
        PriceTrend.Rising => "📈",
        PriceTrend.Falling => "📉",
        PriceTrend.Stable => "➡️",
        _ => "❓"
    };

    public string TrendText => Trend switch
    {
        PriceTrend.Rising => "Rising",
        PriceTrend.Falling => "Falling",
        PriceTrend.Stable => "Stable",
        _ => "Unknown"
    };

    public string SavingsVsAverageText
    {
        get
        {
            if (AveragePrice > 0)
            {
                var savings = AveragePrice - CurrentLowestPrice;
                var percent = (savings / AveragePrice) * 100;
                return $"{percent:F0}% below average";
            }
            return string.Empty;
        }
    }
}
