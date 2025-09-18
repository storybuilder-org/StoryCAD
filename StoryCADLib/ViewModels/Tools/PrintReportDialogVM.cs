using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Printing;
using StoryCAD.Services.Reports;
using Windows.Graphics.Printing;
using StoryCAD.Services.Dialogs.Tools;
using StoryCAD.ViewModels.SubViewModels;
using StoryCAD.Services.Locking;
using StoryCAD.Services.Messages;
using StoryCAD.Services;
using SkiaSharp;

namespace StoryCAD.ViewModels.Tools;

public class PrintReportDialogVM : ObservableRecipient
{
    public ContentDialog Dialog;
    public PrintTask PrintJobManager;

    private PrintManager _printManager;
    private bool _printHandlerAttached;
    private bool _isPrinting;
    private volatile bool _printTaskCreated;

    public PrintDocument? Document;
    public IPrintDocumentSource? PrintDocSource;

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
    private const float PdfFontSize = 10f;
    // Constructor for XAML compatibility
    public PrintReportDialogVM() : this(
        Ioc.Default.GetRequiredService<AppState>(),
        Ioc.Default.GetRequiredService<Windowing>(),
        Ioc.Default.GetRequiredService<EditFlushService>(),
        Ioc.Default.GetRequiredService<ILogService>())
    {
    }

    public PrintReportDialogVM(AppState appState, Windowing window, EditFlushService editFlushService, ILogService logService)
    {
        _appState = appState;
        Window = window;
        _editFlushService = editFlushService;
        _logService = logService;
        _printPreviewCache = new();
    }

    #region Properties
    private bool _createSummary;
    public bool CreateSummary { get => _createSummary; set => SetProperty(ref _createSummary, value); }
    private bool _createStructure;
    public bool CreateStructure { get => _createStructure; set => SetProperty(ref _createStructure, value); }
    private bool _selectAllProblems;
    public bool SelectAllProblems { get => _selectAllProblems; set => SetProperty(ref _selectAllProblems, value); }
    private bool _selectAllCharacters;
    public bool SelectAllCharacters { get => _selectAllCharacters; set => SetProperty(ref _selectAllCharacters, value); }
    private bool _selectAllScenes;
    public bool SelectAllScenes { get => _selectAllScenes; set => SetProperty(ref _selectAllScenes, value); }
    private bool _selectAllWeb;
    public bool SelectAllWeb { get => _selectAllWeb; set => SetProperty(ref _selectAllWeb, value); }
    private bool _selectAllSetting;
    public bool SelectAllSettings { get => _selectAllSetting; set => SetProperty(ref _selectAllSetting, value); }
    private bool _createOverview;
    public bool CreateOverview { get => _createOverview; set => SetProperty(ref _createOverview, value); }
    private bool _problemList;
    public bool ProblemList { get => _problemList; set => SetProperty(ref _problemList, value); }
    private bool _characterList;
    public bool CharacterList { get => _characterList; set => SetProperty(ref _characterList, value); }
    private bool _settingList;
    public bool SettingList { get => _settingList; set => SetProperty(ref _settingList, value); }
    private bool _sceneList;
    public bool SceneList { get => _sceneList; set => SetProperty(ref _sceneList, value); }
    private bool _webList;
    public bool WebList { get => _webList; set => SetProperty(ref _webList, value); }

    private List<StoryNodeItem> _selectedNodes = new();
    public List<StoryNodeItem> SelectedNodes { get => _selectedNodes; set => SetProperty(ref _selectedNodes, value); }
    private List<StoryNodeItem> _problemNodes = new();
    public List<StoryNodeItem> ProblemNodes { get => _problemNodes; set => SetProperty(ref _problemNodes, value); }
    private List<StoryNodeItem> _characterNodes = new();
    public List<StoryNodeItem> CharacterNodes { get => _characterNodes; set => SetProperty(ref _characterNodes, value); }
    private List<StoryNodeItem> _settingNodes = new();
    public List<StoryNodeItem> SettingNodes { get => _settingNodes; set => SetProperty(ref _settingNodes, value); }
    private List<StoryNodeItem> _sceneNodes = new();
    public List<StoryNodeItem> SceneNodes { get => _sceneNodes; set => SetProperty(ref _sceneNodes, value); }
    private List<StoryNodeItem> _webNodes = new();
    public List<StoryNodeItem> WebNodes { get => _webNodes; set => SetProperty(ref _webNodes, value); }
    #endregion

