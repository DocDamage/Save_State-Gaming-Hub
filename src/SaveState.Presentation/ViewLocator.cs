namespace SaveState.Presentation;

using Avalonia.Controls;
using Avalonia.Controls.Templates;
using CommunityToolkit.Mvvm.ComponentModel;
using SaveState.Presentation.ViewModels;

public class ViewLocator : IDataTemplate
{
    public Control Build(object? data)
    {
        if (data is null)
            return new TextBlock { Text = "Data is null" };

        var vmType = data.GetType();
        var fullName = vmType.FullName!;

        string viewName;
        // Handle standard ViewModels
        if (fullName.Contains("ViewModels."))
        {
            viewName = fullName.Replace("ViewModels", "Views").Replace("ViewModel", "View");
        }
        // Handle Widgets (Services.Dashboard.Widgets -> Views.Dashboard.Widgets)
        else if (fullName.Contains("Services.Dashboard.Widgets"))
        {
            viewName = fullName.Replace("Services.Dashboard.Widgets", "Views.Dashboard.Widgets") + "View";
        }
        else
        {
            viewName = fullName.Replace("ViewModel", "View");
        }

        var type = vmType.Assembly.GetType(viewName);

        if (type != null)
        {
            try
            {
                return (Control)Activator.CreateInstance(type)!;
            }
            catch (Exception ex)
            {
                return new TextBlock { Text = $"Failed to create {viewName}: {ex.Message}" };
            }
        }

        return new TextBlock {
            Text = $"Not Found: {viewName}\nVM Type: {fullName}",
            Foreground = Avalonia.Media.Brushes.Red
        };
    }

    public bool Match(object? data)
    {
        return data is ObservableObject;
    }
}
