using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using SaveState.Core.Mugen.DTOs;
using SaveState.Application.Mugen.Queries;
using SaveState.Application.Mugen.Commands;
using Microsoft.Extensions.Options;
using SaveState.Core.Configuration;
using SaveState.Core.Mugen.Services;
using SaveState.Core.Mugen.ValueObjects;
using System.Linq;
// Use ValueObjects as canonical types for ambiguous names
using MugenRosterEntry = SaveState.Core.Mugen.ValueObjects.MugenRosterEntry;
using MugenRosterEntryType = SaveState.Core.Mugen.ValueObjects.MugenRosterEntryType;

namespace SaveState.Presentation.ViewModels.Shell.Mugen;

public partial class MugenRosterViewModel : MugenSectionViewModelBase
{
    private readonly IMediator _mediator;
    private readonly MugenOptions _mugenOptions;
    private readonly IMugenRosterService _rosterService;

    [ObservableProperty]
    private string _searchTerm = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    // --- Database View ---
    public ObservableCollection<MugenCharacterSummary> AllCharacters { get; } = new();
    public ObservableCollection<MugenCharacterSummary> FilteredCharacters { get; } = new();

    // --- Roster Editor View ---
    public ObservableCollection<MugenRosterEntryViewModel> RosterEntries { get; } = new();

    [ObservableProperty]
    private bool _isRosterLoading;

    [ObservableProperty]
    private string _rosterFilePath;

    [ObservableProperty]
    private MugenRosterEntryViewModel? _selectedRosterEntry;

    [ObservableProperty]
    private string _newRosterCategoryName = string.Empty;

    [ObservableProperty]
    private string _newRosterCharacterPath = string.Empty;

    [ObservableProperty]
    private string _newRosterStagePath = string.Empty;

    [ObservableProperty]
    private bool _showEditor = false; // Toggle between DB View and Editor View

    private IReadOnlyList<string> _rosterHeaderLines = Array.Empty<string>();
    private IReadOnlyList<string> _rosterFooterLines = Array.Empty<string>();

    public MugenRosterViewModel(IMediator mediator, IOptions<MugenOptions> mugenOptions, IMugenRosterService rosterService)
    {
        _mediator = mediator;
        _mugenOptions = mugenOptions.Value;
        _rosterService = rosterService;
        Title = "CHARACTER ROSTER";
    }

