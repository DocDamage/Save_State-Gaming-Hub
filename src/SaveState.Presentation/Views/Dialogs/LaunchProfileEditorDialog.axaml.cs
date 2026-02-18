// Copyright (c) 2026 SaveStateReborn. All rights reserved.

using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SaveState.Core.SmartLauncher;

namespace SaveState.Presentation.Views.Dialogs;

/// <summary>
/// Dialog for editing launch profiles.
/// </summary>
public partial class LaunchProfileEditorDialog : Window
{
    private LaunchProfile? _profile;

    public LaunchProfileEditorDialog()
    {
        InitializeComponent();
    }

    public LaunchProfile? EditedProfile => _profile;

    public void SetProfile(LaunchProfile profile)
    {
        _profile = profile;
        LoadProfileData();
    }

    private void LoadProfileData()
    {
        if (_profile == null) return;

        ProfileNameTextBox.Text = _profile.Name;
        ProfileDescriptionTextBox.Text = _profile.Description;

        // Set priority
        for (int i = 0; i < PriorityComboBox.Items.Count; i++)
        {
            var item = (ComboBoxItem)PriorityComboBox.Items[i]!;
            if (item.Tag?.ToString() == _profile.Priority.ToString())
            {
                PriorityComboBox.SelectedIndex = i;
                break;
            }
        }

        // Set performance settings
        EnableMemoryOptimizationCheckBox.IsChecked = _profile.PerformanceSettings.EnableMemoryOptimization;
        ClearStandbyListCheckBox.IsChecked = _profile.PerformanceSettings.ClearStandbyList;
        DisableVisualEffectsCheckBox.IsChecked = _profile.PerformanceSettings.DisableVisualEffects;
        DisableFullscreenOptimizationsCheckBox.IsChecked = _profile.DisableFullscreenOptimizations;
        RunAsAdministratorCheckBox.IsChecked = _profile.RunAsAdministrator;

        // Set processes
        ProcessesTextBox.Text = string.Join(Environment.NewLine, _profile.ProcessesToSuspend);

        // Set power plan
        if (!string.IsNullOrEmpty(_profile.PowerPlanGuid))
        {
            for (int i = 0; i < PowerPlanComboBox.Items.Count; i++)
            {
                var item = (ComboBoxItem)PowerPlanComboBox.Items[i]!;
                if (item.Tag?.ToString() == _profile.PowerPlanGuid)
                {
                    PowerPlanComboBox.SelectedIndex = i;
                    break;
                }
            }
        }
    }

    private void OnSaveClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_profile == null)
        {
            _profile = new LaunchProfile();
        }

        _profile.Name = ProfileNameTextBox.Text ?? "New Profile";
        _profile.Description = ProfileDescriptionTextBox.Text;

        // Parse priority
        if (PriorityComboBox.SelectedItem is ComboBoxItem priorityItem &&
            Enum.TryParse<ProcessPriority>(priorityItem.Tag?.ToString(), out var priority))
        {
            _profile.Priority = priority;
        }

        // Set performance settings
        _profile.PerformanceSettings.EnableMemoryOptimization = EnableMemoryOptimizationCheckBox.IsChecked ?? false;
        _profile.PerformanceSettings.ClearStandbyList = ClearStandbyListCheckBox.IsChecked ?? false;
        _profile.PerformanceSettings.DisableVisualEffects = DisableVisualEffectsCheckBox.IsChecked ?? false;
        _profile.DisableFullscreenOptimizations = DisableFullscreenOptimizationsCheckBox.IsChecked ?? true;
        _profile.RunAsAdministrator = RunAsAdministratorCheckBox.IsChecked ?? false;

        // Parse processes
        _profile.ProcessesToSuspend = ProcessesTextBox.Text?
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim())
            .Where(p => !string.IsNullOrEmpty(p))
            .ToList() ?? new List<string>();

        // Set power plan
        if (PowerPlanComboBox.SelectedItem is ComboBoxItem powerItem)
        {
            _profile.PowerPlanGuid = powerItem.Tag?.ToString();
        }

        Close(true);
    }

    private void OnCancelClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close(false);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
