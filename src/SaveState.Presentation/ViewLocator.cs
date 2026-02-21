namespace SaveState.Presentation;

using Avalonia.Controls;
using Avalonia.Controls.Templates;
using CommunityToolkit.Mvvm.ComponentModel;
using SaveState.Presentation.ViewModels;

/// <summary>
/// Automatically resolves Views from ViewModels using naming conventions.
/// 
/// Convention:
///   ViewModels.Library.LibraryViewModel → Views.Library.LibraryView
///   ViewModels.Dialogs.MessageDialogViewModel → Views.Dialogs.MessageDialogView
///   
/// Also handles Widgets: Services.Dashboard.Widgets.MyWidget → Views.Dashboard.Widgets.MyWidgetView
/// 
/// Registered in App.axaml as the default data template for ObservableObject types.
/// </summary>
public class ViewLocator : IDataTemplate
{
    /// <summary>
    /// Builds a View control for the given ViewModel.
    /// </summary>
    /// <param name="data">The ViewModel instance.</param>
    /// <returns>The corresponding View control, or an error TextBlock if not found.</returns>
    public Control Build(object? data)
    {
        if (data is null)
            return new TextBlock { Text = "Data is null" };

        var vmType = data.GetType();
        var fullName = vmType.FullName!;

        // Transform ViewModel name to View name using convention
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

    /// <summary>
    /// Determines if this data template can handle the given data.
    /// </summary>
    /// <param name="data">The data to check.</param>
    /// <returns>True if the data is an ObservableObject (ViewModel).</returns>
    public bool Match(object? data)
    {
        return data is ObservableObject;
    }
}
