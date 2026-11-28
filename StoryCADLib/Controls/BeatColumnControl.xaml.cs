using Windows.ApplicationModel.DataTransfer;
using Microsoft.UI.Xaml;
using StoryCADLib.ViewModels.SubViewModels;
using StoryCADLib.ViewModels.Tools;

namespace StoryCADLib.Controls;

/// <summary>
///     Shows one Problem: a header, its beats as cards, and its elements on no beat (issue #1546).
///     The code here is only for the drag operation. Each drop calls one method of BeatSheetsViewModel.
/// </summary>
public sealed partial class BeatColumnControl : UserControl
{
    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
        nameof(ViewModel), typeof(BeatSheetsViewModel), typeof(BeatColumnControl), new PropertyMetadata(null));

    public static readonly DependencyProperty ShowHeaderProperty = DependencyProperty.Register(
        nameof(ShowHeader), typeof(bool), typeof(BeatColumnControl),
        new PropertyMetadata(true, (d, _) => ((BeatColumnControl)d).ApplyVisibility()));

    public static readonly DependencyProperty ShowBeatDescriptionsProperty = DependencyProperty.Register(
        nameof(ShowBeatDescriptions), typeof(bool), typeof(BeatColumnControl),
        new PropertyMetadata(false, (d, _) => ((BeatColumnControl)d).ApplyVisibility()));

    public static readonly DependencyProperty ShowElementDescriptionsProperty = DependencyProperty.Register(
        nameof(ShowElementDescriptions), typeof(bool), typeof(BeatColumnControl),
        new PropertyMetadata(false, (d, _) => ((BeatColumnControl)d).ApplyVisibility()));

    public BeatColumnControl()
    {
        InitializeComponent();
        ApplyVisibility();
    }

    /// <summary>The Problem that this control shows.</summary>
    public BeatSheetsViewModel ViewModel
    {
        get => (BeatSheetsViewModel)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    /// <summary>True shows the name, the category and the beat sheet of the Problem above the beats.</summary>
    public bool ShowHeader
    {
        get => (bool)GetValue(ShowHeaderProperty);
        set => SetValue(ShowHeaderProperty, value);
    }

    /// <summary>True shows the description of each beat on its card.</summary>
    public bool ShowBeatDescriptions
    {
        get => (bool)GetValue(ShowBeatDescriptionsProperty);
        set => SetValue(ShowBeatDescriptionsProperty, value);
    }

    /// <summary>True shows the description of the Scene or Problem on each card and in the list "On no beat".</summary>
    public bool ShowElementDescriptions
    {
        get => (bool)GetValue(ShowElementDescriptionsProperty);
        set => SetValue(ShowElementDescriptionsProperty, value);
    }

    /// <summary>Raised when one of the two lists gets the focus. The beat tree window uses it.</summary>
    public event EventHandler<BeatSheetsViewModel> Activated;

    /// <summary>The list of the beats. The beat tree window reads the position of a card from it.</summary>
    public ListView BeatsList => BeatsListView;

    /// <summary>The list of the elements on no beat.</summary>
    public ListView NoBeatList => NoBeatListView;

    /// <summary>The header. The beat tree window draws a connector to it.</summary>
    public FrameworkElement Header => HeaderPanel;

    private void ApplyVisibility()
    {
        if (HeaderPanel == null)
        {
            return;
        }

        HeaderPanel.Visibility = ShowHeader ? Visibility.Visible : Visibility.Collapsed;
        ((VisibilityHolder)Resources["DescriptionVisibility"]).Value =
            ShowBeatDescriptions ? Visibility.Visible : Visibility.Collapsed;
        ((VisibilityHolder)Resources["ElementDescriptionVisibility"]).Value =
            ShowElementDescriptions ? Visibility.Visible : Visibility.Collapsed;
    }

    private void List_GotFocus(object sender, RoutedEventArgs e)
    {
        if (ViewModel != null)
        {
            Activated?.Invoke(this, ViewModel);
        }
    }

    // ----- The drag operation -----

    private void BeatsListView_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
    {
        // A card with no element does not start a drag operation.
        if (ViewModel == null || e.Items.FirstOrDefault() is not StructureBeat beat || beat.Guid == Guid.Empty)
        {
            e.Cancel = true;
            return;
        }

        Start(e, new StructureDrag
        {
            Element = beat.Guid,
            Source = ViewModel,
            BeatIndex = ViewModel.StructureBeats.IndexOf(beat)
        });
    }

    private void NoBeatListView_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
    {
        if (ViewModel == null || e.Items.FirstOrDefault() is not StoryElement element)
        {
            e.Cancel = true;
            return;
        }

        Start(e, new StructureDrag { Element = element.Uuid, Source = ViewModel });
    }

    private static void Start(DragItemsStartingEventArgs e, StructureDrag drag)
    {
        StructureDrag.Current = drag;
        e.Data.SetText(drag.ToText());
        e.Data.RequestedOperation = DataPackageOperation.Move;
    }

    private void ListView_DragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
    {
        StructureDrag.Current = null;
    }

    private void Target_DragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = StructureDrag.Current != null && ViewModel != null
            ? DataPackageOperation.Move
            : DataPackageOperation.None;
        e.Handled = true;
    }

    /// <summary>A drop on a card: put the element on that beat.</summary>
    private async void Card_Drop(object sender, DragEventArgs e)
    {
        var drag = StructureDrag.Current;
        if (drag == null || ViewModel == null ||
            (sender as FrameworkElement)?.DataContext is not StructureBeat beat)
        {
            return;
        }

        e.Handled = true;
        var index = ViewModel.StructureBeats.IndexOf(beat);
        var fromIndex = ReferenceEquals(drag.Source, ViewModel) ? drag.BeatIndex : -1;
        await ViewModel.PlaceOnBeatAsync(drag.Element, index, fromIndex);
    }

    /// <summary>A drop on the list of the elements on no beat.</summary>
    private async void NoBeatListView_Drop(object sender, DragEventArgs e)
    {
        var drag = StructureDrag.Current;
        if (drag == null || ViewModel == null)
        {
            return;
        }

        e.Handled = true;
        if (ReferenceEquals(drag.Source, ViewModel))
        {
            // From a card of this beat column: take the element off the beat. It stays in the Problem.
            if (drag.BeatIndex >= 0 && drag.BeatIndex < ViewModel.StructureBeats.Count)
            {
                ViewModel.SelectedBeat = ViewModel.StructureBeats[drag.BeatIndex];
                ViewModel.SelectedBeatIndex = drag.BeatIndex;
                ViewModel.UnbindElement();
            }

            return;
        }

        await ViewModel.AddElementAsync(drag.Element);
    }
}
