namespace StoryCAD.Views;

public sealed partial class HomePage : Page
{
    private Windowing WindowingVM = Ioc.Default.GetRequiredService<Windowing>();

    public HomePage()
    {
        InitializeComponent();
    }
}
