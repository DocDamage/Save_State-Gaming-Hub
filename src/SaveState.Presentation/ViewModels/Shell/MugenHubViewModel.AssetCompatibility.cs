using System;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Core.Mugen.Entities;
using SaveState.Core.Mugen.ValueObjects;
using SaveState.Core.Mugen.DTOs;
// Use ValueObjects as canonical types for ambiguous names
using MugenAssetEntry = SaveState.Core.Mugen.ValueObjects.MugenAssetEntry;

namespace SaveState.Presentation.ViewModels.Shell;

/// <summary>
/// Asset and compatibility partial class for MugenHubViewModel.
/// </summary>
public partial class MugenHubViewModel
{
    [RelayCommand]
    private async Task LoadMoveListAsync()
    {
        await LoadMoveListForSelectionAsync(SelectedCharacter);
    }

    [RelayCommand]
    private async Task LoadAssetsAsync()
    {
        await LoadAssetPreviewForSelectionAsync(SelectedCharacter);
    }

    [RelayCommand]
    private async Task OpenAssetAsync(MugenAssetEntry? asset)
    {
        if (asset == null || string.IsNullOrWhiteSpace(asset.FullPath))
            return;

        try
        {
            if (!File.Exists(asset.FullPath))
            {
                _notificationService.ShowWarning("Asset file not found.");
                return;
            }

            await Task.Run(() =>
            {
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = asset.FullPath,
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(startInfo);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open asset");
            _notificationService.ShowError("Failed to open asset.");
        }
    }

    [RelayCommand]
    private async Task AnalyzeCompatibilityAsync()
    {
        if (SelectedCharacter == null)
        {
            _notificationService.ShowWarning("Select a character to analyze.");
            return;
        }

        try
        {
            IsCompatibilityLoading = true;
            CompatibilityStatus = "Analyzing compatibility...";

            var result = await _compatibilityService.AnalyzeAsync(SelectedCharacter);
            CompatibilityIssues.Clear();
            CompatibilityFixes.Clear();

            if (result.IsSuccess && result.Value != null)
            {
                foreach (var issue in result.Value.Issues)
                    CompatibilityIssues.Add(new MugenCompatibilityIssue(
                        issue.Code,
                        issue.Message,
                        "Warning",
                        null));

                CompatibilityStatus = CompatibilityIssues.Count == 0
                    ? "No issues detected."
                    : $"{CompatibilityIssues.Count} issues found.";
            }
            else
            {
                CompatibilityStatus = result.Error ?? "Compatibility analysis failed.";
                _notificationService.ShowError(CompatibilityStatus);
            }
        }
        catch (Exception ex)
        {
            CompatibilityStatus = $"Compatibility analysis failed: {ex.Message}";
            _notificationService.ShowError(CompatibilityStatus);
        }
        finally
        {
            IsCompatibilityLoading = false;
        }
    }

    [RelayCommand]
    private async Task FixCompatibilityAsync()
    {
        if (SelectedCharacter == null)
        {
            _notificationService.ShowWarning("Select a character to fix.");
            return;
        }

        try
        {
            IsCompatibilityLoading = true;
            CompatibilityStatus = "Applying fixes...";

            var result = await _compatibilityService.FixAsync(SelectedCharacter);
            CompatibilityIssues.Clear();
            CompatibilityFixes.Clear();

            if (result.IsSuccess && result.Value != null)
            {
                foreach (var fix in result.Value.Fixes)
                    CompatibilityFixes.Add(new MugenCompatibilityFix(
                        fix.Code,
                        fix.Message,
                        true,
                        null));

                foreach (var issue in result.Value.Issues)
                     CompatibilityIssues.Add(new MugenCompatibilityIssue(
                        issue.Code,
                        issue.Message,
                        "Warning",
                        null));

                CompatibilityStatus = CompatibilityFixes.Count == 0
                    ? "No fixes applied."
                    : $"{CompatibilityFixes.Count} fixes applied.";
            }
            else
            {
                CompatibilityStatus = result.Error ?? "Compatibility fixes failed.";
                _notificationService.ShowError(CompatibilityStatus);
            }
        }
        catch (Exception ex)
        {
            CompatibilityStatus = $"Compatibility fixes failed: {ex.Message}";
            _notificationService.ShowError(CompatibilityStatus);
        }
        finally
        {
            IsCompatibilityLoading = false;
        }
    }

    private async Task LoadMoveListForSelectionAsync(MugenCharacter? character)
    {
        if (character == null)
        {
            MoveList.Clear();
            MoveListStatus = "Select a character to load moves.";
            return;
        }

        try
        {
            IsMoveListLoading = true;
            MoveListStatus = "Loading move list...";

            var result = await _moveListService.GetMoveListAsync(character);
            MoveList.Clear();

            if (result.IsSuccess && result.Value != null)
            {
                foreach (var entry in result.Value)
                    // Map ValueObject to DTO
                    MoveList.Add(new MugenMoveEntryDto
                    {
                        MoveName = entry.Name,
                        Command = entry.Command,
                        Type = "Normal",
                        Notes = entry.Comment
                    });

                MoveListStatus = MoveList.Count == 0 ? "No moves found." : $"{MoveList.Count} moves loaded.";
            }
            else
            {
                MoveListStatus = result.Error ?? "Move list load failed.";
            }
        }
        catch (Exception ex)
        {
            MoveListStatus = $"Move list load failed: {ex.Message}";
        }
        finally
        {
            IsMoveListLoading = false;
        }
    }

    private async Task LoadAssetPreviewForSelectionAsync(MugenCharacter? character)
    {
        if (character == null)
        {
            AssetEntries.Clear();
            AssetStatus = "Select a character to preview assets.";
            return;
        }

        try
        {
            IsAssetLoading = true;
            AssetStatus = "Loading assets...";

            var result = await _assetPreviewService.GetAssetsAsync(character);
            AssetEntries.Clear();

            if (result.IsSuccess && result.Value != null)
            {
                foreach (var entry in result.Value)
                    AssetEntries.Add(entry);

                AssetStatus = AssetEntries.Count == 0 ? "No assets found." : $"{AssetEntries.Count} assets found.";
            }
            else
            {
                AssetStatus = result.Error ?? "Asset load failed.";
            }
        }
        catch (Exception ex)
        {
            AssetStatus = $"Asset load failed: {ex.Message}";
        }
        finally
        {
            IsAssetLoading = false;
        }
    }
}