    public Task OpenPrintReportDialog() => OpenPrintReportDialog(ReportOutputMode.Print);

    public async Task OpenPrintReportDialog(ReportOutputMode mode)
    {
        using (var serializationLock = new SerializationLock(_logService))
        {
            if (_appState.CurrentDocument.Model?.CurrentView == null)
            {
                Messenger.Send(new StatusChangedMessage(new("You need to load a Story first!", LogLevel.Warn)));
                return;
            }

            Messenger.Send(new StatusChangedMessage(new("Generate reports executing", LogLevel.Info, true)));
            _editFlushService.FlushCurrentEdits();

            var result = await Window.ShowContentDialog(new()
            {
                Title = "Generate Reports",
                Content = new PrintReportsDialog(this, _appState, _logService),
                PrimaryButtonText = "Confirm",
                SecondaryButtonText = "Cancel",
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

    private async void StartPrintMenu()
    {
        if (_isPrinting) return;
        _isPrinting = true;
        _printTaskCreated = false;

        try
        {
            await GeneratePrintDocumentReportAsync().ConfigureAwait(false);

#if WINDOWS10_0_18362_0_OR_GREATER
            if (PrintManager.IsSupported())
            {
                try
                {
                    await RunOnUIAsync(async () =>
                    {
                        RegisterForPrint(); // must run on UI thread for this HWND
                        await Windows.Graphics.Printing.PrintManagerInterop
                            .ShowPrintUIForWindowAsync(Window.WindowHandle);
                    });
                }
                catch (Exception ex)
                {
                    await Window.ShowContentDialog(new ContentDialog
                    {
                        Title = "Printing error",
                        Content = "The following error occurred when trying to print:\n\n" + ex.Message,
                        PrimaryButtonText = "Ok"
                    }, true);
                }
            }
            else
            {
                await Window.ShowContentDialog(new ContentDialog
                {
                    Title = "Printing",
                    Content = "Your device does not appear to support printing.",
                    PrimaryButtonText = "Ok"
                });
            }
#else
            await Window.ShowContentDialog(new ContentDialog
            {
                Title = "Printing",
                Content = "Printing is only available on the Windows head.",
                PrimaryButtonText = "Ok"
            });
#endif
        }
        finally
        {
            _isPrinting = false;
        }
    }

    public void RegisterForPrint()
    {
#if WINDOWS10_0_18362_0_OR_GREATER
        _printManager ??= Windows.Graphics.Printing.PrintManagerInterop.GetForWindow(Window.WindowHandle);
        if (!_printHandlerAttached)
        {
            _printManager.PrintTaskRequested += PrintTaskRequested;
            _printHandlerAttached = true;
        }
#endif
    }

    private async Task<IReadOnlyList<IReadOnlyList<string>>> BuildReportPagesAsync()
    {
        var report = await new PrintReports(this, _appState).Generate();
        return BuildReportPages(report);
    }

    private static IReadOnlyList<IReadOnlyList<string>> BuildReportPages(string report)
    {
        var pages = new List<IReadOnlyList<string>>();

        if (string.IsNullOrWhiteSpace(report))
        {
            pages.Add(new List<string> { "(Empty report)" });
            return pages;
        }

        var rawPages = report.Split(new[] { "\\PageBreak" }, StringSplitOptions.None);

        foreach (var rawPage in rawPages)
        {
            var lines = rawPage.Split('\n');
            var currentPage = new List<string>(LinesPerPage);

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
                if (currentPage.Count >= LinesPerPage)
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

    private void UnregisterForPrint()
    {
#if WINDOWS10_0_18362_0_OR_GREATER
        if (_printManager is not null && _printHandlerAttached)
        {
            _printManager.PrintTaskRequested -= PrintTaskRequested;
            _printHandlerAttached = false;
        }
#endif
    }

    // Build the document on the UI thread to avoid COM threading errors.
    public async Task GeneratePrintDocumentReportAsync()
    {
        UnhookDocumentEvents();

        var pages = await BuildReportPagesAsync();

        await RunOnUIAsync(() =>
        {
            Document = new();
            _printPreviewCache = new();

            foreach (var pageLines in pages)
            {
                var displayText = string.Join(Environment.NewLine, pageLines);
                var panel = new StackPanel
                {
                    Children =
                    {
                        new TextBlock {
                            Text = displayText,
                            Foreground = new SolidColorBrush(Colors.Black),
                            Margin = new Thickness(120, 50, 0, 0),
                            HorizontalAlignment = HorizontalAlignment.Left,
                            FontSize = 10
                        }
                    }
                };

                _printPreviewCache.Add(panel);
            }

            if (_printPreviewCache.Count == 0)
            {
                _printPreviewCache.Add(new StackPanel
                {
                    Children =
                    {
                        new TextBlock {
                            Text = "(Empty report)",
                            Foreground = new SolidColorBrush(Colors.Black),
                            Margin = new Thickness(120, 50, 0, 0),
                            HorizontalAlignment = HorizontalAlignment.Left,
                            FontSize = 10
                        }
                    }
                });
            }

            Document.AddPages += AddPages;
            Document.GetPreviewPage += GetPreviewPage;
            Document.Paginate += Paginate;

            PrintDocSource = Document.DocumentSource;
        });
    }

    private async Task ExportReportsToPdfAsync()
    {
        try
        {
            var pages = await BuildReportPagesAsync();

            var exportFile = await Window.ShowFileSavePicker("Export", ".pdf");
            if (exportFile is null)
            {
                Messenger.Send(new StatusChangedMessage(new("PDF export cancelled", LogLevel.Info)));
                return;
            }

            using var memoryStream = new MemoryStream();
            using (var document = SKDocument.CreatePdf(memoryStream))
            {
                if (document is null)
                {
                    Messenger.Send(new StatusChangedMessage(new("Unable to create a PDF document for export.", LogLevel.Error)));
                    await Window.ShowContentDialog(new ContentDialog
                    {
                        Title = "Export error",
                        Content = "Unable to create a PDF document for export.",
                        PrimaryButtonText = "OK"
                    }, true);
                    return;
                }

                using var paint = new SKPaint
                {
                    Color = SKColors.Black,
                    TextSize = PdfFontSize,
                    Typeface = SKTypeface.Default,
                    IsAntialias = true
                };

                var lineHeight = paint.FontSpacing;

                foreach (var pageLines in pages)
                {
                    var canvas = document.BeginPage(PdfPageWidth, PdfPageHeight);
                    if (canvas is null)
                    {
                        continue;
                    }

                    try
                    {
                        var y = PdfMarginTop + lineHeight;
                        foreach (var line in pageLines)
                        {
                            canvas.DrawText(line ?? string.Empty, PdfMarginLeft, y, paint);
                            y += lineHeight;
                        }
                    }
                    finally
                    {
                        document.EndPage();
                        canvas.Dispose();
                    }
                }

                document.Close();
            }

            await Windows.Storage.FileIO.WriteBytesAsync(exportFile, memoryStream.ToArray());
            Messenger.Send(new StatusChangedMessage(new($"Reports exported to PDF: {exportFile.Path}", LogLevel.Info)));
        }
        catch (Exception ex)
        {
            _logService.LogException(LogLevel.Error, ex, "Failed to export reports to PDF.");
            Messenger.Send(new StatusChangedMessage(new("PDF export failed", LogLevel.Error)));
            await Window.ShowContentDialog(new ContentDialog
            {
                Title = "Export error",
                Content = "The following error occurred while exporting to PDF:\n\n" + ex.Message,
                PrimaryButtonText = "OK"
            }, true);
        }
    }
    private void Paginate(object sender, PaginateEventArgs e)
    {
        if (Document is null) return;
        var count = Math.Max(1, _printPreviewCache.Count);
        Document.SetPreviewPageCount(count, PreviewPageCountType.Intermediate);
    }

    private void GetPreviewPage(object sender, GetPreviewPageEventArgs e)
    {
        if (Document is null) return;

        try
        {
            Document.SetPreviewPage(e.PageNumber, _printPreviewCache[e.PageNumber - 1]); // 1-based
        }
        catch { }
    }

    private void AddPages(object sender, AddPagesEventArgs e)
    {
        if (Document is null) return;

        foreach (StackPanel page in _printPreviewCache)
            Document.AddPage(page);

        Document.AddPagesComplete();

        if (_printPreviewCache.Count > 0)
            Document.SetPreviewPage(1, _printPreviewCache[0]);

        Document.SetPreviewPageCount(_printPreviewCache.Count, PreviewPageCountType.Final);
    }

    private void PrintTaskRequested(PrintManager sender, PrintTaskRequestedEventArgs args)
    {
        try
        {
            var deferral = args.Request.GetDeferral();

            PrintJobManager = args.Request.CreatePrintTask(
                "StoryCAD - " + Path.GetFileNameWithoutExtension(_appState.CurrentDocument!.FilePath),
                sourceArgs =>
                {
                    if (PrintDocSource is not null)
                        sourceArgs.SetSource(PrintDocSource);
                });

            _printTaskCreated = true;
            PrintJobManager.Completed += PrintTaskCompleted;

            deferral.Complete();
        }
        catch (Exception e)
        {
            _logService.LogException(LogLevel.Error, e, "Error trying to print report");
        }
    }

    private void PrintTaskCompleted(PrintTask sender, PrintTaskCompletedEventArgs args)
    {
        Window.GlobalDispatcher.TryEnqueue(async () =>
        {
            if (args.Completion == PrintTaskCompletion.Failed)
            {
                await Window.ShowContentDialog(new ContentDialog
                {
                    Title = "Printing error",
                    Content = "An error occurred trying to print your document.",
                    PrimaryButtonText = "OK"
                }, true);
            }

            UnregisterForPrint();
            UnhookDocumentEvents();
            Document = null;
            PrintDocSource = null;
            _printTaskCreated = false;
        });
    }

    private void UnhookDocumentEvents()
    {
        if (Document is not null)
        {
            Document.AddPages -= AddPages;
            Document.GetPreviewPage -= GetPreviewPage;
            Document.Paginate -= Paginate;
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

        foreach (StoryNodeItem child in node.Children)
            TraverseNode(child);
    }

    /// <summary>Print only the passed node.</summary>
    public void PrintSingleNode(StoryNodeItem elementItem)
    {
        SelectedNodes.Clear();

        var _ = new PrintReports(this, _appState);

        if (elementItem.Type == StoryItemType.StoryOverview) { CreateOverview = true; }
        else { SelectedNodes.Add(elementItem); }

        StartPrintMenu();

        SelectedNodes.Clear();
        CreateOverview = false;
    }

    // ---- UI thread helpers ----
    private Task RunOnUIAsync(Func<Task> func)
    {
        var tcs = new TaskCompletionSource<bool>();
        Window.GlobalDispatcher.TryEnqueue(async () =>
        {
            try
            {
                await func();
                tcs.SetResult(true);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        return tcs.Task;
    }

    private Task RunOnUIAsync(Action action)
    {
        var tcs = new TaskCompletionSource<bool>();
        Window.GlobalDispatcher.TryEnqueue(() =>
        {
            try
            {
                action();
                tcs.SetResult(true);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        return tcs.Task;
    }
}
