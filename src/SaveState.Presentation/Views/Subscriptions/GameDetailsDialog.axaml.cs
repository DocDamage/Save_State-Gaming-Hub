// Copyright (c) 2026 SaveStateReborn. All rights reserved.

using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;

namespace SaveState.Presentation.Views.Subscriptions;

/// <summary>
/// Dialog for displaying detailed game information.
/// </summary>
public partial class GameDetailsDialog : Window
{
    public GameDetailsDialog()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
