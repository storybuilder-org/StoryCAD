using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCAD.Controls;
using StoryCAD.ViewModels;

namespace StoryCAD.Views;

public sealed partial class ProblemPage : BindablePage
{
    public ProblemViewModel ProblemVm;
    public ShellViewModel ShellVm => Ioc.Default.GetService<ShellViewModel>();


    public ProblemPage()
    {
        ProblemVm = Ioc.Default.GetService<ProblemViewModel>();
        InitializeComponent();
        DataContext = ProblemVm;
    }

    //private void Conflict_ContextRequested(UIElement sender, ContextRequestedEventArgs args)
    //{
    //    FlyoutShowOptions myOption = new();
    //    myOption.ShowMode = FlyoutShowMode.Transient;
    //    ConflictCommandBarFlyout.ShowAt(NavigationTree, myOption);
    //}
}