using Windows.Graphics.Printing;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using SkiaSharp;
using StoryCADLib.Services;
using StoryCADLib.Services.Dialogs.Tools;
using StoryCADLib.Services.Locking;
using StoryCADLib.Services.Messages;
using StoryCADLib.Services.Reports;

namespace StoryCADLib.ViewModels.Tools;

public partial class PrintReportDialogVM : ObservableRecipient
{
    public ContentDialog Dialog;
    public PrintTask PrintJobManager;

#pragma warning disable CS0169 // Field is used in WinAppSDK platform-specific partial class
    private bool _isPrinting;
#pragma warning restore CS0169

    private readonly AppState _appState;
    private readonly Windowing Window;
    private readonly EditFlushService _editFlushService;
    private readonly ILogService _logService;
    private List<StackPanel> _printPreviewCache;

    public enum ReportOutputMode
    {
        Print,
        Pdf
    }

    private const int LinesPerPage = 70;
    private const float PdfPageWidth = 612f;
    private const float PdfPageHeight = 792f;
    private const float PdfMarginLeft = 90f;
    private const float PdfMarginTop = 38f;
    private const float PdfMarginBottom = 38f;

    private const float PdfFontSize = 10f;

    public PrintReportDialogVM(AppState appState, Windowing window, EditFlushService editFlushService,
        ILogService logService)
    {
        _appState = appState;
        Window = window;
        _editFlushService = editFlushService;
        _logService = logService;
        _printPreviewCache = new List<StackPanel>();
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

    public Task OpenPrintReportDialog() => OpenPrintReportDialog(ReportOutputMode.Print);

    public async Task OpenPrintReportDialog(ReportOutputMode mode)
    {
        using (var serializationLock = new SerializationLock(_logService))
        {
            if (_appState.CurrentDocument?.Model?.CurrentView.Count == 0)
            {
                Messenger.Send(
                    new StatusChangedMessage(new StatusMessage("You need to load a Story first!", LogLevel.Warn)));
                return;
            }

            Messenger.Send(
                new StatusChangedMessage(new StatusMessage("Generate reports executing", LogLevel.Info, true)));
            _editFlushService.FlushCurrentEdits();

            var result = await Window.ShowContentDialog(new ContentDialog
            {
                Title = "Generate Reports",
                Content = new PrintReportsDialog(this, _appState, _logService),
                PrimaryButtonText = "Confirm",
                SecondaryButtonText = "Cancel"
            });

            if (result == ContentDialogResult.Primary)
            {
                if (mode == ReportOutputMode.Print)
                {
                    StartPrintMenu();
                }
                else
                {
                    await ExportReportsToPdfAsync();
                }
            }
        }
    }

#if !WINDOWS10_0_18362_0_OR_GREATER
    private async void StartPrintMenu()
    {
        await ExportReportsToPdfAsync();
    }
#endif


    private async Task<IReadOnlyList<IReadOnlyList<string>>> BuildReportPagesAsync(int? linesPerPageOverride = null)
    {
        var report = await new PrintReports(this, _appState).Generate();
        return BuildReportPages(report, linesPerPageOverride);
    }

    private static IReadOnlyList<IReadOnlyList<string>> BuildReportPages(string report,
        int? linesPerPageOverride = null)
    {
        var pages = new List<IReadOnlyList<string>>();
        var linesPerPage = Math.Max(1, linesPerPageOverride ?? LinesPerPage);

        if (string.IsNullOrWhiteSpace(report))
        {
            pages.Add(new List<string> { "(Empty report)" });
            return pages;
        }

        var rawPages = report.Split(new[] { "\\PageBreak" }, StringSplitOptions.None);

        foreach (var rawPage in rawPages)
        {
            var lines = rawPage.Split('\n');
            var currentPage = new List<string>(linesPerPage);

            void FlushPage()
            {
                if (currentPage.Count == 0)
                {
                    return;
                }

                if (currentPage.Any(line => !string.IsNullOrWhiteSpace(line)))
                {
                    pages.Add(new List<string>(currentPage));
                }

                currentPage.Clear();
            }

            foreach (var line in lines)
            {
                currentPage.Add(line.Replace("\r", string.Empty));
                if (currentPage.Count >= linesPerPage)
                {
                    FlushPage();
                }
            }

            FlushPage();
        }

        if (pages.Count == 0)
        {
            pages.Add(new List<string> { "(Empty report)" });
        }

        return pages;
    }


#if WINDOWS10_0_18362_0_OR_GREATER
    // GeneratePrintDocumentReportAsync is defined in PrintReportDialogVM.WinAppSDK.cs
#else
    public Task GeneratePrintDocumentReportAsync() => Task.CompletedTask;
#endif

    private async Task ExportReportsToPdfAsync()
    {
        try
        {
            var exportFile = await Window.ShowFileSavePicker("Export", ".pdf");
            if (exportFile is null)
            {
                Messenger.Send(new StatusChangedMessage(new StatusMessage("PDF export cancelled", LogLevel.Info)));
                return;
            }

            using var memoryStream = new MemoryStream();
            using (var document = SKDocument.CreatePdf(memoryStream))
            {
                if (document is null)
                {
                    Messenger.Send(new StatusChangedMessage(
                        new StatusMessage("Unable to create a PDF document for export.", LogLevel.Error)));
                    await Window.ShowContentDialog(new ContentDialog
                    {
                        Title = "Export error",
                        Content = "Unable to create a PDF document for export.",
                        PrimaryButtonText = "OK"
                    }, true);
                    return;
                }

                using var font = new SKFont(SKTypeface.Default, PdfFontSize);
                using var paint = new SKPaint
                {
                    Color = SKColors.Black,
                    IsAntialias = true
                };

                var lineHeight = font.Spacing;
                if (lineHeight <= 0)
                {
                    lineHeight = PdfFontSize * 1.2f;
                }

                var printableHeight = PdfPageHeight - PdfMarginTop - PdfMarginBottom;
                if (printableHeight <= 0)
                {
                    printableHeight = PdfPageHeight;
                }

                var linesPerPdfPage = Math.Max(1, (int)Math.Floor(printableHeight / lineHeight));
                var pages = await BuildReportPagesAsync(linesPerPdfPage);

                foreach (var pageLines in pages)
                {
                    var lineIndex = 0;

                    while (lineIndex < pageLines.Count)
                    {
                        var canvas = document.BeginPage(PdfPageWidth, PdfPageHeight);
                        if (canvas is null)
                        {
                            break;
                        }

                        try
                        {
                            var y = PdfMarginTop + lineHeight;

                            while (lineIndex < pageLines.Count)
                            {
                                canvas.DrawText(pageLines[lineIndex] ?? string.Empty, PdfMarginLeft, y, SKTextAlign.Left, font, paint);
                                lineIndex++;

                                if (lineIndex >= pageLines.Count)
                                {
                                    break;
                                }

                                var nextY = y + lineHeight;
                                if (nextY > PdfPageHeight - PdfMarginBottom)
                                {
                                    break;
                                }

                                y = nextY;
                            }
                        }
                        finally
                        {
                            document.EndPage();
                            canvas.Dispose();
                        }
                    }
                }

                document.Close();
            }

            await FileIO.WriteBytesAsync(exportFile, memoryStream.ToArray());
            Messenger.Send(new StatusChangedMessage(new StatusMessage($"Reports exported to PDF: {exportFile.Path}",
                LogLevel.Info)));
        }
        catch (Exception ex)
        {
            _logService.LogException(LogLevel.Error, ex, "Failed to export reports to PDF.");
            Messenger.Send(new StatusChangedMessage(new StatusMessage("PDF export failed", LogLevel.Error)));
            await Window.ShowContentDialog(new ContentDialog
            {
                Title = "Export error",
                Content = "The following error occurred while exporting to PDF:\n\n" + ex.Message,
                PrimaryButtonText = "OK"
            }, true);
        }
    }


    /// <summary>Traverse a node and add it to the relevant list.</summary>
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

        foreach (var child in node.Children)
        {
            TraverseNode(child);
        }
    }

    /// <summary>Print only the passed node.</summary>
    public void PrintSingleNode(StoryNodeItem elementItem)
    {
        // Check if an outline is open before proceeding
        if (_appState.CurrentDocument?.Model == null)
        {
            Messenger.Send(
                new StatusChangedMessage(new StatusMessage("You need to load a Story first!", LogLevel.Warn)));
            return;
        }

        SelectedNodes.Clear();

        if (elementItem.Type == StoryItemType.StoryOverview)
        {
            CreateOverview = true;
        }
        else
        {
            SelectedNodes.Add(elementItem);
        }

        StartPrintMenu();

        SelectedNodes.Clear();
        CreateOverview = false;
    }
}
