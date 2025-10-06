namespace StoryCAD.Views;

public sealed partial class TrashCanPage : Page
{
    public TrashCanPage()
    {
        InitializeComponent();
        DataContext = TrashCanVm;
    }

    public TrashCanViewModel TrashCanVm => Ioc.Default.GetService<TrashCanViewModel>();
}
