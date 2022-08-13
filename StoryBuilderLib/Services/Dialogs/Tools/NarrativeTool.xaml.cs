using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.WinUI.UI.Controls.TextToolbarSymbols;
using Elmah.Io.Client;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using StoryBuilder.Models;
using StoryBuilder.ViewModels;
using StoryBuilder.ViewModels.Tools;

namespace StoryBuilder.Services.Dialogs.Tools
{
    public sealed partial class NarrativeTool : Page
    {
        ShellViewModel ShellVM = Ioc.Default.GetService<ShellViewModel>();
        NarrativeToolVM ToolVM = Ioc.Default.GetService<NarrativeToolVM>();

        public NarrativeTool() { InitializeComponent(); }

        //This is ran when a item is clicked on either tree.
        private void ItemInvoked(object sender, TappedRoutedEventArgs e)
        {
            TreeViewItem item = (TreeViewItem)sender;
            ToolVM.SelectedNode = (StoryNodeItem)item.DataContext;

            //Only shows one selected item between either tree.
            if (item.Tag.Equals("Nar")) { ToolVM.IsNarratorSelected = true; } //Narrator Tree was clicked, so clear the explorer tree items.
            else { ToolVM.IsNarratorSelected = false; } //Explorer Tree was clicked, so clear the narrator tree items.
        }

        private void Move(object sender, RoutedEventArgs e)
        {
            if (ToolVM.SelectedNode == null || ShellVM.StoryModel.NarratorView[1].Children.Contains(ToolVM.SelectedNode)) { return; }
            var old = ShellVM.CurrentNode;
            ShellVM.CurrentNode = ToolVM.SelectedNode;
            if ((sender as Button).Tag.ToString().Contains("UP")) { ShellVM.MoveUpCommand.Execute(null); } //Move up
            else { ShellVM.MoveDownCommand.Execute(null); } //Move down
            ShellVM.CurrentNode = old; 
        }
    }
}
