using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using StoryCADLib.Models;

namespace StoryCADLib.Collaborator.Views;

/// <summary>
/// Collaborator element picker: choose an existing element or create one.
/// </summary>
public sealed partial class ElementPicker : Page
{
    public ViewModels.ElementPickerVM PickerVM;

    public ElementPicker(ViewModels.ElementPickerVM viewModel)
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
    /// After Create: close flyout, rebuild list, leave dialog open so user can Select.
    /// </summary>
    private void OnAfterCreate()
    {
        NewButton.Flyout?.Hide();
        RefreshElementList(preserveSelection: true);
        PickerVM.NotifySelectionChanged();
    }

    public void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        PickerVM.SelectedElement = null;
        RefreshElementList(preserveSelection: false);

        // Pre-select when changing an existing linked element
        if (ElementBox.ItemsSource is System.Collections.IEnumerable items
            && PickerVM.CurrentSelection.HasValue
            && PickerVM.CurrentSelection.Value != Guid.Empty)
        {
            var current = items.Cast<object>().FirstOrDefault(o =>
                o is StoryElement se && se.Uuid == PickerVM.CurrentSelection.Value);
            if (current != null)
            {
                ElementBox.SelectedItem = current;
                PickerVM.SelectedElement = current;
            }
        }

        PickerVM.NotifySelectionChanged();
    }

    private void ElementBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        PickerVM.NotifySelectionChanged();
    }

    /// <summary>
    /// Rebuild list from live model collections (ItemsSource is a snapshot).
    /// Count &lt;= 1 means only the "(none)" placeholder.
    /// </summary>
    private void RefreshElementList(bool preserveSelection)
    {
        var preserve = preserveSelection ? PickerVM.SelectedElement as StoryElement : null;

        if (!TryGetForcedOrSelectedType(out var type))
        {
            ElementBox.ItemsSource = null;
            ElementBox.IsEnabled = false;
            NewButton.IsEnabled = false;
            return;
        }

        // Create only when allowed (filtered multi-owner path sets AllowCreate false).
        NewButton.IsEnabled = PickerVM.AllowCreate;
        NewButton.Visibility = PickerVM.AllowCreate ? Visibility.Visible : Visibility.Collapsed;

        var elements = type switch
        {
            StoryItemType.Problem => PickerVM.StoryModel?.StoryElements.Problems,
            StoryItemType.Character => PickerVM.StoryModel?.StoryElements.Characters,
            StoryItemType.Setting => PickerVM.StoryModel?.StoryElements.Settings,
            StoryItemType.Scene => PickerVM.StoryModel?.StoryElements.Scenes,
            _ => null
        };

        if (elements == null || elements.Count <= 1)
        {
            ElementBox.ItemsSource = null;
            ElementBox.IsEnabled = false;
            return;
        }

        // Skip(1) drops the "(none)" placeholder; the rest are listed alphabetically.
        IEnumerable<StoryElement> query = elements.Skip(1);
        if (PickerVM.AllowedGuids is { Count: > 0 } allowed)
        {
            var set = allowed as HashSet<Guid> ?? allowed.ToHashSet();
            query = query.Where(e => set.Contains(e.Uuid));
        }

        var elementList = query
            .OrderBy(e => e.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        ElementBox.IsEnabled = elementList.Count > 0;
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

    private bool TryGetForcedOrSelectedType(out StoryItemType type)
    {
        if (PickerVM.ForcedType != null)
        {
            type = (StoryItemType)PickerVM.ForcedType;
            return true;
        }

        if (PickerVM.SelectedType is ComboBoxItem item && item.Content != null)
        {
            type = Enum.Parse<StoryItemType>(item.Content.ToString()!, true);
            return true;
        }

        type = default;
        return false;
    }
}
