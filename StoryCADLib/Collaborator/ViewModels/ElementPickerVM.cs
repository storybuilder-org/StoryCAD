using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using StoryCADLib.Models;
using StoryCADLib.Services.Collaborator.Contracts;

namespace StoryCADLib.Collaborator.ViewModels;

/// <summary>
///     ViewModel for the Element Picker
/// </summary>
[Microsoft.UI.Xaml.Data.Bindable]
public class ElementPickerVM
{
    /// <summary>
    ///     Instance of element picker
    /// </summary>
    private ContentDialog dialog;

    private IStoryCADAPI? _storyApi;

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
    ///     Descriptive label for what we're picking (e.g., "Protagonist", "Story Problem")
    /// </summary>
    public string PickerLabel { get; set; }

    /// <summary>
    ///     GUID of the currently selected element (for pre-selection when changing)
    /// </summary>
    public Guid? CurrentSelection { get; set; }

    /// <summary>
    ///     Spawns an instance of the picker.
    /// </summary>
    /// <param name="Model">StoryModel to show elements from</param>
    /// <param name="XAMLRoot">XamlRoot for the dialog</param>
    /// <param name="Type">Only allow elements of this type to be picked</param>
    /// <param name="label">Descriptive label for what we're picking (e.g., "Protagonist")</param>
    /// <param name="currentSelection">GUID of currently selected element for pre-selection</param>
    /// <returns>The GUID of element the user picked</returns>
    public async Task<string> ShowPicker(StoryModel Model,
        XamlRoot XAMLRoot, StoryItemType? Type = null, string label = null, Guid? currentSelection = null,
        IStoryCADAPI? storyApi = null)
    {
        //Reset VM
        SelectedType = null;
        SelectedElement = null;
        NewNodeName = "";
        ForcedType = Type;
        StoryModel = Model;
        PickerLabel = label;
        CurrentSelection = currentSelection;
        _storyApi = storyApi;

        //Spawn new picker, passing this VM so Page uses the same instance
        var ui = new Views.ElementPicker(this);

        // Build dialog title - "Change" if there's a current selection, "Select" otherwise
        var hasCurrentSelection = currentSelection.HasValue && currentSelection.Value != Guid.Empty;
        var actionVerb = hasCurrentSelection ? "Change" : "Select";
        var title = !string.IsNullOrEmpty(label)
            ? $"{actionVerb} {label}"
            : $"{actionVerb} {Type.ToString()} element";

        //create and show dialog
        dialog = new ContentDialog
        {
            Title = title,
            PrimaryButtonText = "Select",
            SecondaryButtonText = "Cancel",
            Content = ui,
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
        if (_storyApi == null || StoryModel == null)
            return;

        StoryItemType type;
        if (ForcedType != null)
        {
            type = (StoryItemType)ForcedType;
        }
        else
        {
            var comboItem = SelectedType as ComboBoxItem;
            if (comboItem == null) return;
            type = Enum.Parse<StoryItemType>(comboItem.Content.ToString()!, true);
        }

        var overview = StoryModel.StoryElements
            .FirstOrDefault(e => e.ElementType == StoryItemType.StoryOverview);
        if (overview == null) return;

        var name = string.IsNullOrWhiteSpace(NewNodeName) ? $"New {type}" : NewNodeName;
        var addResult = _storyApi.AddElement(type, overview.Uuid.ToString(), name);
        if (!addResult.IsSuccess) return;

        var lookupResult = _storyApi.GetStoryElement(addResult.Payload);
        if (lookupResult?.IsSuccess == true && lookupResult.Payload != null)
        {
            SelectedElement = lookupResult.Payload;
            dialog?.Hide();
        }
    }
}
