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

        public NarrativeTool()
        {
            this.InitializeComponent();
        }

        private void UIElement_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            TreeViewItem item = (TreeViewItem)sender;
            ToolVM.LastSelectedNode = (StoryNodeItem)item.DataContext;
        }

    }
}
