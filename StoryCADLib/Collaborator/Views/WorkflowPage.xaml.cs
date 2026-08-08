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

    /// <summary>
    /// Guards against the same message being sent twice when both QuerySubmitted and the
    /// query button's Click fire for one press, and against a second press mid-send.
    /// </summary>
    private bool _isSending;

    // Fires on Enter, and on the in-box send (query) button where the platform raises it.
    private async void InputTextBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        // QueryText is the box's current text, but the query button leaves it empty on some
        // targets; fall back to the box itself so the button sends what the user typed.
        await SendCurrentAsync(string.IsNullOrWhiteSpace(args.QueryText) ? sender?.Text : args.QueryText);
    }

    /// <summary>
    /// The in-box send button does not raise QuerySubmitted on every target, so hook the
    /// templated query button directly. Both routes funnel through SendCurrentAsync.
    /// </summary>
    private void InputTextBox_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not AutoSuggestBox box)
        {
            return;
        }

        var queryButton = FindQueryButton(box);
        if (queryButton == null)
        {
            return;
        }

        queryButton.Click -= QueryButton_Click;
        queryButton.Click += QueryButton_Click;
    }

    private async void QueryButton_Click(object sender, RoutedEventArgs e)
    {
        await SendCurrentAsync(InputTextBox.Text);
    }

    private async System.Threading.Tasks.Task SendCurrentAsync(string text)
    {
        if (ViewModel == null || _isSending || string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        _isSending = true;
        try
        {
            ViewModel.InputText = text;
            await ViewModel.SendButtonClicked();
        }
        finally
        {
            _isSending = false;
        }
    }

    /// <summary>
    /// Walks the applied template for the AutoSuggestBox query button ("QueryButton" is the
    /// documented template part). Matches that name only: the inner TextBox template also
    /// carries a clear button, and hooking that one would send on clear.
    /// </summary>
    private static Button FindQueryButton(DependencyObject root)
    {
        var count = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(root, i);
            if (child is Button button && button.Name == "QueryButton")
            {
                return button;
            }

            var found = FindQueryButton(child);
            if (found != null)
            {
                return found;
            }
        }

        return null;
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
