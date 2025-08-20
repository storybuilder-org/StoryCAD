using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Printing;
using StoryCAD.Services.Reports;
using Windows.Graphics.Printing;
using StoryCAD.Services.Dialogs.Tools;
using StoryCAD.ViewModels.SubViewModels;
using StoryCAD.Services.Backup;
using StoryCAD.Services.Locking;

namespace StoryCAD.ViewModels.Tools;

public class PrintReportDialogVM : ObservableRecipient
{
    public ContentDialog Dialog;
    public PrintTask PrintJobManager;
    private PrintManager _printManager;
    public PrintDocument Document = new();
    public IPrintDocumentSource PrintDocSource;
    private readonly Windowing Window;
    private readonly OutlineViewModel _outlineViewModel;
    private readonly ShellViewModel _shellViewModel;
    private readonly ILogService _logService;
    private List<StackPanel> _printPreviewCache; //This stores a list of pages for print preview

    // Constructor for XAML compatibility - will be removed later
    public PrintReportDialogVM() : this(
        Ioc.Default.GetRequiredService<Windowing>(),
        Ioc.Default.GetRequiredService<OutlineViewModel>(),
        Ioc.Default.GetRequiredService<ShellViewModel>(),
        Ioc.Default.GetRequiredService<ILogService>())
    {
    }

    public PrintReportDialogVM(Windowing window, OutlineViewModel outlineViewModel, ShellViewModel shellViewModel, ILogService logService)
    {
        Window = window;
        _outlineViewModel = outlineViewModel;
        _shellViewModel = shellViewModel;
        _logService = logService;
    }

    #region Properties
    private bool _createSummary;
    public bool CreateSummary
    {
        get => _createSummary;
        set => SetProperty(ref _createSummary, value);
    }
    private bool _createStructure;
    public bool CreateStructure
    {
        get => _createStructure;
        set => SetProperty(ref _createStructure, value);
    }
    private bool _selectAllProblems;
    public bool SelectAllProblems
    {
        get => _selectAllProblems;
        set => SetProperty(ref _selectAllProblems, value);
    }

    private bool _selectAllCharacters;
    public bool SelectAllCharacters
    {
        get => _selectAllCharacters;
        set => SetProperty(ref _selectAllCharacters, value);
    }

    private bool _selectAllScenes;
    public bool SelectAllScenes
    {
        get => _selectAllScenes;
        set => SetProperty(ref _selectAllScenes, value);
    }

    private bool _selectAllWeb;
    public bool SelectAllWeb
    {
        get => _selectAllWeb;
        set => SetProperty(ref _selectAllWeb, value);
    }

    private bool _selectAllSetting;
    public bool SelectAllSettings
    {
        get => _selectAllSetting;
        set => SetProperty(ref _selectAllSetting, value);
    }

    private bool _createOverview;
    public bool CreateOverview
    {
        get => _createOverview;
        set => SetProperty(ref _createOverview, value);
    }

    private bool _problemList;
    public bool ProblemList
    {
        get => _problemList;
        set => SetProperty(ref _problemList, value);
    }

    private bool _characterList;
    public bool CharacterList
    {
        get => _characterList;
        set => SetProperty(ref _characterList, value);
    }

    private bool _settingList;
    public bool SettingList
    {
        get => _settingList;
        set => SetProperty(ref _settingList, value);
    }

    private bool _sceneList;
    public bool SceneList
    {
        get => _sceneList;
        set => SetProperty(ref _sceneList, value);
    }

    private bool _webList;
    public bool WebList
    {
        get => _webList;
        set => SetProperty(ref _webList, value);
    }

    private List<StoryNodeItem> _selectedNodes = new();
    public List<StoryNodeItem> SelectedNodes
    {
        get => _selectedNodes;
        set => SetProperty(ref _selectedNodes, value);
    }

    private List<StoryNodeItem> _problemNodes = new();
    public List<StoryNodeItem> ProblemNodes 
    {
        get => _problemNodes;
        set => SetProperty(ref _problemNodes, value);
    }

    private List<StoryNodeItem> _characterNodes = new();
    public List<StoryNodeItem> CharacterNodes
    {
        get => _characterNodes;
        set => SetProperty(ref _characterNodes, value);
    }

    private List<StoryNodeItem> _settingNodes = new();
    public List<StoryNodeItem> SettingNodes
    {
        get => _settingNodes;
        set => SetProperty(ref _settingNodes, value);
    }

