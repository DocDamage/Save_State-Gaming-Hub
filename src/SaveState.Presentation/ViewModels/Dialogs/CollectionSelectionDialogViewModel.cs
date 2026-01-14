using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Core.GameLibrary.Entities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace SaveState.Presentation.ViewModels.Dialogs;

public partial class CollectionSelectionDialogViewModel : ObservableObject
{
    private readonly List<CollectionSelectionOption> _allCollections;
    private Action<CollectionSelectionResult?>? _closeAction;

    [ObservableProperty]
    private ObservableCollection<CollectionSelectionOption> _collections = new();

    [ObservableProperty]
    private CollectionSelectionOption? _selectedCollection;

    [ObservableProperty]
    private string _filterText = string.Empty;

    public string Title { get; }

    public CollectionSelectionDialogViewModel(
        IEnumerable<CollectionSelectionOption> options,
        Guid? currentSelectionId = null,
        string? title = null)
    {
        Title = title ?? "Select Collection";
        _allCollections = options.ToList();
        ApplyFilter();

        if (currentSelectionId.HasValue)
        {
            SelectedCollection = Collections.FirstOrDefault(c => c.Id == currentSelectionId.Value);
        }
    }

    public void SetCloseAction(Action<CollectionSelectionResult?> closeAction)
    {
        _closeAction = closeAction;
    }

    partial void OnFilterTextChanged(string value)
    {
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        IEnumerable<CollectionSelectionOption> filtered = _allCollections;

        if (!string.IsNullOrWhiteSpace(FilterText))
        {
            filtered = filtered.Where(option =>
                option.Name.Contains(FilterText, StringComparison.OrdinalIgnoreCase) ||
                option.TypeLabel.Contains(FilterText, StringComparison.OrdinalIgnoreCase));
        }

        Collections = new ObservableCollection<CollectionSelectionOption>(filtered);
    }

    [RelayCommand]
    private void Confirm()
    {
        if (SelectedCollection != null)
        {
            _closeAction?.Invoke(new CollectionSelectionResult(SelectedCollection.Id, SelectedCollection.Name));
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        _closeAction?.Invoke(null);
    }
}

public record CollectionSelectionOption(Guid Id, string Name, CollectionType Type)
{
    public string TypeLabel => Type.ToString();
}

public record CollectionSelectionResult(Guid CollectionId, string CollectionName);
