using Microsoft.UI.Xaml.Navigation;
using StoryCAD.Models;
using StoryCAD.Services;
using CommunityToolkit.Mvvm.DependencyInjection;

namespace StoryCAD.Views;

public sealed partial class SettingPage : Page
{
    public SettingViewModel SettingVm => Ioc.Default.GetService<SettingViewModel>();

    public SettingPage()
    {
        InitializeComponent();
        DataContext = SettingVm;
    }
    
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        var appState = Ioc.Default.GetRequiredService<AppState>();
        appState.CurrentSaveable = DataContext as ISaveable;
    }
}