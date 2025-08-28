using Microsoft.UI.Xaml.Navigation;
using StoryCAD.Models;
using StoryCAD.Services;
using CommunityToolkit.Mvvm.DependencyInjection;

namespace StoryCAD.Views;

public sealed partial class CharacterPage : BindablePage
{
    public CharacterViewModel CharVm => Ioc.Default.GetService<CharacterViewModel>();
    public CharacterPage()
    {
        InitializeComponent();
        DataContext = CharVm;
    }
    
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        var appState = Ioc.Default.GetRequiredService<AppState>();
        appState.CurrentSaveable = DataContext as ISaveable;
    }
}