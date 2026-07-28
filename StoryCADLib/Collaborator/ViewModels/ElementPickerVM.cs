using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using StoryCADLib.Models;
using StoryCADLib.Services.Collaborator.Contracts;

namespace StoryCADLib.Collaborator.ViewModels;

/// <summary>
/// ViewModel for the Collaborator Element Picker dialog.
/// </summary>
[Microsoft.UI.Xaml.Data.Bindable]
public class ElementPickerVM
{
    private ContentDialog dialog;

    public StoryModel StoryModel { get; set; }
    public object SelectedType { get; set; }
    public object SelectedElement { get; set; }
    public string NewNodeName { get; set; }
    public StoryItemType? ForcedType { get; set; }
    public string PickerLabel { get; set; }
    public Guid? CurrentSelection { get; set; }

    /// <summary>
    /// API used to create new elements. Set by <see cref="ShowPicker"/>.
    /// </summary>
    public IStoryCADAPI StoryApi { get; set; }

    /// <summary>
    /// Page rebuilds the list after create (ItemsSource is a ToList snapshot).
    /// </summary>
    public Action AfterCreate { get; set; }

    /// <summary>
    /// Shows the picker. Returns selected element GUID, or null if cancelled / nothing selected.
    /// </summary>
    public async Task<string> ShowPicker(
        StoryModel Model,
        XamlRoot XAMLRoot,
        StoryItemType? Type = null,
        string label = null,
        Guid? currentSelection = null,
        IStoryCADAPI storyApi = null)
    {
        SelectedType = null;
        SelectedElement = null;
        NewNodeName = "";
        ForcedType = Type;
        StoryModel = Model;
        PickerLabel = label;
        CurrentSelection = currentSelection;
        StoryApi = storyApi;

        var ui = new Views.ElementPicker(this);

        var hasCurrentSelection = currentSelection.HasValue && currentSelection.Value != Guid.Empty;
        var actionVerb = hasCurrentSelection ? "Change" : "Select";
        var title = !string.IsNullOrEmpty(label)
            ? $"{actionVerb} {label}"
            : $"{actionVerb} {Type} element";

        dialog = new ContentDialog
        {
            Title = title,
            PrimaryButtonText = "Select",
            SecondaryButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            Content = ui,
            XamlRoot = XAMLRoot,
            // Keep dialog wide enough for list + Create; avoid a tiny “abbreviated” shell.
            MinWidth = 400
        };

        // Pre-select when changing an existing reference
        if (hasCurrentSelection)
            CurrentSelection = currentSelection;

        UpdatePrimaryEnabled();

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Secondary)
            return null;

        return ResolveSelectedGuid();
    }

    /// <summary>
    /// GUID of the selected element, or null when nothing is selected.
    /// </summary>
    public string ResolveSelectedGuid()
    {
        return (SelectedElement as StoryElement)?.Uuid.ToString();
    }

    /// <summary>
    /// Called by the page when list selection changes so Primary can enable/disable.
    /// </summary>
    public void NotifySelectionChanged()
    {
        UpdatePrimaryEnabled();
    }

    private void UpdatePrimaryEnabled()
    {
        if (dialog != null)
            dialog.IsPrimaryButtonEnabled = SelectedElement is StoryElement;
    }

    /// <summary>
    /// Creates a new element of the forced/selected type, selects it in the list,
    /// and leaves the dialog open for the user to confirm with Select.
    /// </summary>
    public void CreateNode()
    {
        if (StoryApi == null || StoryModel == null)
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

        var name = string.IsNullOrWhiteSpace(NewNodeName) ? $"New {type}" : NewNodeName.Trim();
        var addResult = StoryApi.AddElement(type, overview.Uuid.ToString(), name);
        if (!addResult.IsSuccess) return;

        StoryElement created = null;
        if (StoryModel.StoryElements.StoryElementGuids.TryGetValue(addResult.Payload, out var fromModel))
            created = fromModel;
        else
        {
            var lookupResult = StoryApi.GetStoryElement(addResult.Payload);
            if (lookupResult?.IsSuccess == true)
                created = lookupResult.Payload;
        }

        if (created == null) return;

        SelectedElement = created;
        NewNodeName = "";
        // Refresh list + selection; do NOT close the dialog — user confirms with Select.
        AfterCreate?.Invoke();
        UpdatePrimaryEnabled();
    }
}
