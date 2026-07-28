using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using StoryCADLib.Collaborator.ViewModels;
using StoryCADLib.Models;

namespace StoryCADLib.Collaborator.Views;

/// <summary>
///     A simple picker allowing a user to pick the
///     type of element they want
/// </summary>
public sealed partial class ElementPicker : Page
{
    public Collaborator.ViewModels.ElementPickerVM PickerVM;

    public ElementPicker(Collaborator.ViewModels.ElementPickerVM viewModel)
    {
        InitializeComponent();
        PickerVM = viewModel;
        PickerVM.AfterCreate = OnAfterCreate;

        if (PickerVM.ForcedType != null)
        {
            TypeBox.Visibility = Visibility.Collapsed;
            PickerVM.SelectedType = PickerVM.ForcedType;
            Selector_OnSelectionChanged(null, null);
        }
    }

    /// <summary>
    ///     After Create: close the flyout, rebuild the list (was a dead ToList snapshot),
    ///     select the new element. Dialog hide is handled by the VM.
    /// </summary>
    private void OnAfterCreate()
    {
        NewButton.Flyout?.Hide();
        RefreshElementList(preserveSelection: true);
    }

    /// <summary>
    ///     This just handles the UI when the type Combobox is changed
    /// </summary>
    public void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        PickerVM.SelectedElement = null;
        RefreshElementList(preserveSelection: false);

        // Pre-select current element if one is specified (Change flow)
        if (ElementBox.ItemsSource is System.Collections.IEnumerable items
            && PickerVM.CurrentSelection.HasValue
            && PickerVM.CurrentSelection.Value != Guid.Empty)
        {
            var currentElement = items.Cast<object>().FirstOrDefault(e =>
                e is StoryElement se && se.Uuid == PickerVM.CurrentSelection.Value);
            if (currentElement != null)
            {
                ElementBox.SelectedItem = currentElement;
                PickerVM.SelectedElement = currentElement;
            }
        }
    }

    /// <summary>
    ///     Rebuild ElementBox from the live model collections.
    ///     Count &lt;= 1 means only the "(none)" placeholder — treat as empty.
    /// </summary>
    private void RefreshElementList(bool preserveSelection)
    {
        var preserve = preserveSelection ? PickerVM.SelectedElement as StoryElement : null;

        StoryItemType type;
        if (PickerVM.ForcedType == null)
        {
            var typeItem = PickerVM.SelectedType as ComboBoxItem;
            if (typeItem == null)
            {
                ElementBox.ItemsSource = null;
                ElementBox.IsEnabled = false;
                return;
            }

            type = Enum.Parse<StoryItemType>(typeItem.Content.ToString()!, true);
        }
        else
        {
            type = (StoryItemType)PickerVM.ForcedType;
        }

        NewButton.IsEnabled = true;

        var elements = type switch
        {
            StoryItemType.Problem => PickerVM.StoryModel.StoryElements.Problems,
            StoryItemType.Character => PickerVM.StoryModel.StoryElements.Characters,
            StoryItemType.Setting => PickerVM.StoryModel.StoryElements.Settings,
            StoryItemType.Scene => PickerVM.StoryModel.StoryElements.Scenes,
            _ => null
        };

        // Filtered collections include a "(none)" placeholder at index 0
        if (elements == null || elements.Count <= 1)
        {
            ElementBox.ItemsSource = null;
            ElementBox.IsEnabled = false;
            return;
        }

        ElementBox.IsEnabled = true;
        var elementList = elements.Skip(1).ToList();
        ElementBox.ItemsSource = elementList;

        if (preserve != null)
        {
            var match = elementList.FirstOrDefault(e => e.Uuid == preserve.Uuid);
            if (match != null)
            {
                ElementBox.SelectedItem = match;
                PickerVM.SelectedElement = match;
            }
        }
    }
}
