using StoryCADLib.Services;

namespace StoryCAD.Views;

public sealed partial class OverviewPage : Page
{
    public OverviewPage()
    {
        InitializeComponent();
        DataContext = OverviewVm;

        // Equal-width tabs are sized on the first layout pass, before the page has its final
        // width, so a new outline showed narrow tabs until the outline was reopened. Redo the
        // tab sizing once the page has its width (#1422, 2026-11-01).
        OverviewTabView.SizeChanged += (_, _) => DispatcherQueue.TryEnqueue(
            Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
            {
                OverviewTabView.TabWidthMode = TabViewWidthMode.SizeToContent;
                OverviewTabView.TabWidthMode = TabViewWidthMode.Equal;
            });
    }

    public OverviewViewModel OverviewVm => Ioc.Default.GetService<OverviewViewModel>();

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        var appState = Ioc.Default.GetRequiredService<AppState>();
        appState.CurrentSaveable = DataContext as ISaveable;
    }
}
