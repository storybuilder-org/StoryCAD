using System.Collections.Generic;
using Windows.ApplicationModel.DataTransfer;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using StoryCAD.ViewModels.Tools;

namespace StoryCAD.Views;

public sealed partial class ProblemPage : BindablePage
{
    public ProblemViewModel ProblemVm;
    public ShellViewModel ShellVm => Ioc.Default.GetService<ShellViewModel>();
    public MasterPlotsViewModel MasterPlotsViewModel = Ioc.Default.GetService<MasterPlotsViewModel>();
	public List<StoryElement> Problems = Ioc.Default.GetService<ShellViewModel>().StoryModel.StoryElements.Problems.ToList();
	public List<StoryElement> Scenes = Ioc.Default.GetService<ShellViewModel>().StoryModel.StoryElements.Scenes.ToList();
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
    private async void DroppedItem(object sender, DragEventArgs e)
    {
	    var x = await e.DataView.GetTextAsync();
    }

    private void UIElement_OnDragOver(object sender, DragEventArgs e)
    {
	    e.AcceptedOperation = DataPackageOperation.Move;
	    e.Handled = true;
    }

    private void ListViewBase_OnDragItemsStarting(object sender, DragItemsStartingEventArgs e)
    {
	    var listView = sender as ListView;
	    if (e.Items.Count > 0)
	    {
		    var draggedStoryElement = e.Items[0] as StoryElement; // Cast to StoryElement
		    if (draggedStoryElement != null)
		    {
			    // Set the StoryElement object as part of the drag data
			    e.Data.SetText(draggedStoryElement.Uuid.ToString());
			    e.Data.RequestedOperation = DataPackageOperation.Move;
		    }
	    }
	}
}