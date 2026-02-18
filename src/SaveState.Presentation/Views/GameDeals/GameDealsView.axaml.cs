// Copyright (c) 2026 SaveStateReborn. All rights reserved.

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using SaveState.Presentation.ViewModels.GameDeals;

namespace SaveState.Presentation.Views.GameDeals;

/// <summary>
/// View for browsing game deals.
/// </summary>
public partial class GameDealsView : UserControl
{
    public GameDealsView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnDealPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border border && border.DataContext is GameDealViewModel deal)
        {
            if (DataContext is GameDealsViewModel viewModel)
            {
                viewModel.ShowDealDetailsCommand.Execute(deal);
            }
        }
    }
}
