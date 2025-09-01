using Windows.ApplicationModel.DataTransfer;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using StoryCAD.Models;
using StoryCAD.Services;
using StoryCAD.Services.Logging;
using StoryCAD.ViewModels.SubViewModels;
using StoryCAD.ViewModels.Tools;

namespace StoryCAD.Views;

public sealed partial class ProblemPage : Page
{
    public ProblemViewModel ProblemVm;
    public ShellViewModel ShellVm => Ioc.Default.GetService<ShellViewModel>();
    public OutlineViewModel OutlineVM => Ioc.Default.GetService<OutlineViewModel>();
    public BeatSheetsViewModel BeatSheetsViewModel = Ioc.Default.GetService<BeatSheetsViewModel>();
    public LogService LogService = Ioc.Default.GetService<LogService>();
    public ProblemPage()
    {
        ProblemVm = Ioc.Default.GetService<ProblemViewModel>();
        InitializeComponent();
        DataContext = ProblemVm;
    }

    private void ExpanderSet(Expander sender, ExpanderExpandingEventArgs args)
    {
        ProblemVm.SelectedBeat = (StructureBeatViewModel)(sender as Expander).DataContext;
        ProblemVm.SelectedBeatIndex = ProblemVm.StructureBeats.IndexOf(ProblemVm.SelectedBeat);
    }
    
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        var appState = Ioc.Default.GetRequiredService<AppState>();
        appState.CurrentSaveable = DataContext as ISaveable;
    }
}
