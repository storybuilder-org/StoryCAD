using System;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using StoryBuilder.Services.Logging;
using StoryBuilder.ViewModels;
using StoryBuilder.ViewModels.Tools;

namespace StoryBuilder.Services.Dialogs.Tools;
public sealed partial class PrintReportsDialog
{
    public PrintReportDialogVM PrintVM = Ioc.Default.GetRequiredService<PrintReportDialogVM>();

    public PrintReportsDialog()
    {
        InitializeComponent();
        PrintVM.SelectAllCharacters = false;
        PrintVM.SelectAllProblems = false;
        PrintVM.SelectAllScenes = false;
        PrintVM.SelectAllSettings = false;
        PrintVM.CreateOverview = false;
        PrintVM.CreateSummary = false;
        PrintVM.SelectedNodes.Clear();
        PrintVM.ProblemNodes.Clear();
        PrintVM.CharacterNodes.Clear();
        PrintVM.SceneNodes.Clear();
        PrintVM.SettingNodes.Clear();
        PrintVM.WebNodes.Clear();
        PrintVM.CharacterList = false;
        PrintVM.ProblemList = false;
        PrintVM.SettingList = false;
        PrintVM.SceneList = false;
        PrintVM.WebList = false;

        //Gets all nodes that aren't deleted
        try
        {
            foreach (StoryNodeItem _Child in Ioc.Default.GetRequiredService<ShellViewModel>().StoryModel.ExplorerView[0]) { PrintVM.TraverseNode(_Child); }
        }
        catch (Exception _Ex)
        {
            Ioc.Default.GetService<LogService>()?.LogException(LogLevel.Error, _Ex, "Error parsing nodes.");
        }
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
            foreach (StoryNodeItem _Item in ProblemsList.SelectedItems) { PrintVM.SelectedNodes.Add(_Item); }
        }
        else { PrintVM.SelectedNodes.AddRange(PrintVM.ProblemNodes); }

        if (!PrintVM.SelectAllCharacters)
        {
            foreach (StoryNodeItem _Item in CharactersList.SelectedItems) { PrintVM.SelectedNodes.Add(_Item); }
        }
        else { PrintVM.SelectedNodes.AddRange(PrintVM.CharacterNodes); }

        if (!PrintVM.SelectAllScenes)
        {
            foreach (StoryNodeItem _Item in ScenesList.SelectedItems) { PrintVM.SelectedNodes.Add(_Item); }
        }
        else { PrintVM.SelectedNodes.AddRange(PrintVM.SceneNodes); }

        if (!PrintVM.SelectAllSettings)
        {
            foreach (StoryNodeItem _Item in SettingsList.SelectedItems) { PrintVM.SelectedNodes.Add(_Item); }
        }
        else { PrintVM.SelectedNodes.AddRange(PrintVM.SettingNodes); }

        if (!PrintVM.SelectAllWeb)
        {
            foreach (StoryNodeItem _Item in WebList.SelectedItems) { PrintVM.SelectedNodes.Add(_Item); }
        }
        else { PrintVM.SelectedNodes.AddRange(PrintVM.WebNodes); }

        ProblemsList.IsEnabled = !PrintVM.SelectAllProblems;
        CharactersList.IsEnabled = !PrintVM.SelectAllCharacters;
        SettingsList.IsEnabled = !PrintVM.SelectAllSettings;
        ScenesList.IsEnabled = !PrintVM.SelectAllScenes;
        WebList.IsEnabled = !PrintVM.SelectAllWeb;
    }


    private void CheckboxClicked(object sender, RoutedEventArgs e)
    {
        //This clears any selected checkboxes
        switch ((sender as CheckBox)?.Content.ToString())
        {
            case "Print all problems": ProblemsList.SelectedItems.Clear(); break;
            case "Print all characters": CharactersList.SelectedItems.Clear(); break;
            case "Print all scenes": ScenesList.SelectedItems.Clear(); break;
            case "Print all settings": SettingsList.SelectedItems.Clear(); break;
            case "Print all websites": WebList.SelectedItems.Clear(); break;
        }
        UpdateSelection(null,null);
    }
}