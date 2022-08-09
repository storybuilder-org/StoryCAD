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
            ShellVM.CurrentNode = (StoryNodeItem)item.DataContext;

            //Only shows one selected item between either tree.
            //if (item.Tag == "Nar") { explorerview.SelectedNode = null; } //Narrator Tree was clicked, so clear the explorer tree items.
            //else { explorerview.SelectedNode = null; } //Explorer Tree was clicked, so clear the narrator tree items.
        }

    }
}
