using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using StoryCADLib.Collaborator.ViewModels;
using StoryCADLib.DAL;
using StoryCADLib.Services;
using StoryCADLib.Services.Collaborator.Contracts;

namespace StoryCADLib.Collaborator.Views;

/// <summary>
///     The shell page that contains the workflow navigation menu and content frame.
///     ViewModel is created in the constructor and set as DataContext.
/// </summary>
public sealed partial class WorkflowShell : Page
{
    public WorkflowShell()
    {
        InitializeComponent();
        DataContext = new WorkflowShellViewModel();
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
            // Keep VM pane flag in sync with control default after load.
            shellVm.IsPaneOpen = NavView.IsPaneOpen;
            // Menu population is handled by Collaborator after navigation
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

    private async void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        if (ShellViewModel?.CurrentSettings == null) return;

        var settings = ShellViewModel.CurrentSettings;

        // Create settings UI
        var tersenessCombo = new ComboBox
        {
            Header = "Response Terseness",
            ItemsSource = new[] { "Concise", "Balanced", "Detailed" },
            SelectedIndex = (int)settings.Terseness,
            Width = 200,
            Margin = new Thickness(0, 0, 0, 12)
        };

        var preservationCombo = new ComboBox
        {
            Header = "Content Preservation",
            ItemsSource = new[] { "Strict", "Balanced", "Flexible" },
            SelectedIndex = (int)settings.ContentPreservation,
            Width = 200,
            Margin = new Thickness(0, 0, 0, 12)
        };

        var genreTextBox = new TextBox
        {
            Header = "Genre Preferences (comma-separated)",
            Text = settings.GenrePreferences,
            Width = 300,
            Margin = new Thickness(0, 0, 0, 12)
        };

        var likesTextBox = new TextBox
        {
            Header = "Story Forms I Like (comma-separated)",
            Text = settings.StoryFormLikes,
            Width = 300,
            Margin = new Thickness(0, 0, 0, 12)
        };

        var dislikesTextBox = new TextBox
        {
            Header = "Story Forms to Avoid (comma-separated)",
            Text = settings.StoryFormDislikes,
            Width = 300,
            Margin = new Thickness(0, 0, 0, 12)
        };

        var loggingCombo = new ComboBox
        {
            Header = "Logging Visibility",
            ItemsSource = new[] { "Off", "Basic", "Detailed (may expose prompts)" },
            SelectedIndex = (int)settings.LoggingLevel,
            Width = 250,
            Margin = new Thickness(0, 0, 0, 0)
        };

        // The only setting here that persists across sessions: it is seeded from
        // PreferencesModel.ShowCollaboratorCost when Collaborator opens and written back
        // below. The other five reset to their defaults every open.
        var showCostToggle = new CheckBox
        {
            Content = "Show cost per run on the status bar",
            IsChecked = settings.ShowCostDetails,
            Margin = new Thickness(0, 12, 0, 0)
        };

        var panel = new StackPanel
        {
            Children =
            {
                tersenessCombo,
                preservationCombo,
                genreTextBox,
                likesTextBox,
                dislikesTextBox,
                loggingCombo,
                showCostToggle
            }
        };

        var dialog = new ContentDialog
        {
            Title = "Collaborator Settings",
            Content = panel,
            PrimaryButtonText = "Save",
            CloseButtonText = "Cancel",
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            // Apply changes
            var newSettings = new CollaboratorSettings
            {
                Terseness = (TersenessLevel)tersenessCombo.SelectedIndex,
                ContentPreservation = (ContentPreservationLevel)preservationCombo.SelectedIndex,
                GenrePreferences = genreTextBox.Text,
                StoryFormLikes = likesTextBox.Text,
                StoryFormDislikes = dislikesTextBox.Text,
                LoggingLevel = (LoggingVisibility)loggingCombo.SelectedIndex,
                ShowCostDetails = showCostToggle.IsChecked == true
            };

            // Assigning CurrentSettings applies ShowCostDetails to the cost line immediately,
            // so the bar appears or disappears without reopening Collaborator.
            ShellViewModel.CurrentSettings = newSettings;
            ShellViewModel.OnSettingsChanged?.Invoke(newSettings);

            // ShowCostDetails is the one member that outlives the session. Written only when
            // it actually changed, so saving unrelated settings does not rewrite the file.
            var preferences = Ioc.Default.GetService<PreferenceService>();
            if (preferences?.Model != null &&
                preferences.Model.ShowCollaboratorCost != newSettings.ShowCostDetails)
            {
                preferences.Model.ShowCollaboratorCost = newSettings.ShowCostDetails;
                await new PreferencesIo().WritePreferences(preferences.Model);
            }
        }
    }
}
