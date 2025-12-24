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

        // Use pre-filtered collections from StoryElementCollection for efficiency
        var elements = type switch
        {
            StoryItemType.Problem => PickerVM.StoryModel.StoryElements.Problems,
            StoryItemType.Character => PickerVM.StoryModel.StoryElements.Characters,
            StoryItemType.Setting => PickerVM.StoryModel.StoryElements.Settings,
            StoryItemType.Scene => PickerVM.StoryModel.StoryElements.Scenes,
            _ => null
        };

        // Note: filtered collections include a "(none)" placeholder at index 0
        // Real elements start at index 1, so check for Count > 1
        if (elements == null || elements.Count <= 1)
        {
            ElementBox.IsEnabled = false;
        }
        else
        {
            //Update element box with elements (skip the "(none)" placeholder)
            ElementBox.IsEnabled = true;
            ElementBox.ItemsSource = elements.Skip(1);
        }
    }

    /// <summary>
    /// Custom XAML initialization for dynamically loaded plugin DLL.
    /// See WorkflowShell.xaml.cs for detailed explanation of this pattern.
    /// </summary>
}
