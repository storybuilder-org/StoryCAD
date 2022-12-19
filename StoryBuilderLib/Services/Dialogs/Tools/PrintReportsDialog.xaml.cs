using System;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Printing;
using StoryBuilder.Models;
using StoryBuilder.Services.Logging;
using StoryBuilder.ViewModels;
using StoryBuilder.ViewModels.Tools;
using Windows.Graphics.Printing;
using PrintDocument = Microsoft.UI.Xaml.Printing.PrintDocument;

namespace StoryBuilder.Services.Dialogs.Tools;
public sealed partial class PrintReportsDialog : Page
{
    public PrintReportDialogVM PrintVM = Ioc.Default.GetRequiredService<PrintReportDialogVM>();
    private PrintManager _PrintManager;
    private IPrintDocumentSource _PrintDocSource;

    public PrintReportsDialog()
    {
        InitializeComponent();
        RegisterPrint();
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
            foreach (StoryNodeItem rootChild in Ioc.Default.GetRequiredService<ShellViewModel>().DataSource[0].Children)
            {
                PrintVM.TraverseNode(rootChild);
            }
        }
        catch (Exception ex)
        {
            Ioc.Default.GetService<LogService>().LogException(LogLevel.Error, ex, "Error parsing nodes.");
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
        switch ((sender as CheckBox).Content.ToString())
        {
            case "Print all problems": ProblemsList.SelectedItems.Clear(); break;
            case "Print all characters": CharactersList.SelectedItems.Clear(); break;
            case "Print all scenes": ScenesList.SelectedItems.Clear(); break;
            case "Print all settings": SettingsList.SelectedItems.Clear(); break;
            case "Print all websites": WebList.SelectedItems.Clear(); break;
        }
        UpdateSelection(null,null);
    }

    void RegisterPrint()
    {
        // Register for PrintTaskRequested event
        _PrintManager = PrintManagerInterop.GetForWindow(GlobalData.WindowHandle);
        _PrintManager.PrintTaskRequested += PrintTaskRequested;
    }

    private async void StartPrintMenu(object sender, RoutedEventArgs e)
    {

        _PrintDocSource = _document.DocumentSource;

        if (PrintManager.IsSupported())
        {
            try
            {
                // Show print UI
                await PrintManagerInterop.ShowPrintUIForWindowAsync(GlobalData.WindowHandle);
            }
            catch //Error setting up printer
            {
                await new ContentDialog()
                {
                    XamlRoot = (sender as Button).XamlRoot,
                    Title = "Printing error",
                    Content = "An error occurred trying to print.",
                    PrimaryButtonText = "Ok"
                }.ShowAsync();
            }
        }
        else // Printing isn't supported.
        {
            await new ContentDialog()
            {
                XamlRoot = (sender as Button).XamlRoot,
                Title = "Printing not supported",
                Content = "This device doesn't support printing.",
                PrimaryButtonText = "OK"
            }.ShowAsync();
        }
    }

    private void PrintTaskRequested(PrintManager sender, PrintTaskRequestedEventArgs args)
    {
        // Create the PrintTask.
        // Defines the title and delegate for PrintTaskSourceRequested
        var printTask = args.Request.CreatePrintTask("Print", PrintTaskSourceRequrested);

        // Handle PrintTask.Completed to catch failed print jobs
        printTask.Completed += PrintTaskCompleted;
    }

    private void PrintTaskSourceRequrested(PrintTaskSourceRequestedArgs args)
    {
        // Set the document source.
        args.SetSource(_PrintDocSource);
    }

    private void GetPreviewPage(object sender, GetPreviewPageEventArgs e)
    {
        // Provide a UIElement as the print preview.
        //_document.SetPreviewPage(e.PageNumber, x);
    }



    private void PrintTaskCompleted(PrintTask sender, PrintTaskCompletedEventArgs args)
    {
        // Notify the user when the print operation fails.
        if (args.Completion == PrintTaskCompletion.Failed)
        {
            DispatcherQueue.TryEnqueue(async () =>
            {
                await new ContentDialog()
                {
                    XamlRoot = Content.XamlRoot,
                    Title = "Printing error",
                    Content = "\nSorry, failed to print.",
                    PrimaryButtonText = "OK"
                }.ShowAsync();
            });
        }
    }
}