using StoryCAD.Services;
using StoryCAD.Services.Logging;
using StoryCAD.ViewModels.SubViewModels;
using StoryCAD.ViewModels.Tools;

namespace StoryCAD.Views;

public sealed partial class ProblemPage : Page
{
    public BeatSheetsViewModel BeatSheetsViewModel = Ioc.Default.GetService<BeatSheetsViewModel>();
    public LogService LogService = Ioc.Default.GetService<LogService>();
    public ProblemViewModel ProblemVm;

    public ProblemPage()
    {
        ProblemVm = Ioc.Default.GetService<ProblemViewModel>();
        InitializeComponent();
        // Responsive XAML: layout made scrollable/stretchy; window enforces the only min-size.
        DataContext = ProblemVm;
    }

    public ShellViewModel ShellVm => Ioc.Default.GetService<ShellViewModel>();
    public OutlineViewModel OutlineVM => Ioc.Default.GetService<OutlineViewModel>();

    private void ExpanderSet(Expander sender, ExpanderExpandingEventArgs args)
    {
        ProblemVm.SelectedBeat = (StructureBeatViewModel)sender.DataContext;
        ProblemVm.SelectedBeatIndex = ProblemVm.StructureBeats.IndexOf(ProblemVm.SelectedBeat);
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        var appState = Ioc.Default.GetRequiredService<AppState>();
        appState.CurrentSaveable = DataContext as ISaveable;
    }
}
