using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System;

namespace SaveState.Presentation.ViewModels.Overlays;

public partial class MemoryMonitorOverlayViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = "Memory Monitor";

    [ObservableProperty]
    private bool _isVisible = true;

    [ObservableProperty]
    private ObservableCollection<MemoryAddressViewModel> _watchedAddresses = new();

    public MemoryMonitorOverlayViewModel()
    {
        // Sample data
        WatchedAddresses.Add(new MemoryAddressViewModel("HP", "0x00FF3420", "100", "int32"));
        WatchedAddresses.Add(new MemoryAddressViewModel("MP", "0x00FF3424", "50", "int32"));
        WatchedAddresses.Add(new MemoryAddressViewModel("Lives", "0x00FF3428", "3", "byte"));
    }

    [RelayCommand]
    private void Close()
    {
        IsVisible = false;
        // Logic to notify parent to remove this overlay
    }

    [RelayCommand]
    private void AddAddress()
    {
        WatchedAddresses.Add(new MemoryAddressViewModel("New Address", "0x00000000", "0", "byte"));
    }
}

public partial class MemoryAddressViewModel : ObservableObject
{
    [ObservableProperty]
    private string _label;

    [ObservableProperty]
    private string _address;

    [ObservableProperty]
    private string _value;

    [ObservableProperty]
    private string _type;

    [ObservableProperty]
    private bool _isFrozen;

    public MemoryAddressViewModel(string label, string address, string value, string type)
    {
        Label = label;
        Address = address;
        Value = value;
        Type = type;
    }

    [RelayCommand]
    private void ToggleFreeze()
    {
        IsFrozen = !IsFrozen;
    }
}
