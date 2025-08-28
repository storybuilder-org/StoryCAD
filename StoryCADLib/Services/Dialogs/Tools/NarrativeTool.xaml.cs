using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using StoryCAD.ViewModels.SubViewModels;
using StoryCAD.ViewModels.Tools;

namespace StoryCAD.Services.Dialogs.Tools
{
    public sealed partial class NarrativeTool : Page
    {
        ShellViewModel ShellVM = Ioc.Default.GetService<ShellViewModel>();
        OutlineViewModel OutlineVM = Ioc.Default.GetService<OutlineViewModel>();
        NarrativeToolVM ToolVM = Ioc.Default.GetService<NarrativeToolVM>();
        AppState AppState = Ioc.Default.GetService<AppState>();

        public NarrativeTool() { InitializeComponent(); }

        //This is ran when a item is clicked on either tree.
        private void ItemInvoked(object sender, TappedRoutedEventArgs e)
        {
            TreeViewItem item = (TreeViewItem)sender;
            ToolVM.SelectedNode = (StoryNodeItem)item.DataContext;

            //Only shows one selected item between either tree.
            if (item.Tag.Equals("Nar")) { ToolVM.IsNarratorSelected = true; } //NarratorView Tree was clicked, so clear the explorer tree items.
            else { ToolVM.IsNarratorSelected = false; } //ExplorerView Tree was clicked, so clear the narrator tree items.
        }

        private void Move(object sender, RoutedEventArgs e)
        {
            if (ToolVM.SelectedNode == null) { return; }
            StoryNodeItem old = ShellVM.CurrentNode;
            ShellVM.CurrentNode = ToolVM.SelectedNode;
            if ((sender as Button).Tag.ToString().Contains("UP")) { ShellVM.MoveUpCommand.Execute(null); } //Move up
            else { ShellVM.MoveDownCommand.Execute(null); } //Move down
            ShellVM.CurrentNode = old; 
        }
    }
}
