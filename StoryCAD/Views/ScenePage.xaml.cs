using StoryCADLib.Services;

namespace StoryCAD.Views;

public sealed partial class ScenePage : Page
{
    public ScenePage()
    {
        InitializeComponent();
        DataContext = SceneVm;
    }

    public SceneViewModel SceneVm => Ioc.Default.GetService<SceneViewModel>();

    private void ScenePurpose_Checked(object sender, RoutedEventArgs e)
    {
        var chk = sender as CheckBox;
        var element = chk.DataContext as StringSelection;
        if (element == null)
        {
            return;
        }

        element.Selection = true;
        SceneVm.AddScenePurpose(element);
    }

    private void ScenePurpose_Unchecked(object sender, RoutedEventArgs e)
    {
        var chk = sender as CheckBox;
        var element = chk.DataContext as StringSelection;
        if (element == null)
        {
            return;
        }

        element.Selection = false;
        SceneVm.RemoveScenePurpose(element);
    }

    private void CastMember_Checked(object sender, RoutedEventArgs e)
    {
        var chk = sender as CheckBox;
        var item = chk.DataContext;
        if (item == null)
        {
            return;
        }

        var element = item as StoryElement;
        SceneVm.AddCastMember(element);
    }

    private void CastMember_Unchecked(object sender, RoutedEventArgs e)
    {
        var chk = sender as CheckBox;
        var item = chk.DataContext;
        if (item == null)
        {
            return;
        }

        var element = item as StoryElement;
        SceneVm.RemoveCastMember(element);
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        var appState = Ioc.Default.GetRequiredService<AppState>();
        appState.CurrentSaveable = DataContext as ISaveable;
    }
}
