using StoryCADLib.Models.StoryWorld;
using StoryCADLib.Services;

namespace StoryCAD.Views;

public sealed partial class StoryWorldPage : Page
{
    public StoryWorldPage()
    {
        InitializeComponent();
        DataContext = StoryWorldVm;
    }

    public StoryWorldViewModel StoryWorldVm => Ioc.Default.GetService<StoryWorldViewModel>();

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        var appState = Ioc.Default.GetRequiredService<AppState>();
        appState.CurrentSaveable = DataContext as ISaveable;
    }

    private void WorldTabView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Update the ViewModel with the selected tab index for context-sensitive buttons
        if (sender is TabView tabView)
        {
            StoryWorldVm.SelectedTabIndex = tabView.SelectedIndex;
        }
    }

    #region Remove Entry Handlers

    private async void RemovePhysicalWorld_Click(object sender, RoutedEventArgs e)
    {
        var entry = (sender as FrameworkElement)?.DataContext as PhysicalWorldEntry;
        if (entry == null) return;

        ContentDialog cd = new()
        {
            Title = "Remove World?",
            Content = $"Are you sure you want to remove '{entry.Name}'?",
            PrimaryButtonText = "Yes",
            SecondaryButtonText = "No"
        };
        var result = await Ioc.Default.GetRequiredService<Windowing>().ShowContentDialog(cd);
        if (result == ContentDialogResult.Primary)
        {
            StoryWorldVm.RemovePhysicalWorld(entry);
        }
    }

    private async void RemoveSpecies_Click(object sender, RoutedEventArgs e)
    {
        var entry = (sender as FrameworkElement)?.DataContext as SpeciesEntry;
        if (entry == null) return;

        ContentDialog cd = new()
        {
            Title = "Remove Species?",
            Content = $"Are you sure you want to remove '{entry.Name}'?",
            PrimaryButtonText = "Yes",
            SecondaryButtonText = "No"
        };
        var result = await Ioc.Default.GetRequiredService<Windowing>().ShowContentDialog(cd);
        if (result == ContentDialogResult.Primary)
        {
            StoryWorldVm.RemoveSpecies(entry);
        }
    }

    private async void RemoveCulture_Click(object sender, RoutedEventArgs e)
    {
        var entry = (sender as FrameworkElement)?.DataContext as CultureEntry;
        if (entry == null) return;

        ContentDialog cd = new()
        {
            Title = "Remove Culture?",
            Content = $"Are you sure you want to remove '{entry.Name}'?",
            PrimaryButtonText = "Yes",
            SecondaryButtonText = "No"
        };
        var result = await Ioc.Default.GetRequiredService<Windowing>().ShowContentDialog(cd);
        if (result == ContentDialogResult.Primary)
        {
            StoryWorldVm.RemoveCulture(entry);
        }
    }

    private async void RemoveGovernment_Click(object sender, RoutedEventArgs e)
    {
        var entry = (sender as FrameworkElement)?.DataContext as GovernmentEntry;
        if (entry == null) return;

        ContentDialog cd = new()
        {
            Title = "Remove Government?",
            Content = $"Are you sure you want to remove '{entry.Name}'?",
            PrimaryButtonText = "Yes",
            SecondaryButtonText = "No"
        };
        var result = await Ioc.Default.GetRequiredService<Windowing>().ShowContentDialog(cd);
        if (result == ContentDialogResult.Primary)
        {
            StoryWorldVm.RemoveGovernment(entry);
        }
    }

    private async void RemoveReligion_Click(object sender, RoutedEventArgs e)
    {
        var entry = (sender as FrameworkElement)?.DataContext as ReligionEntry;
        if (entry == null) return;

        ContentDialog cd = new()
        {
            Title = "Remove Religion?",
            Content = $"Are you sure you want to remove '{entry.Name}'?",
            PrimaryButtonText = "Yes",
            SecondaryButtonText = "No"
        };
        var result = await Ioc.Default.GetRequiredService<Windowing>().ShowContentDialog(cd);
        if (result == ContentDialogResult.Primary)
        {
            StoryWorldVm.RemoveReligion(entry);
        }
    }

    #endregion
}
