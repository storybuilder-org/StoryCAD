using Windows.System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
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

    private void InputTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter && !string.IsNullOrWhiteSpace(InputTextBox.Text))
        {
            SendButton_Click(this, new RoutedEventArgs());
        }
    }

    private async void SendButton_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel != null)
        {
            await ViewModel.SendButtonClicked();
        }
    }
}
