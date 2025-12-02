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

    public ElementPicker()
    {
        InitializeComponent();
        PickerVM = new Collaborator.ViewModels.ElementPickerVM();

        if (PickerVM.ForcedType != null)
        {
            TypeBox.Visibility = Visibility.Collapsed;
            PickerVM.SelectedType = PickerVM.ForcedType;
            Selector_OnSelectionChanged(null, null);
        }
    }

    /// <summary>
    ///     This just handles the UI when the type Combobox is changed
    /// </summary>
    public void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        StoryItemType type;
        if (PickerVM.ForcedType == null)
        {
            //Get elements
            var Type = PickerVM.SelectedType as ComboBoxItem;
            type = Enum.Parse<StoryItemType>(Type.Content.ToString()!,
                true);
        }
        else
        {
            type = (StoryItemType)PickerVM.ForcedType;
        }


        //Reset the UIs, so we can't enter an invalid state
        PickerVM.SelectedElement = null;
        ElementBox.ItemsSource = null;
        NewButton.IsEnabled = true;

        var elements = PickerVM.StoryModel.StoryElements
            .Where(element => element.ElementType == type);
        if (elements.Count() == 0) // no elements so disable picker
        {
            ElementBox.IsEnabled = false;
        }
        else
        {
            //Update element box
            ElementBox.IsEnabled = true; //Enable elements box
            ElementBox.ItemsSource = elements;
        }
    }

    /// <summary>
    /// Custom XAML initialization for dynamically loaded plugin DLL.
    /// See WorkflowShell.xaml.cs for detailed explanation of this pattern.
    /// </summary>
}
