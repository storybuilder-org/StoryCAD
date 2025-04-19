namespace StoryCAD.Views;

public sealed partial class FolderPage : Page
{
    public FolderViewModel FolderVm => Ioc.Default.GetService<FolderViewModel>();

    public FolderPage()
    {
        InitializeComponent();
        DataContext = FolderVm;
    }
}