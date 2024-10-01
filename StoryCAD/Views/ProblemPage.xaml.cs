using System.Collections.Generic;
using Windows.ApplicationModel.DataTransfer;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using StoryCAD.Models.Tools;
using StoryCAD.Services.Logging;
using StoryCAD.ViewModels.Tools;

namespace StoryCAD.Views;

public sealed partial class ProblemPage : BindablePage
{
    public ProblemViewModel ProblemVm;
    public ShellViewModel ShellVm => Ioc.Default.GetService<ShellViewModel>();
    public MasterPlotsViewModel MasterPlotsViewModel = Ioc.Default.GetService<MasterPlotsViewModel>();
    public LogService LogService = Ioc.Default.GetService<LogService>();
	public List<StoryElement> Problems = Ioc.Default.GetService<ShellViewModel>().StoryModel.StoryElements.Problems.ToList();
	public List<StoryElement> Scenes = Ioc.Default.GetService<ShellViewModel>().StoryModel.StoryElements.Scenes.ToList();
	public ProblemPage()
    {
        ProblemVm = Ioc.Default.GetService<ProblemViewModel>();
        InitializeComponent();
        DataContext = ProblemVm;
    }

	/// <summary>
	/// Ran when an item is dropped on the right side of the beat panel
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="e"></param>
    private async void DroppedItem(object sender, DragEventArgs e)
    {
	    var stackPanel = sender as Grid;
	    if (stackPanel == null) return;

	    var structureBeatsModel = stackPanel.DataContext as StructureBeatsModel;

	    // Now, you can access structureBeatsModel to know which item was dropped on
	    if (structureBeatsModel != null)
	    {
		    try
		    {
			    string text = await e.DataView.GetTextAsync();
			    //Check we are dragging an element GUID and not something else
				if (text.Contains("GUID"))
				{
					structureBeatsModel.Guid = text.Split(":")[1];
				}
			}
			catch (Exception ex)
		    {
			    LogService.Log(LogLevel.Warn,$"Failed to drag valid element (StructureDND)" +
			                                 $" (This is expected if non element object was DND) {ex.Message}");
			}
		}
    }

    private void UIElement_OnDragOver(object sender, DragEventArgs e)
    {
	    e.AcceptedOperation = DataPackageOperation.Move;
	    e.Handled = true;
    }

    private void ListViewBase_OnDragItemsStarting(object sender, DragItemsStartingEventArgs e)
    {
	    if (e.Items.Count > 0)
	    {
		    var draggedStoryElement = e.Items[0] as StoryElement; // Cast to StoryElement
		    if (draggedStoryElement != null)
		    {
			    // Set the StoryElement object as part of the drag data
			    e.Data.SetText("GUID:" + draggedStoryElement.Uuid.ToString());
			    e.Data.RequestedOperation = DataPackageOperation.Move;
		    }
	    }
	}

    private async void UpdateSelectedBeat(object sender, SelectionChangedEventArgs e)
    {
		await Ioc.Default.GetService<Windowing>().ShowContentDialog(new ContentDialog
		{
			Title = "This will clear selected story beats",
			PrimaryButtonText = "Confirm",
			SecondaryButtonText = "Cancel"
		});

	    ProblemVm.StructureBeats.Clear();
	    foreach (var item in ProblemVm.StructureModel.MasterPlotScenes)
	    {
			ProblemVm.StructureBeats.Add(new StructureBeatsModel
			{
				Title = item.SceneTitle,
				Description = item.Notes,
			});
		}
    }

    private void AddBeat(object sender, RoutedEventArgs e)
    {
		//Add beat
		ProblemVm.StructureBeats.Add(new StructureBeatsModel
		{
			Title = ProblemVm.AddBeat_Name,
			Description = ProblemVm.AddBeat_Description,
			Guid = ""
		});

		//Reset boxes.
		ProblemVm.AddBeat_Name = "";
		ProblemVm.AddBeat_Description = "";
	}
}