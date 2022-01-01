using System;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using Windows.Graphics.Printing.OptionDetails;
using CommunityToolkit.Mvvm.DependencyInjection;
using StoryBuilder.Models;
using StoryBuilder.ViewModels;
using StoryBuilder.ViewModels.Tools;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace StoryBuilder.Services.Dialogs.Tools
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class PrintReportsDialog : Page
    {

        public PrintReportDialogVM PrintVM = Ioc.Default.GetRequiredService<PrintReportDialogVM>();

        public PrintReportsDialog()
        {
            this.InitializeComponent();
            //Gets all nodes that aren't deleted
            foreach (var rootChild in Ioc.Default.GetRequiredService<ShellViewModel>().DataSource[0].Children)
            {
                PrintVM.TraverseNode(rootChild);
            }
        }

        /// <summary>
        /// You can't bind selected items so when the values change this function is ran which updates the values in the VM accordingly
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void UpdateSelection(object sender, SelectionChangedEventArgs e)
        {
            PrintVM.SelectedNodes.Clear();
            
            foreach (var item in ProblemsList.SelectedItems) { PrintVM.SelectedNodes.Add((StoryNodeItem) item); }
            foreach (var item in CharactersList.SelectedItems) { PrintVM.SelectedNodes.Add((StoryNodeItem) item); }
            foreach (var item in ScenesList.SelectedItems) { PrintVM.SelectedNodes.Add((StoryNodeItem) item); }
            foreach (var item in SettingsList.SelectedItems) { PrintVM.SelectedNodes.Add((StoryNodeItem) item); }
        }
    }
}
