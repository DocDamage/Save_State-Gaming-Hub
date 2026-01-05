using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using SaveState.Application.Mugen.DTOs;
using SaveState.Application.Mugen.Queries;
using SaveState.Application.Mugen.Commands;
using Microsoft.Extensions.Options;
using SaveState.Core.Configuration;

namespace SaveState.Presentation.ViewModels.Shell.Mugen;

public partial class MugenRosterViewModel : MugenSectionViewModelBase
{
    private readonly IMediator _mediator;
    private readonly MugenOptions _mugenOptions;

    [ObservableProperty]
    private string _searchTerm = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    public ObservableCollection<MugenCharacterSummaryDto> AllCharacters { get; } = new();
    public ObservableCollection<MugenCharacterSummaryDto> FilteredCharacters { get; } = new();

    public MugenRosterViewModel(IMediator mediator, IOptions<MugenOptions> mugenOptions)
    {
        _mediator = mediator;
        _mugenOptions = mugenOptions.Value;
        Title = "CHARACTER ROSTER";
    }

    partial void OnSearchTermChanged(string value)
    {
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var filtered = string.IsNullOrWhiteSpace(SearchTerm)
            ? (IEnumerable<MugenCharacterSummaryDto>)AllCharacters
            : AllCharacters.Where(c => c.Name.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                                       (c.Author?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false));

        FilteredCharacters.Clear();
        foreach (var character in filtered)
        {
            FilteredCharacters.Add(character);
        }
    }

    public override async Task InitializeAsync()
    {
        if (AllCharacters.Count == 0)
        {
            await LoadCharactersAsync();
        }
    }

    [RelayCommand]
    private async Task LoadCharactersAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Loading MUGEN characters...";

            var query = new GetMugenCharactersQuery();
            var results = await _mediator.Send(query);

            AllCharacters.Clear();
            foreach (var character in results)
            {
                AllCharacters.Add(character);
            }

            ApplyFilter();
            StatusMessage = $"Found {AllCharacters.Count} characters.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ScanCharactersAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Scanning for MUGEN characters...";

            foreach (var path in _mugenOptions.CharacterDirectories)
            {
                StatusMessage = $"Scanning: {path}";
                await _mediator.Send(new ScanMugenCharactersCommand(path));
            }

            await LoadCharactersAsync();
            StatusMessage = "Scan complete.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Scan failed: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
