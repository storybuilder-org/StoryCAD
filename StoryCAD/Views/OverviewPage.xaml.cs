namespace StoryCAD.Views;

public sealed partial class OverviewPage : Page
{

    public OverviewViewModel OverviewVm => Ioc.Default.GetService<OverviewViewModel>();
    public ShellViewModel ShellVM => Ioc.Default.GetService<ShellViewModel>();

    public OverviewPage()
    {
        InitializeComponent();
        DataContext = OverviewVm;
    }
}