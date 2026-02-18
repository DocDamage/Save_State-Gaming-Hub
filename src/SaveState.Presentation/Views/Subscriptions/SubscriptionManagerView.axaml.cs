// Copyright (c) 2026 SaveStateReborn. All rights reserved.

using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SaveState.Presentation.Views.Subscriptions;

/// <summary>
/// View for managing game subscription services.
/// </summary>
public partial class SubscriptionManagerView : UserControl
{
    public SubscriptionManagerView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
