using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using StoryCADLib.Collaborator.ViewModels;

namespace StoryCADLib.Collaborator.Views;

/// <summary>
/// Page for displaying and executing a specific workflow.
///
/// BINDING PATTERN:
/// - Public ViewModel property exposes DataContext as WorkflowViewModel
/// - XAML uses {x:Bind ViewModel.Property, Mode=OneWay} for compile-time binding
/// - DataContext is set by Uno Navigation automatically, or via OnNavigatedTo fallback
///
/// NOTE: x:DataType at Page level is NOT supported on Skia/Desktop targets.
/// Instead, expose a public ViewModel property and bind to ViewModel.Property.
/// </summary>
public sealed partial class WorkflowPage : Page
{
    public WorkflowPage()
    {
        InitializeComponent();
        DataContext = new WorkflowViewModel();
    }

    /// <summary>
    /// Get the ViewModel from DataContext.
    /// Must be public for x:Bind to access it from XAML.
    /// </summary>
    public WorkflowViewModel ViewModel => DataContext as WorkflowViewModel;

    /// <summary>
    /// Called when navigating to this page.
    /// Uno Navigation sets DataContext (ViewModel) automatically.
    /// Extract WorkflowModel from navigation data and initialize ViewModel.
    /// </summary>
    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (ViewModel != null)
        {
            await ViewModel.InitializeAsync(e.Parameter);
        }
    }

    // Fires on Enter and on the in-box send (query) button alike.
    private async void InputTextBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (ViewModel == null)
        {
            return;
        }

        // QueryText is the box's current text; push it in case the TwoWay binding hasn't yet.
        ViewModel.InputText = args.QueryText;
        await ViewModel.SendButtonClicked();
    }

    // Per-row accept/skip ticks (#129): row item comes from the button's DataContext.
    private void AcceptItemButton_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is StoryCADLib.Collaborator.Models.PendingUpdateItem item)
        {
            ViewModel?.AcceptItem(item.Key);
        }
    }

    private void SkipItemButton_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is StoryCADLib.Collaborator.Models.PendingUpdateItem item)
        {
            ViewModel?.SkipItem(item.Key);
        }
    }
}
