using System.Collections.Generic;
using Windows.ApplicationModel.DataTransfer;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using StoryCAD.Services.Logging;
using StoryCAD.ViewModels.Tools;

namespace StoryCAD.Views;

public sealed partial class ProblemPage : BindablePage
{
    public ProblemViewModel ProblemVm;
    public ShellViewModel ShellVm => Ioc.Default.GetService<ShellViewModel>();
    public BeatSheetsViewModel BeatSheetsViewModel = Ioc.Default.GetService<BeatSheetsViewModel>();
    public LogService LogService = Ioc.Default.GetService<LogService>();
	public List<StoryElement> Problems = Ioc.Default.GetService<ShellViewModel>().StoryModel.StoryElements.Problems.ToList();
	public List<StoryElement> Scenes = Ioc.Default.GetService<ShellViewModel>().StoryModel.StoryElements.Scenes.ToList();
	public ProblemPage()
    {
        ProblemVm = Ioc.Default.GetService<ProblemViewModel>();
        InitializeComponent();
        DataContext = ProblemVm;
    }
	
#region DND

	/// <summary>
	/// Ran when an item is dropped on the right side of the beat panel
	/// </summary>
	private async void DroppedItem(object sender, DragEventArgs e)
	{
		var stackPanel = sender as Grid;
		if (stackPanel == null) return;

		var structureBeatsModel = stackPanel.DataContext as StructureBeatViewModel;

		// Now, you can access structureBeatsModel to know which item was dropped on
		if (structureBeatsModel != null)
		{
			try
			{
				string text = await e.DataView.GetTextAsync();
				//Check we are dragging an element GUID and not something else
				if (text.Contains("GUID"))
				{
					string guid = text.Split(":")[1];
					Guid uuid;
					Guid.TryParse(guid,out uuid);

					//Find element being dropped.
					StoryElement Element = ShellVm.StoryModel.StoryElements.First(g => g.Uuid == uuid);
					int ElementIndex = ShellVm.StoryModel.StoryElements.IndexOf(Element);
					if (Element.Type == StoryItemType.Problem)
					{
						ProblemModel problem = (ProblemModel)Element;
						//Enforce rule that problems can only be bound to one structure beat model
						if (!string.IsNullOrEmpty(problem.BoundStructure)) //Check element is actually bound elsewhere
						{
							//Show dialog asking to rebind.
							var res = await Ioc.Default.GetRequiredService<Windowing>().ShowContentDialog(new()
							{
								Title = "Already bound!",
								Content = $"This problem is already bound to a different structure ({problem.Name}) " +
								          $"Would you like to remove it from there and bind it here instead?",
								PrimaryButtonText = "Rebind here",
								SecondaryButtonText = "Don't Rebind"
							});
							
							//Do nothing if user clicks don't rebind.
							if (res != ContentDialogResult.Primary) { return; }

							if (problem.Uuid == ProblemVm.Uuid) //Rebind from VM
							{
								StructureBeatViewModel oldStructure = problem.StructureBeats.First(g => g.Guid == problem.BoundStructure);
								int index = ProblemVm.StructureBeats.IndexOf(oldStructure);
								ProblemVm.StructureBeats[index].Guid = String.Empty;
							}
							else //Remove from old structure and update story elements.
							{
								StructureBeatViewModel oldStructure = problem.StructureBeats.First(g => g.Guid == problem.BoundStructure);
								int index = problem.StructureBeats.IndexOf(oldStructure);
								problem.StructureBeats[index].Guid = String.Empty;
								ShellVm.StoryModel.StoryElements[ElementIndex] = problem;
							}
						}

						if (problem.Uuid == ProblemVm.Uuid)
						{
							ProblemVm.BoundStructure = ProblemVm.Uuid.ToString();
						}
						else
						{
							problem.BoundStructure = ProblemVm.Uuid.ToString();
							ShellVm.StoryModel.StoryElements[ElementIndex] = problem;
						}
					}
					structureBeatsModel.Guid = guid;
				}
			}
			catch (Exception ex)
			{
				LogService.Log(LogLevel.Warn, $"Failed to drag valid element (StructureDND)" +
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
	#endregion
	
	/// <summary>
	/// Deletes a beat
	/// </summary>
	public void DeleteBeat(object sender, RoutedEventArgs e)
	{
		StructureBeatViewModel model = ((sender as Button).Parent as StackPanel).DataContext as StructureBeatViewModel;
		ProblemVm.StructureBeats.Remove(model);
	}

	/// <summary>
	/// Moves a selected beat higher.
	/// </summary>
	private void MoveUp(object sender, RoutedEventArgs e)
	{
		StructureBeatViewModel model = ((sender as Button).Parent as StackPanel).DataContext as StructureBeatViewModel;
		int ModelIndex = ProblemVm.StructureBeats.IndexOf(model);

		//Sanity check
		if (ModelIndex == 0)
		{
			ShellVm.ShowMessage(LogLevel.Warn, "Can't move Beat higher", true);
			return;
		}	

		ProblemVm.StructureBeats.Move(ModelIndex, ModelIndex-1);
	}
	/// <summary>
	/// Moves a selected beat lower.
	/// </summary>
	private void MoveDown(object sender, RoutedEventArgs e)
	{
		StructureBeatViewModel model = ((sender as Button).Parent as StackPanel).DataContext as StructureBeatViewModel;
		int ModelIndex = ProblemVm.StructureBeats.IndexOf(model);

		//Sanity check
		if (ModelIndex+1 == ProblemVm.StructureBeats.Count)
		{
			ShellVm.ShowMessage(LogLevel.Warn, "Can't move Beat lower", true);
			return;
		}

		ProblemVm.StructureBeats.Move(ModelIndex, ModelIndex + 1);
	}
}