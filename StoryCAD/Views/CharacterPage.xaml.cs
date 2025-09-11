using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using StoryCAD.Models;
using StoryCAD.Services;
using StoryCAD.Services.Logging;
using StoryCAD.ViewModels.SubViewModels;
using StoryCAD.ViewModels.Tools;
using CommunityToolkit.Mvvm.DependencyInjection;

namespace StoryCAD.Views;

// Note: XAML updated for responsive layout; code-behind unchanged.
public sealed partial class CharacterPage : Page
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
