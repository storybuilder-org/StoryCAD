namespace StoryCAD.Views;

public sealed partial class HomePage
{
	private Windowing WindowingVM = Ioc.Default.GetRequiredService<Windowing>();
    public HomePage() { InitializeComponent(); }
}