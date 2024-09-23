using Microsoft.UI.Xaml;
using StoryCAD.ViewModels.Tools;

namespace StoryCAD.Views;

public sealed partial class ProblemPage : BindablePage
{
    public ProblemViewModel ProblemVm;
    public ShellViewModel ShellVm => Ioc.Default.GetService<ShellViewModel>();
    public MasterPlotsViewModel MasterPlotsViewModel = Ioc.Default.GetService<MasterPlotsViewModel>();

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
    private void DroppedItem(object sender, DragEventArgs e)
    {
	    var x = sender;
    }

    private void UIElement_OnDropCompleted(UIElement sender, DropCompletedEventArgs args)
    {
	    throw new NotImplementedException();
    }
}