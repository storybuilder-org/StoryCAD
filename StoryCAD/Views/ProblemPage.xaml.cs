using StoryCADLib.Services;
using StoryCADLib.Services.Logging;
using StoryCADLib.ViewModels.SubViewModels;

namespace StoryCAD.Views;

public sealed partial class ProblemPage : Page
{
    public LogService LogService = Ioc.Default.GetService<LogService>();
    public ProblemViewModel ProblemVm;
    public BeatSheetsViewModel BeatSheetsVm;

    public ProblemPage()
    {
        ProblemVm = Ioc.Default.GetService<ProblemViewModel>();
        BeatSheetsVm = ProblemVm.BeatSheetsVm;
        InitializeComponent();
        DataContext = ProblemVm;
    }

    public ShellViewModel ShellVm => Ioc.Default.GetService<ShellViewModel>();
    public OutlineViewModel OutlineVM => Ioc.Default.GetService<OutlineViewModel>();

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        var appState = Ioc.Default.GetRequiredService<AppState>();
        appState.CurrentSaveable = DataContext as ISaveable;
    }
}
