using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using StoryCADLib.Collaborator.ViewModels;

namespace StoryCADLib.Collaborator.Views;

/// <summary>
///     The shell page that contains the workflow navigation menu and content frame.
///
///     ViewModel Injection:
///     - WorkflowShellViewModel is injected by Uno.Extensions.Navigation via DataContext
///     - Navigation framework resolves the ViewModel from DI when navigating to "Shell" route
///     - No manual service resolution needed - trust the framework
/// </summary>
public sealed partial class WorkflowShell : Page
{
    public WorkflowShell()
    {
        InitializeComponent();
        this.Loaded += WorkflowShell_Loaded;
    }

    /// <summary>
    /// ViewModel property for x:Bind support.
    /// DataContext is set by Uno Navigation framework when navigating to "Shell" route.
    /// </summary>
    public WorkflowShellViewModel ShellViewModel => DataContext as WorkflowShellViewModel;

    private void WorkflowShell_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is WorkflowShellViewModel shellVm)
        {
            shellVm.ContentFrame = StepFrame;
            shellVm.NavView = NavView;
        }
    }

    private void StepFrame_OnNavigated(object sender, NavigationEventArgs e)
    {
        // Handle frame navigation if needed
        // The frame now navigates to WorkflowPage with a specific WorkflowViewModel
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (DataContext is WorkflowShellViewModel shellVm)
        {
            shellVm.NavView_SelectionChanged(sender, args);
        }
    }
}
