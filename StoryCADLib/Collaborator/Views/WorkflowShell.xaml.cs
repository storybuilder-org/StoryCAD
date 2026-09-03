using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
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

    /// <summary>
    ///     Lets the user pick which workflows sit in the pane's starred band. The pane opens on
    ///     gaps plus stars; everything else waits in collapsed element-type groups, so this
    ///     dialog is how a writer shapes that short list without hunting for each row's star.
    /// </summary>
    private async void CustomizeWorkflowsButton_Click(object sender, RoutedEventArgs e)
    {
        if (ShellViewModel == null || ShellViewModel.StarEntries.Count == 0) return;

        // Checkbox per workflow, sectioned by element type. Entries arrive in registry order,
        // which is already grouped, so a header goes in wherever the group name changes.
        var panel = new StackPanel { Spacing = 2 };
        var checkBoxes = new List<(CheckBox Box, string Label)>();
        var currentGroup = string.Empty;

        foreach (var entry in ShellViewModel.StarEntries)
        {
            if (!string.Equals(entry.GroupTitle, currentGroup, StringComparison.Ordinal))
            {
                currentGroup = entry.GroupTitle;
                panel.Children.Add(new TextBlock
                {
                    Text = currentGroup,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    Margin = new Thickness(0, panel.Children.Count == 0 ? 0 : 12, 0, 4)
                });
            }

            var box = new CheckBox
            {
                IsChecked = entry.IsStarred,
                MinWidth = 0,
                Content = new StackPanel
                {
                    Children =
                    {
                        new TextBlock { Text = entry.Title, TextWrapping = TextWrapping.Wrap },
                        new TextBlock
                        {
                            Text = entry.Description,
                            TextWrapping = TextWrapping.Wrap,
                            Opacity = 0.7,
                            FontSize = 12
                        }
                    }
                }
            };
            AutomationProperties.SetName(box, entry.Title);

            checkBoxes.Add((box, entry.Label));
            panel.Children.Add(box);
        }

        var dialog = new ContentDialog
        {
            Title = "Customize Workflows",
            Content = new ScrollViewer
            {
                Content = panel,
                MaxHeight = 480,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            },
            PrimaryButtonText = "Save",
            CloseButtonText = "Cancel",
            XamlRoot = this.XamlRoot
        };

        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;

        var starred = checkBoxes
            .Where(c => c.Box.IsChecked == true)
            .Select(c => c.Label)
            .ToList();

        if (ShellViewModel.OnStarsChanged != null)
            await ShellViewModel.OnStarsChanged(starred);
    }

    /// <summary>
    ///     Opens the Collaborator section of the user manual in the default browser.
    ///     The host comes from <see cref="AppState.ManualBaseUrl"/>, which follows the
    ///     UseBetaDocumentation preference, so a beta tester lands on the beta manual and
    ///     everyone else on production.
    ///
    ///     The trailing slash is load-bearing: the section's landing page is index.html and
    ///     the bare folder URL is what serves it (StoryCAD #1514). Naming a page here instead
    ///     would pin the link to one topic and go stale as the section is reorganized.
    /// </summary>
    private void ManualLink_Click(object sender, RoutedEventArgs e)
    {
        var appState = Ioc.Default.GetService<AppState>();
        if (appState == null) return;

        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = new Uri(new Uri(appState.ManualBaseUrl), "docs/StoryCAD%20Collaborator/").ToString(),
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            // A browser that will not launch is not worth interrupting a writing session
            // over: the flyout the link sits in already answers the common questions.
            Ioc.Default.GetService<LogService>()?
                .LogException(LogLevel.Warn, ex, "Could not open the Collaborator manual section");
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

        // Persists across sessions with Terseness (Collaborator #49): seeded from
        // PreferencesModel when Collaborator opens and written back below. The other
        // three reset to their defaults every open.
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

            // ShowCostDetails and Terseness outlive the session. Written only when one of
            // them actually changed, so saving unrelated settings does not rewrite the file.
            var preferences = Ioc.Default.GetService<PreferenceService>();
            if (preferences?.Model != null)
            {
                var changed = false;
                if (preferences.Model.ShowCollaboratorCost != newSettings.ShowCostDetails)
                {
                    preferences.Model.ShowCollaboratorCost = newSettings.ShowCostDetails;
                    changed = true;
                }
                if (preferences.Model.CollaboratorTerseness != newSettings.Terseness)
                {
                    preferences.Model.CollaboratorTerseness = newSettings.Terseness;
                    changed = true;
                }
                if (changed)
                    await new PreferencesIo().WritePreferences(preferences.Model);
            }
        }
    }
}
