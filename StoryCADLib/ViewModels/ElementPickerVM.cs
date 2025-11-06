using StoryCADLib.Services.Dialogs;

namespace StoryCADLib.ViewModels;

/// <summary>
///     ViewModel for the Element Picker
/// </summary>
public class ElementPickerVM
{
    /// <summary>
    ///     Instance of element picker
    /// </summary>
    private ContentDialog dialog;

    /// <summary>
    ///     Currently selected item
    /// </summary>
    public StoryModel StoryModel { get; set; }

    /// <summary>
    ///     Currently selected item
    /// </summary>
    public object SelectedType { get; set; }

    /// <summary>
    ///     Currently selected item
    /// </summary>
    public object SelectedElement { get; set; }

    /// <summary>
    ///     Text for new node textbox.
    /// </summary>
    public string NewNodeName { get; set; }

    /// <summary>
    ///     Type the picker forces the user to pick
    /// </summary>
    public StoryItemType? ForcedType { get; set; }

    /// <summary>
    ///     Spawns an instance of the picker.
    /// </summary>
    /// <param name="Model">StoryModel to show elements from</param>
    /// <param name="XAMLRoot">Only allow the value of Type to be picked.</param>
    /// <param name="Type">Only allow the value of Type to be picked.</param>
    /// <returns>The GUID of element the user picked</returns>
    public async Task<string> ShowPicker(StoryModel Model,
        XamlRoot XAMLRoot, StoryItemType? Type = null)
    {
        //Reset VM
        SelectedType = null;
        SelectedElement = null;
        NewNodeName = "";
        ForcedType = Type;
        StoryModel = Model;

        //Spawn new picker
        ElementPicker UI = new();


        //create and show dialog
        dialog = new ContentDialog
        {
            Title = $"Select a {Type.ToString()} element",
            PrimaryButtonText = "Select",
            SecondaryButtonText = "Cancel",
            Content = UI,
            XamlRoot = XAMLRoot
        };

        //interpret result
        if (await dialog.ShowAsync() != ContentDialogResult.Secondary)
        {
            return (SelectedElement as StoryElement).Uuid.ToString();
        }

        return null; //Return unknown if dialog is closed or element isn't selected.
    }

    /// <summary>
    ///     Creates a new node of the type the user selected
    /// </summary>
    public void CreateNode()
    {
        /*
            StoryElement NewElement;
            StoryItemType type;
            if (ForcedType == null)
            {
                //Get elements
                ComboBoxItem Type = SelectedType as ComboBoxItem;
                type = Enum.Parse<StoryItemType>(Type.Content.ToString()!,
                    true);
            }
            else
            {
                type = (StoryItemType)ForcedType;
            }


            throw new NotImplementedException("This code needs to updated.");
            /*
            switch (type)
            {
                case StoryItemType.Problem:
                    NewElement = new ProblemModel(NewNodeName, StoryModel,);
                    break;
                case StoryItemType.Character:
                    NewElement = new CharacterModel(NewNodeName, StoryModel);
                    break;
                case StoryItemType.Setting:
                    NewElement = new SettingModel(NewNodeName, StoryModel);
                    break;
                case StoryItemType.Scene:
                    NewElement = new SceneModel(NewNodeName, StoryModel);
                    break;
                default:
                    //Throw an exception if we are asked to create a node type we don't expect
                    throw new Exception(
                        $"Unexpected element type {type}");
            }

            //Persist node to tree and set as selected element
            StoryNodeItem Node = new(NewElement, StoryModel.ExplorerView[0]);
            SelectedElement = NewElement;

            //Close popup
            if (dialog is not null)
                dialog.Hide();*/
    }
}