    partial void OnSearchTermChanged(string value)
    {
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var filtered = string.IsNullOrWhiteSpace(SearchTerm)
            ? (IEnumerable<MugenCharacterSummary>)AllCharacters
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
    [RelayCommand]
    private void ToggleView()
    {
        ShowEditor = !ShowEditor;
        if (ShowEditor && RosterEntries.Count == 0)
        {
            _ = LoadRosterAsync();
        }
    }

    [RelayCommand]
    private async Task LoadRosterAsync()
    {
        try
        {
            IsRosterLoading = true;
            StatusMessage = "Loading roster file...";

            var result = await _rosterService.LoadRosterAsync();
            if (!result.IsSuccess || result.Value == null)
            {
                RosterEntries.Clear();
                _rosterHeaderLines = Array.Empty<string>();
                _rosterFooterLines = Array.Empty<string>();
                StatusMessage = result.Error ?? "Failed to load roster.";
                RosterFilePath = _rosterService.GetSelectDefPath() ?? "select.def not found";
                return;
            }

            var roster = result.Value;
            _rosterHeaderLines = roster.HeaderLines;
            _rosterFooterLines = roster.FooterLines;
            RosterFilePath = _rosterService.GetSelectDefPath() ?? "Unknown path";

            RosterEntries.Clear();
            SelectedRosterEntry = null;
            foreach (var entry in roster.Entries)
            {
                RosterEntries.Add(new MugenRosterEntryViewModel(entry));
            }

            StatusMessage = $"Loaded {RosterEntries.Count} entries from select.def";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Roster load failed: {ex.Message}";
        }
        finally
        {
            IsRosterLoading = false;
        }
    }

    [RelayCommand]
    private async Task SaveRosterAsync()
    {
        try
        {
            IsRosterLoading = true;
            StatusMessage = "Saving roster...";

            var entries = RosterEntries.Select(entry => entry.ToEntry()).ToList();
            var roster = new MugenRoster(entries, _rosterHeaderLines, _rosterFooterLines);
            var result = await _rosterService.SaveRosterAsync(roster);

            if (result.IsSuccess)
            {
                StatusMessage = "Roster saved successfully.";
            }
            else
            {
                StatusMessage = result.Error ?? "Roster save failed.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Roster save failed: {ex.Message}";
        }
        finally
        {
            IsRosterLoading = false;
        }
    }

    [RelayCommand]
    private void AddRosterCategory()
    {
        if (string.IsNullOrWhiteSpace(NewRosterCategoryName)) return;

        var entry = new MugenRosterEntry(
            MugenRosterEntryType.Category,
            null,
            null,
            NewRosterCategoryName.Trim(),
            null);

        InsertRosterEntry(new MugenRosterEntryViewModel(entry));
        NewRosterCategoryName = string.Empty;
    }

    [RelayCommand]
    private void AddRosterCharacter()
    {
        if (string.IsNullOrWhiteSpace(NewRosterCharacterPath)) return;

        var category = SelectedRosterEntry?.EntryType == MugenRosterEntryType.Category
            ? SelectedRosterEntry.Category
            : SelectedRosterEntry?.Category;

        var stagePath = string.IsNullOrWhiteSpace(NewRosterStagePath)
            ? null
            : NewRosterStagePath.Trim();

        var entry = new MugenRosterEntry(
            MugenRosterEntryType.Character,
            NewRosterCharacterPath.Trim(),
            stagePath,
            category,
            null);

        InsertRosterEntry(new MugenRosterEntryViewModel(entry));
        NewRosterCharacterPath = string.Empty;
        NewRosterStagePath = string.Empty;
    }

    [RelayCommand]
    private void AddToRoster(MugenCharacterSummary character)
    {
        if (character == null) return;

        // Try to construct a relative path if possible, or use character name folder
        // For MUGEN, it's usually folder/defname.def
        // We'll assume the character folder name is a good guess if we don't have the full path here
        // But the DTO should probably have enough info.

        // Let's assume the character folder name is 'character.Name'
        var charPath = $"{character.Name}/{character.Name}.def";

        var category = SelectedRosterEntry?.EntryType == MugenRosterEntryType.Category
            ? SelectedRosterEntry.Category
            : SelectedRosterEntry?.Category;

        var entry = new MugenRosterEntry(
            MugenRosterEntryType.Character,
            charPath,
            null,
            category,
            null);

        InsertRosterEntry(new MugenRosterEntryViewModel(entry));
        StatusMessage = $"Added {character.DisplayName} to roster.";
    }

    private void InsertRosterEntry(MugenRosterEntryViewModel newItem)
    {
        if (SelectedRosterEntry != null)
        {
            var index = RosterEntries.IndexOf(SelectedRosterEntry);
            if (index >= 0)
            {
                RosterEntries.Insert(index + 1, newItem);
                return;
            }
        }
        RosterEntries.Add(newItem);
    }

    [RelayCommand]
    private void RemoveRosterEntry(MugenRosterEntryViewModel? entry)
    {
        if (entry == null) return;
        RosterEntries.Remove(entry);
    }

    [RelayCommand]
    private void MoveRosterEntryUp(MugenRosterEntryViewModel? entry)
    {
        if (entry == null) return;
        var index = RosterEntries.IndexOf(entry);
        if (index > 0)
        {
            RosterEntries.Move(index, index - 1);
        }
    }

    [RelayCommand]
    private void MoveRosterEntryDown(MugenRosterEntryViewModel? entry)
    {
        if (entry == null) return;
        var index = RosterEntries.IndexOf(entry);
        if (index >= 0 && index < RosterEntries.Count - 1)
        {
            RosterEntries.Move(index, index + 1);
        }
    }
}

public partial class MugenRosterEntryViewModel : ObservableObject
{
    private readonly MugenRosterEntry _original;

    [ObservableProperty]
    private string? _characterPath;

    [ObservableProperty]
    private string? _stagePath;

    [ObservableProperty]
    private string? _category;

    [ObservableProperty]
    private string? _rawLine;

    public MugenRosterEntryType EntryType { get; }
    public bool IsCategory => EntryType == MugenRosterEntryType.Category;
    public bool IsCharacter => EntryType == MugenRosterEntryType.Character;
    public bool IsComment => EntryType == MugenRosterEntryType.Comment;

    public string DisplayLabel
    {
        get
        {
            if (IsCategory) return $"[Category] {Category}";
            if (IsCharacter) return $"{CharacterPath} {(StagePath != null ? $"({StagePath})" : "")}";
            if (IsComment) return $"// {RawLine}";
            return RawLine ?? string.Empty;
        }
    }

    public MugenRosterEntryViewModel(MugenRosterEntry entry)
    {
        _original = entry;
        EntryType = entry.EntryType;
        CharacterPath = entry.CharacterPath;
        StagePath = entry.StagePath;
        Category = entry.Category;
        RawLine = entry.RawLine;
    }

    public MugenRosterEntry ToEntry()
    {
        return new MugenRosterEntry(
            EntryType,
            CharacterPath,
            StagePath,
            Category,
            RawLine);
    }
}
