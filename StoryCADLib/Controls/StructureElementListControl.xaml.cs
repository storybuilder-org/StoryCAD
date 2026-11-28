using Windows.ApplicationModel.DataTransfer;
using Microsoft.UI.Xaml;
using StoryCADLib.ViewModels.SubViewModels;
using StoryCADLib.ViewModels.Tools;

namespace StoryCADLib.Controls;

/// <summary>
///     Shows the list of all Scenes or all Problems of the outline (issue #1546).
///     The Structure tab and the beat tree window use it.
/// </summary>
public sealed partial class StructureElementListControl : UserControl
{
    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
        nameof(ViewModel), typeof(StructureElementListViewModel), typeof(StructureElementListControl),
        new PropertyMetadata(null));

    public StructureElementListControl()
    {
        InitializeComponent();
    }

    public StructureElementListViewModel ViewModel
    {
        get => (StructureElementListViewModel)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    // ----- The drag operation -----

    private void ElementsList_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
    {
        if (e.Items.FirstOrDefault() is not StructureElementItem item)
        {
            e.Cancel = true;
            return;
        }

        var drag = new StructureDrag { Element = item.Element.Uuid };
        StructureDrag.Current = drag;
        e.Data.SetText(drag.ToText());
        e.Data.RequestedOperation = DataPackageOperation.Move;
    }

    private void ElementsList_DragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
    {
        StructureDrag.Current = null;
    }

    private void ElementsList_DragOver(object sender, DragEventArgs e)
    {
        // Only an element that comes from a beat column can come back to this list.
        e.AcceptedOperation = StructureDrag.Current?.Source != null
            ? DataPackageOperation.Move
            : DataPackageOperation.None;
        e.Handled = true;
    }

    /// <summary>A drop of a card or of an element on no beat: the element leaves that Problem.</summary>
    private async void ElementsList_Drop(object sender, DragEventArgs e)
    {
        var drag = StructureDrag.Current;
        if (drag?.Source == null)
        {
            return;
        }

        e.Handled = true;
        await drag.Source.RemoveElementAsync(drag.Element);
    }
}
