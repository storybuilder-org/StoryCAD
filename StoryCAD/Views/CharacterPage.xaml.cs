namespace StoryCAD.Views;

public sealed partial class CharacterPage : BindablePage
{
    public CharacterViewModel CharVm => Ioc.Default.GetService<CharacterViewModel>();
    public CharacterPage()
    {
        InitializeComponent();
        DataContext = CharVm;
    }
}