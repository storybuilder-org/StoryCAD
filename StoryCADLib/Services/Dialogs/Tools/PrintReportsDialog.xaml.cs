using System;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using StoryCAD.Models;
using StoryCAD.Services.Logging;
using StoryCAD.ViewModels;
using StoryCAD.ViewModels.Tools;
using Windows.Graphics.Printing;
using Microsoft.UI.Dispatching;

namespace StoryCAD.Services.Dialogs.Tools;
public sealed partial class PrintReportsDialog
{
    public PrintReportDialogVM PrintVM = Ioc.Default.GetRequiredService<PrintReportDialogVM>();
    DispatcherTimer _isDone = new() { Interval = new(0, 0, 0, 1, 0) };

    public PrintReportsDialog()
    {
        InitializeComponent();
        PrintVM.RegisterForPrint();
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

        //Warn user if they are on win10 as print manager can't be used.
        if (Environment.OSVersion.Version.Build <= 22000) { Win10Warning.IsOpen = true; }

        //Gets all nodes that aren't deleted
        try
        {
            foreach (StoryNodeItem rootChild in Ioc.Default.GetRequiredService<ShellViewModel>().DataSource[0].Children)
            {
                PrintVM.TraverseNode(rootChild);
            }
        }
        catch (Exception ex)
        {
            Ioc.Default.GetRequiredService<LogService>().LogException(LogLevel.Error, ex, "Error parsing nodes.");
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
        else { PrintVM.SelectedNodes.AddRange(PrintVM.SettingNodes); }

        if (!PrintVM.SelectAllWeb)
        {
            foreach (StoryNodeItem item in WebList.SelectedItems) { PrintVM.SelectedNodes.Add(item); }
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

    private async void StartPrintMenu(object sender, RoutedEventArgs e)
    {
        PrintVM.GeneratePrintDocumentReport();
        PrintVM.PrintDocSource = PrintVM.Document.DocumentSource;

        //Device has to support printing AND run windows 11 (Win11's RTM Build was build 22000 and Win10 Build will ever be above 22000)
        //Windows 10 currently does not support PrintManagers due to a bug in windows 10, however this should get fixed soon (hopefully)
        if (PrintManager.IsSupported() && Environment.OSVersion.Version.Build >= 22000) 
        {
            try
            {
                // Show print UI
                await PrintManagerInterop.ShowPrintUIForWindowAsync(GlobalData.WindowHandle);
            }
            catch (Exception ex) //Error setting up printer
            {
                GlobalData.GlobalDispatcher.TryEnqueue(async () =>
                {
                    PrintVM.CloseDialog();
                    await new ContentDialog
                    {
                        XamlRoot = GlobalData.XamlRoot,
                        Title = "Printing error",
                        Content = "The following error occurred when trying to print:\n\n" + ex.Message,
                        PrimaryButtonText = "Ok"
                    }.ShowAsync();
                });
;
            }
        }
        else //Print Manager isn't supported so we fall back to the old version of printing directly.
        {
            PrintVM.ShowLoadingBar = true;
            LoadingBar.Opacity = 1;
            PrintVM.StartGeneratingReports();
            _isDone.Tick += IsReportGenerationFinished;
            _isDone.Start();
        }
    }
    private void IsReportGenerationFinished(object sender, object e)
    {
        if (!PrintVM.ShowLoadingBar)
        {
            _isDone.Stop();
            LoadingBar.Opacity = 0;
            _isDone.Tick -= IsReportGenerationFinished;
            PrintVM.ShowLoadingBar = false;
            PrintVM.CloseDialog();
            Ioc.Default.GetRequiredService<ShellViewModel>().ShowMessage(LogLevel.Info, "Generate Print Reports complete", true);
        }
    }
}