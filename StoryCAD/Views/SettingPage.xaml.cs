namespace StoryCAD.Views;

public sealed partial class SettingPage : Page
{
    public SettingViewModel SettingVm => Ioc.Default.GetService<SettingViewModel>();

    public SettingPage()
    {
        InitializeComponent();
        DataContext = SettingVm;
    }
}