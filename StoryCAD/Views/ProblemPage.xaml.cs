using Windows.ApplicationModel.DataTransfer;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using StoryCAD.Services.Logging;
using StoryCAD.ViewModels.SubViewModels;
using StoryCAD.ViewModels.Tools;

namespace StoryCAD.Views;

public sealed partial class ProblemPage : BindablePage
{
    public ProblemViewModel ProblemVm;
    public ShellViewModel ShellVm => Ioc.Default.GetService<ShellViewModel>();
    public OutlineViewModel OutlineVM => Ioc.Default.GetService<OutlineViewModel>();
    public BeatSheetsViewModel BeatSheetsViewModel = Ioc.Default.GetService<BeatSheetsViewModel>();
    public LogService LogService = Ioc.Default.GetService<LogService>();
    public ProblemPage()
    {
        ProblemVm = Ioc.Default.GetService<ProblemViewModel>();
        InitializeComponent();
        DataContext = ProblemVm;
    }

    #region DND
    /// <summary>
    /// Ran when an item is dropped on the right side of the beat panel
    /// </summary>
    private async void DroppedItem(object sender, DragEventArgs e)
    {
        var stackPanel = sender as Grid;
        if (stackPanel == null) return;
        //Extract model
        var structureBeatsModel = stackPanel.DataContext as StructureBeatViewModel;
        if (structureBeatsModel == null) { return; }

        try // Now, you can access structureBeatsModel to know which item was dropped on
        {
            string text = await e.DataView.GetTextAsync();
            //Check we are dragging an element GUID and not something else
            if (!text.Contains("GUID")) { return; }

            //Parse out the element GUID.
            Guid.TryParse(text.Split(":")[1], out Guid uuid);

            //Find element being dropped.
            StoryElement Element = OutlineVM.StoryModel.StoryElements.First(g => g.Uuid == uuid);
            int ElementIndex = OutlineVM.StoryModel.StoryElements.IndexOf(Element);

            //Check if problem is being dropped and enforce rule.
            if (Element.Type == StoryItemType.Problem)
            {
                ProblemModel problem = (ProblemModel)Element;
                //Enforce rule that problems can only be bound to one structure beat model
                if (!string.IsNullOrEmpty(problem.BoundStructure)) //Check element is actually bound elsewhere
                {
                    ProblemModel ContainingStructure = (ProblemModel)OutlineVM.StoryModel.StoryElements.First(g => g.Uuid == Guid.Parse(problem.BoundStructure));
                    //Show dialog asking to rebind.
                    var res = await Ioc.Default.GetRequiredService<Windowing>().ShowContentDialog(new()
                    {
                        Title = "Already assigned!",
                        Content = $"This problem is already assigned to a different structure ({ContainingStructure.Name}) " +
    $"Would you like to assign it here instead?",
                        PrimaryButtonText = "Assign here",
                        SecondaryButtonText = "Cancel"
                    });

                    //Do nothing if user clicks don't rebind.
                    if (res != ContentDialogResult.Primary) { return; }

                    if (problem.BoundStructure.Equals(ProblemVm.Uuid.ToString())) //Rebind from VM
                    {
                        StructureBeatViewModel oldStructure = ContainingStructure.StructureBeats.First(g => g.Guid == problem.Uuid);
                        int index = ProblemVm.StructureBeats.IndexOf(oldStructure);
                        ProblemVm.StructureBeats[index].Guid = Guid.Empty;
                    }
                    else //Remove from old structure and update story elements.
                    {
                        StructureBeatViewModel oldStructure = ContainingStructure.StructureBeats.First(g => g.Guid == problem.Uuid);
                        int index = ContainingStructure.StructureBeats.IndexOf(oldStructure);
                        ContainingStructure.StructureBeats[index].Guid = Guid.Empty;
                        int ContainingStructIndex = OutlineVM.StoryModel.StoryElements.IndexOf(ContainingStructure);
                        OutlineVM.StoryModel.StoryElements[ContainingStructIndex] = ContainingStructure;
                    }
                }

                if (problem.Uuid == ProblemVm.Uuid)
                {
                    ProblemVm.BoundStructure = ProblemVm.Uuid.ToString();
                }
                else
                {
                    problem.BoundStructure = ProblemVm.Uuid.ToString();
                    OutlineVM.StoryModel.StoryElements[ElementIndex] = problem;
                }
            }

            structureBeatsModel.Guid = uuid;
            stackPanel.DataContext = structureBeatsModel;


            //Fix theming issue, since the text might need its font color updating
            RichEditBoxExtended rtfex = (RichEditBoxExtended)stackPanel.Children.First(RTF => RTF.GetType() == typeof(RichEditBoxExtended));
            rtfex.UpdateTheme(null, null);
        }
        catch (Exception ex)
        {
            LogService.Log(LogLevel.Warn, $"Failed to drag valid element (StructureDND)" +
                     $" (This is expected if non element object was DND) {ex.Message}");
        }
    }

    private void UIElement_OnDragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = DataPackageOperation.Move;
        e.Handled = true;
    }

    private void ListViewBase_OnDragItemsStarting(object sender, DragItemsStartingEventArgs e)
    {
        if (e.Items.Count > 0)
        {
            var draggedStoryElement = e.Items[0] as StoryElement; // Cast to StoryElement
            if (draggedStoryElement != null)
            {
                // Set the StoryElement object as part of the drag data
                e.Data.SetText("GUID:" + draggedStoryElement.Uuid.ToString());
                e.Data.RequestedOperation = DataPackageOperation.Move;
            }
        }
    }
    #endregion
}
