using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using StoryCAD.Models;
using StoryCAD.Services;
using CommunityToolkit.Mvvm.DependencyInjection;

namespace StoryCAD.Views;

public sealed partial class ScenePage : Page
{
    public SceneViewModel SceneVm => Ioc.Default.GetService<SceneViewModel>();

    public ScenePage()
    {
        InitializeComponent();
        DataContext = SceneVm;
    }

    private void ScenePurpose_Checked(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        CheckBox chk = sender as CheckBox;
        StringSelection element = chk.DataContext as StringSelection;
        if (element == null)
            return;
        element.Selection = true;
        SceneVm.AddScenePurpose(element);
    }

    private void ScenePurpose_Unchecked(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        CheckBox chk = sender as CheckBox;
        StringSelection element = chk.DataContext as StringSelection;
        if (element == null)
            return;
        element.Selection = false;
        SceneVm.RemoveScenePurpose(element);
    }

    private void CastMember_Checked(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        CheckBox chk = sender as CheckBox;
        object item = chk.DataContext;
        if (item == null)
            return;
        StoryElement element = item as StoryElement;
        SceneVm.AddCastMember(element);
    }

    private void CastMember_Unchecked(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        CheckBox chk = sender as CheckBox;
        object item = chk.DataContext;
        if (item == null)
            return;
        StoryElement element = item as StoryElement;
        SceneVm.RemoveCastMember(element);
    }
    
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        var appState = Ioc.Default.GetRequiredService<AppState>();
        appState.CurrentSaveable = DataContext as ISaveable;
    }
}