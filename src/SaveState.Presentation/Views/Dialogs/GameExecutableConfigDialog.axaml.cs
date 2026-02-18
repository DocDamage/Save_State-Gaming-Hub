// Copyright (c) 2026 SaveStateReborn. All rights reserved.

using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using SaveState.Core.GameLibrary.Entities;

namespace SaveState.Presentation.Views.Dialogs;

/// <summary>
/// Dialog for configuring game executable paths.
/// </summary>
public partial class GameExecutableConfigDialog : Window
{
    private Game? _game;

    public GameExecutableConfigDialog()
    {
        InitializeComponent();
    }

    public string? ExecutablePath => ExecutablePathTextBox.Text;
    public string? LaunchArguments => LaunchArgumentsTextBox.Text;

    public void SetGame(Game game)
    {
        _game = game;
        GameTitleTextBlock.Text = game.Title;
        GamePlatformTextBlock.Text = game.Platform?.Name ?? "Unknown Platform";

        // Pre-populate if values exist
        ExecutablePathTextBox.Text = game.ExecutablePath ?? string.Empty;
        LaunchArgumentsTextBox.Text = game.LaunchArguments ?? string.Empty;
    }

    private async void OnBrowseClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        try
        {
            var storageProvider = StorageProvider;
            if (storageProvider == null) return;

            var options = new FilePickerOpenOptions
            {
                Title = "Select Game Executable",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Executables")
                    {
                        Patterns = new[] { "*.exe" },
                        MimeTypes = new[] { "application/x-msdownload" }
                    },
                    new FilePickerFileType("All Files")
                    {
                        Patterns = new[] { "*" },
                        MimeTypes = new[] { "*/*" }
                    }
                }
            };

            var result = await storageProvider.OpenFilePickerAsync(options);
            if (result.Count > 0 && result[0].Path.IsFile)
            {
                ExecutablePathTextBox.Text = result[0].Path.LocalPath;
            }
        }
        catch (Exception ex)
        {
            // Log error or show message
            System.Diagnostics.Debug.WriteLine($"Error browsing for executable: {ex}");
        }
    }

    private void OnSaveClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ExecutablePathTextBox.Text))
        {
            // Show validation error
            return;
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