    private List<StoryNodeItem> _sceneNodes = new();
    public List<StoryNodeItem> SceneNodes
    {
        get => _sceneNodes;
        set => SetProperty(ref _sceneNodes, value);
    }

    private List<StoryNodeItem> _webNodes = new();
    public List<StoryNodeItem> WebNodes
    {
        get => _webNodes;
        set => SetProperty(ref _webNodes, value);
    }
    #endregion

    public async Task OpenPrintReportDialog()
    {
        ShellViewModel ShellVM = _shellViewModel;
        var autoSaveService = Ioc.Default.GetRequiredService<AutoSaveService>();
        var backupService = Ioc.Default.GetRequiredService<BackupService>();
        var logService = _logService;
        using (var serializationLock = new SerializationLock(autoSaveService, backupService, logService))
        {
            if (_shellViewModel.OutlineManager.StoryModel?.CurrentView == null)
            {
                ShellVM.ShowMessage(LogLevel.Warn, "You need to load a Story first!", false);
                return;
            }

            ShellVM.ShowMessage(LogLevel.Info, "Generate Print Reports executing", true);
            ShellVM.SaveModel();

            // Run reports dialog
            var result = await Ioc.Default.GetService<Windowing>().ShowContentDialog(new()
            {
                Title = "Generate Reports",
                Content = new PrintReportsDialog(),
                PrimaryButtonText = "Confirm",
                SecondaryButtonText = "Cancel",
            });

            if (result == ContentDialogResult.Primary)
            {
                StartPrintMenu();
            }
        }
    }

    private async void StartPrintMenu()
    {
        GeneratePrintDocumentReport();
        PrintDocSource = Document.DocumentSource;

        // Old windows build check (pre 19045 doesn't support PrintManager)
        if (Environment.OSVersion.Version.Build <= 19045) 
        {
            await Window.ShowContentDialog(new ContentDialog()
            {
                Title = "Printing",
                Content = "Printing is not supported on this version of Windows, please update",
                PrimaryButtonText = "Ok"
            });
        }
        if (PrintManager.IsSupported())
        {
            try
            {   // Show print UI
                await PrintManagerInterop.ShowPrintUIForWindowAsync(Window.WindowHandle);
            }
            catch (Exception ex) //Error setting up printer
            {
                Window.GlobalDispatcher.TryEnqueue(async () =>
                {
                    ContentDialog Dialog = new()
                    {
                        Title = "Printing error",
                        Content = "The following error occurred when trying to print:\n\n" + ex.Message,
                        PrimaryButtonText = "Ok"
                    };

                    await Ioc.Default.GetService<Windowing>().ShowContentDialog(Dialog, true);
                });

            }
        }
        
        //Device somehow doesn't support printing; im not sure what exactly this means.
        else
        {
            await Window.ShowContentDialog(new ContentDialog()
            {
                Title = "Printing",
                Content = "Your device does not appear to support printing.",
                PrimaryButtonText = "Ok"
            });
        }
    }

    /// <summary>
    /// This traverses a node and adds it to the relevant list.
    /// </summary>
    /// <param name="node"></param>
    public void TraverseNode(StoryNodeItem node)
    {
        switch (node.Type)
        {
            case StoryItemType.Problem: ProblemNodes.Add(node); break;
            case StoryItemType.Character: CharacterNodes.Add(node); break;
            case StoryItemType.Setting: SettingNodes.Add(node); break;
            case StoryItemType.Scene: SceneNodes.Add(node); break;
            case StoryItemType.Web: WebNodes.Add(node); break;
        }

        //Recurs until children are empty 
        foreach (StoryNodeItem _storyNodeItem in node.Children) { TraverseNode(_storyNodeItem); }
    }

    /// <summary>
    /// This prints a report of the node selected
    /// </summary>
    /// <param name="elementItem">Node to be printed.</param>
    public void PrintSingleNode(StoryNodeItem elementItem)
    {
        SelectedNodes.Clear(); //Only print single node

        PrintReports _rpt = new(this, _outlineViewModel.StoryModel);

        if (elementItem.Type == StoryItemType.StoryOverview) { CreateOverview = true; }
        else { SelectedNodes.Add(elementItem); }

        StartPrintMenu();
        //Reset value incase dialog opened later
        SelectedNodes.Clear();
        CreateOverview = false;
    }

    public void RegisterForPrint()
    {
        // Register for PrintTaskRequested event
        _printManager = PrintManagerInterop.GetForWindow(Window.WindowHandle);
        _printManager.PrintTaskRequested += PrintTaskRequested;
    }

