using StoryCAD.Services.Dialogs;

namespace StoryCAD.ViewModels;

/// <summary>
/// ViewModel for the Element Picker
/// </summary>
class ElementPickerVM
{
	/// <summary>
	/// Currently selected item
	/// </summary>
	public object SelectedType { get; set; }

	/// <summary>
	/// Currently selected item
	/// </summary>
	public object SelectedElement { get; set; }

	/// <summary>
	/// Text for new node textbox.
	/// </summary>
	public string NewNodeName { get; set; }

	/// <summary>
	/// Instance of element picker
	/// </summary>
	private ContentDialog dialog;
	/// <summary>
	/// Spawns an instance of the picker.
	/// </summary>
	/// <returns>The element the user picked</returns>
	public async Task<StoryElement?> ShowPicker()
	{
		//Spawn new picker and reset VM
		ElementPicker UI = new();
		SelectedType = null;
		SelectedElement = null;
		NewNodeName = "";

		//create and show dialog
		dialog = new()
		{
			Title = "Select an element",
			PrimaryButtonText = "Select",
			SecondaryButtonText = "Cancel",
			Content = UI,

		};
		var res = await Ioc.Default.GetRequiredService<Windowing>()
			.ShowContentDialog(dialog, true);

		//interpret result
		if (res != ContentDialogResult.Secondary)
		{
			ComboBoxItem Type = SelectedType as ComboBoxItem;
			if (Type != null) //check user has picked an item
			{
				//The name can be parsed to an enum.
				return SelectedElement as StoryElement;
			}
		}
		
		return null; //Return unknown if dialog is closed or element isn't selected.
	}

	/// <summary>
	/// Creates a new node of the type the user selected
	/// </summary>
	public void CreateNode()
	{
		StoryElement NewElement;
		string type = (SelectedType as ComboBoxItem).Content.ToString();
		switch (type)
		{
			case "Problem":
				NewElement = new ProblemModel(NewNodeName, ShellViewModel.GetModel()); 
				break;
			case "Character":
				NewElement = new CharacterModel(NewNodeName, ShellViewModel.GetModel());
				break;
			case "Setting":
				NewElement = new SettingModel(NewNodeName, ShellViewModel.GetModel());
				break;
			case "Scene":
				NewElement = new SceneModel(NewNodeName, ShellViewModel.GetModel());
				break;
			default:
				//Throw an exception if we are asked to create a node type we don't expect
				throw new Exception(
					$"Unexpected element type {type}");
			break;
		}
		
		//Persist node to tree and set as selected element
		StoryNodeItem Node = new(NewElement, ShellViewModel.GetModel().ExplorerView[0]);
		SelectedElement = NewElement;

		//Close popup
		dialog.Hide();
	}	
}