// Copyright (c) 2026 SaveStateReborn. All rights reserved.

using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SaveState.Presentation.Views.SmartLauncher;

/// <summary>
/// View for the Smart Launcher feature.
/// </summary>
public partial class SmartLauncherView : UserControl
{
    public SmartLauncherView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
