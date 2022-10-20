using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using StoryBuilder.ViewModels;
using StoryBuilder.ViewModels.Tools;

namespace StoryBuilder.Services.Dialogs.Tools;

public sealed partial class NarrativeTool
{
    ShellViewModel _shellVM = Ioc.Default.GetService<ShellViewModel>();
    NarrativeToolVM _toolVM = Ioc.Default.GetService<NarrativeToolVM>();

    public NarrativeTool() { InitializeComponent(); }

    //This is ran when a item is clicked on either tree.
    private void ItemInvoked(object sender, TappedRoutedEventArgs e)
    {
        TreeViewItem _Item = (TreeViewItem)sender;
        _toolVM.SelectedNode = (StoryNodeItem)_Item.DataContext;

        //Only shows one selected item between either tree.
        if (_Item.Tag.Equals("Nar")) { _toolVM.IsNarratorSelected = true; } //Narrator Tree was clicked, so clear the explorer tree items.
        else { _toolVM.IsNarratorSelected = false; } //Explorer Tree was clicked, so clear the narrator tree items.
    }

    private void Move(object sender, RoutedEventArgs e)
    {
        if (_toolVM.SelectedNode == null) { return; }
        StoryNodeItem _Old = _shellVM.CurrentNode;
        _shellVM.CurrentNode = _toolVM.SelectedNode;
        if ((sender as Button).Tag.ToString()!.Contains("UP")) { _shellVM.MoveUpCommand.Execute(null); } //Move up
        else { _shellVM.MoveDownCommand.Execute(null); } //Move down
        _shellVM.CurrentNode = _Old; 
    }
}