    public async void GeneratePrintDocumentReport()
    {
        Document = new();
        _printPreviewCache = new();

        //Treat each page break as its own page.
        var report = await new PrintReports(this, _outlineViewModel.StoryModel).Generate();

		foreach (string pageText in report.Split(@"\PageBreak"))
        {
            //Wrap pages
            string[] Lines = pageText.Split('\n');
            List<string> WrappedPages = new();
            int linecount = 0;
            string currentpage = "";
            for (int i = 0; i < Lines.Length; i++)
            {
                if (linecount >= 70)
                {
                    currentpage += Lines[i];
                    WrappedPages.Add(currentpage);

                    //Reset counter
                    currentpage = "";
                    linecount = 0;

                }
                else
                {
                    currentpage += Lines[i];
                    linecount++;
                }
            }
            WrappedPages.Add(currentpage);

            //We specify black text as it will default to white on dark mode, making it look like nothing was printed
            //(and leading to a week of wondering why noting was being printed.). 
            foreach (string Page in WrappedPages)
            {
                if (!String.IsNullOrWhiteSpace(Page))
                {
                    StackPanel panel = new()
                    {
                        Children =
                        {
                            new TextBlock {
                                Text = Page,
                                Foreground = new SolidColorBrush(Colors.Black),
                                Margin = new(120, 50, 0, 0),
                                HorizontalAlignment = HorizontalAlignment.Left,
                                FontSize = 10
                            }
                        }
                    };

                    _printPreviewCache.Add(panel); //Add page to cache.  
                }
            }
        }

        //Add the text as pages.
        Document.AddPages += AddPages;

        //Fetch preview page
        Document.GetPreviewPage += GetPreviewPage;

        //As each page gets added through AddPages, and keeps preview count in line 
        Document.Paginate += Paginate;
    }

    private void Paginate(object sender, PaginateEventArgs e)
    {
        Document.SetPreviewPageCount(_printPreviewCache.Count, 
            PreviewPageCountType.Intermediate);
    }

    private void GetPreviewPage(object sender, GetPreviewPageEventArgs e)
    {
        //Try Catch as this code may be called before AddPages is done.
        try
        {
            Document.SetPreviewPage(e.PageNumber, _printPreviewCache[e.PageNumber - 1]);
        }
        catch { }
    }

    private void AddPages(object sender, AddPagesEventArgs e)
    {
        //Treat each page break as a new page
        foreach (StackPanel page in _printPreviewCache) { Document.AddPage(page); }

        //All text has been handled, so we mark add pages as complete.
        Document.AddPagesComplete();
        Document.SetPreviewPage(0, _printPreviewCache[0]);

        //Set preview count
        Document.SetPreviewPageCount(_printPreviewCache.Count, PreviewPageCountType.Final);
    }

    /// <summary>
    /// This creates a print task and handles it failure/completion
    /// </summary>
    private void PrintTaskRequested(PrintManager sender, PrintTaskRequestedEventArgs args)
    {
        try
        {
            //Set print job name
            PrintJobManager = args.Request.CreatePrintTask("StoryCAD - " + 
                Path.GetFileNameWithoutExtension(_outlineViewModel.StoryModelFile)
                ,PrintSourceRequested);
            PrintJobManager.Completed += PrintTaskCompleted; //Show message if job failed.
        }
        catch (Exception e)
        {
            _logService.LogException(LogLevel.Error, e, "Error trying to print report");
        }
    }

    /// <summary>
    /// Set print source
    /// </summary>
    private void PrintSourceRequested(PrintTaskSourceRequestedArgs args)
    {
        args.SetSource(PrintDocSource);
    }

    /// <summary>
    /// Fired when the print task is completed/failed.
    /// </summary>
    private void PrintTaskCompleted(PrintTask sender, PrintTaskCompletedEventArgs args)
    {
        Window.GlobalDispatcher.TryEnqueue(async () =>
        {
            if (args.Completion == PrintTaskCompletion.Failed) //Show message if print fails
            {
                ContentDialog Dialog = new()
                {
                    Title = "Printing error",
                    Content = "An error occurred trying to print your document.",
                    PrimaryButtonText = "OK"
                };
                await Window.ShowContentDialog(Dialog, true);
            }

            _printManager.PrintTaskRequested -= PrintTaskRequested;
            Document.AddPages -= AddPages;
            Document.GetPreviewPage -= GetPreviewPage;
            Document.Paginate -= Paginate;
            Document = null;
        });
    }
}