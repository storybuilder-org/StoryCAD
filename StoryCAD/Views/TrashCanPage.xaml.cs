namespace StoryCAD.Views;

public sealed partial class TrashCanPage : Page
{
    public TrashCanViewModel TrashCanVm => Ioc.Default.GetService<TrashCanViewModel>();
    public TrashCanPage()
    {
        InitializeComponent();
        DataContext = TrashCanVm;
    }
}
