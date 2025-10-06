using Microsoft.UI.Xaml.Input;
using StoryCADLib.ViewModels.SubViewModels;
using StoryCADLib.ViewModels.Tools;

namespace StoryCADLib.Services.Dialogs.Tools;

public sealed partial class NarrativeTool : Page
{
    private readonly AppState AppState = Ioc.Default.GetService<AppState>();
    private readonly ShellViewModel ShellVM = Ioc.Default.GetService<ShellViewModel>();
    private OutlineViewModel OutlineVM = Ioc.Default.GetService<OutlineViewModel>();
    private NarrativeToolVM ToolVM = Ioc.Default.GetService<NarrativeToolVM>();

    public NarrativeTool()
    {
        InitializeComponent();
    }

    //This is ran when a item is clicked on either tree.
    private void ItemInvoked(object sender, TappedRoutedEventArgs e)
    {
        var item = (TreeViewItem)sender;
        ToolVM.SelectedNode = (StoryNodeItem)item.DataContext;

        //Only shows one selected item between either tree.
        if (item.Tag.Equals("Nar"))
        {
            ToolVM.IsNarratorSelected = true;
        } //NarratorView Tree was clicked, so clear the explorer tree items.
        else
        {
            ToolVM.IsNarratorSelected = false;
        } //ExplorerView Tree was clicked, so clear the narrator tree items.
    }

    private void Move(object sender, RoutedEventArgs e)
    {
        if (ToolVM.SelectedNode == null)
        {
            return;
        }

        var old = ShellVM.CurrentNode;
        ShellVM.CurrentNode = ToolVM.SelectedNode;
        if ((sender as Button).Tag.ToString().Contains("UP"))
        {
            ShellVM.MoveUpCommand.Execute(null);
        } //Move up
        else
        {
            ShellVM.MoveDownCommand.Execute(null);
        } //Move down

        ShellVM.CurrentNode = old;
    }
}
