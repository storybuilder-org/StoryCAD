using System;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using Windows.Graphics.Printing.OptionDetails;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
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
            InitializeComponent();

            PrintVM.ProblemNodes.Clear();
            PrintVM.CharacterNodes.Clear();
            PrintVM.SceneNodes.Clear();
            PrintVM.SettingNodes.Clear();

            //Gets all nodes that aren't deleted
            try
            {
                foreach (var rootChild in Ioc.Default.GetRequiredService<ShellViewModel>().DataSource[0].Children)
                {
                    PrintVM.TraverseNode(rootChild);
                }
            }
            catch {}
        }

        /// <summary>
        /// You can't bind selected items so when the values change this function is ran which updates the values in the VM accordingly
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdateSelection(object sender, SelectionChangedEventArgs e)
        {
            PrintVM.SelectedNodes.Clear();

            if (!PrintVM.SelectAllProblems)
            {
                foreach (StoryNodeItem item in ProblemsList.SelectedItems) { PrintVM.SelectedNodes.Add(item); }
            }
            else { PrintVM.SelectedNodes.AddRange(PrintVM.ProblemNodes); }

            if (!PrintVM.SelectAllCharacters)
            {
                foreach (StoryNodeItem item in CharactersList.SelectedItems) { PrintVM.SelectedNodes.Add(item); }
            }
            else { PrintVM.SelectedNodes.AddRange(PrintVM.CharacterNodes); }

            if (!PrintVM.SelectAllScenes)
            {
                foreach (StoryNodeItem item in ScenesList.SelectedItems) { PrintVM.SelectedNodes.Add(item); }
            }
            else { PrintVM.SelectedNodes.AddRange(PrintVM.SceneNodes); }

            if (!PrintVM.SelectAllSettings)
            {
                foreach (StoryNodeItem item in SettingsList.SelectedItems) { PrintVM.SelectedNodes.Add(item); }
            }
            else { PrintVM.SelectedNodes.AddRange(PrintVM.SceneNodes); }

            ProblemsList.IsEnabled = !PrintVM.SelectAllProblems;
            CharactersList.IsEnabled = !PrintVM.SelectAllCharacters;
            SettingsList.IsEnabled = !PrintVM.SelectAllSettings;
            ScenesList.IsEnabled = !PrintVM.SelectAllScenes;
        }


        private void CheckboxClicked(object sender, RoutedEventArgs e)
        {
            UpdateSelection(null,null);
        }
    }
}
