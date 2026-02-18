// Copyright (c) 2026 SaveStateReborn. All rights reserved.

using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SaveState.Presentation.Views.Settings;

/// <summary>
/// Settings view for Smart Launcher configuration.
/// </summary>
public partial class SmartLauncherSettingsView : UserControl
{
    public SmartLauncherSettingsView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
