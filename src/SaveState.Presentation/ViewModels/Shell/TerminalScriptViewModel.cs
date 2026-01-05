using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;

namespace SaveState.Presentation.ViewModels.Shell;

public partial class TerminalScriptViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _content = string.Empty;

    [ObservableProperty]
    private string _filePath = string.Empty;

    public TerminalScriptViewModel(string name, string content, string filePath)
    {
        Name = name;
        Content = content;
        FilePath = filePath;
    }
}
