using Microsoft.UI.Xaml.Navigation;
using StoryCAD.Models;
using StoryCAD.Services;
using CommunityToolkit.Mvvm.DependencyInjection;

namespace StoryCAD.Views;

public sealed partial class FolderPage : BindablePage
{
    public FolderViewModel FolderVm => Ioc.Default.GetService<FolderViewModel>();

    public FolderPage()
    {
        InitializeComponent();
        DataContext = FolderVm;
    }
    
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        var appState = Ioc.Default.GetRequiredService<AppState>();
        appState.CurrentSaveable = DataContext as ISaveable;
    }
}