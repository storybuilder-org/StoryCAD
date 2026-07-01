using System.Text;
using Windows.Graphics.Printing;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using SkiaSharp;
using StoryCADLib.Services;
using StoryCADLib.Services.Dialogs.Tools;
using StoryCADLib.Services.Locking;
using StoryCADLib.Services.Messages;
using StoryCADLib.Services.Outline;
using StoryCADLib.Services.Reports;

namespace StoryCADLib.ViewModels.Tools;

[Microsoft.UI.Xaml.Data.Bindable]
public partial class PrintReportDialogVM : ObservableRecipient
{
    public ContentDialog Dialog;
    public PrintTask PrintJobManager;

    private bool _isPrinting;

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

    private bool _createRelationships;

    public bool CreateRelationships
    {
        get => _createRelationships;
        set => SetProperty(ref _createRelationships, value);
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

    private bool _createStoryWorld;

    public bool CreateStoryWorld
    {
        get => _createStoryWorld;
        set => SetProperty(ref _createStoryWorld, value);
    }

    // Unassigned Elements and Plot Structure Diagram are currently generated
    // as part of Story Problem Structure. If they become separate reports,
    // add CreateUnassigned and CreatePlotDiagram bool properties here.

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

    private bool _includeImagesAppendix;

    /// <summary>
    ///     When set, the PDF report appends an "Images" section showing the
    ///     attached pictures (with captions) of each selected Character, Setting,
    ///     and Scene. No-op when no selected element has images.
    /// </summary>
    public bool IncludeImagesAppendix
    {
        get => _includeImagesAppendix;
        set => SetProperty(ref _includeImagesAppendix, value);
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

    private async Task<IReadOnlyList<IReadOnlyList<string>>> BuildReportPagesAsync(int? linesPerPageOverride = null)
    {
        var report = await new PrintReports(this, _appState, _logService).Generate();
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


    private async Task<byte[]> GeneratePdfBytesAsync()
    {
        using var memoryStream = new MemoryStream();
        using (var document = SKDocument.CreatePdf(memoryStream))
        {
            if (document is null)
            {
                return Array.Empty<byte>();
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

            if (IncludeImagesAppendix)
            {
                DrawImageAppendix(document, font, paint, lineHeight);
            }

            document.Close();
        }

        return memoryStream.ToArray();
    }

    /// <summary>
    ///     Appends an "Images" section to the PDF: for each selected Character,
    ///     Setting, and Scene that has attached pictures, draws a heading and the
    ///     images (scaled to the printable width) with their captions, paginating
    ///     onto fresh pages as needed. Images decode straight from their stored
    ///     bytes via SkiaSharp (WebP/PNG/JPEG), so no transcode is required.
    ///     No-op when no selected element has images.
    /// </summary>
    private void DrawImageAppendix(SKDocument document, SKFont font, SKPaint paint, float lineHeight)
    {
        var sections = GatherAppendixImages();
        if (sections.Count == 0)
        {
            return;
        }

        var pageBottom = PdfPageHeight - PdfMarginBottom;
        var maxImageWidth = PdfPageWidth - PdfMarginLeft * 2;
        var maxImageHeight = pageBottom - PdfMarginTop - lineHeight * 2;
        var sampling = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.None);

        using var headingFont = new SKFont(SKTypeface.Default, PdfFontSize + 2f);

        SKCanvas canvas = null;
        float y = 0;

        void StartPage()
        {
            canvas = document.BeginPage(PdfPageWidth, PdfPageHeight);
            y = PdfMarginTop + lineHeight;
        }

        void FinishPage()
        {
            document.EndPage();
            canvas?.Dispose();
            canvas = null;
        }

        StartPage();
        canvas.DrawText("Images", PdfMarginLeft, y, SKTextAlign.Left, headingFont, paint);
        y += lineHeight * 2;

        try
        {
            foreach (var section in sections)
            {
                // Keep a heading with at least its following image: break first if cramped.
                if (y + lineHeight * 2 > pageBottom)
                {
                    FinishPage();
                    StartPage();
                }

                canvas.DrawText(section.Heading, PdfMarginLeft, y, SKTextAlign.Left, headingFont, paint);
                y += lineHeight * 1.5f;

                foreach (var storyImage in section.Images)
                {
                    byte[] bytes = ImageService.GetBytes(storyImage);
                    if (bytes.Length == 0)
                    {
                        continue;
                    }

                    using SKData imageData = SKData.CreateCopy(bytes);
                    using SKImage image = SKImage.FromEncodedData(imageData);
                    if (image is null)
                    {
                        continue;
                    }

                    var (drawWidth, drawHeight) = ScaleToFit(image.Width, image.Height, maxImageWidth, maxImageHeight);

                    var captionLines = string.IsNullOrEmpty(storyImage.Caption)
                        ? new List<string>()
                        : WrapText(storyImage.Caption, font, maxImageWidth);
                    var blockHeight = drawHeight + captionLines.Count * lineHeight + lineHeight;

                    if (y + blockHeight > pageBottom)
                    {
                        FinishPage();
                        StartPage();
                    }

                    canvas.DrawImage(image, SKRect.Create(PdfMarginLeft, y, drawWidth, drawHeight), sampling, paint);
                    y += drawHeight + lineHeight * 0.25f;

                    foreach (var captionLine in captionLines)
                    {
                        y += lineHeight;
                        canvas.DrawText(captionLine, PdfMarginLeft, y, SKTextAlign.Left, font, paint);
                    }

                    y += lineHeight; // gap before the next image
                }
            }
        }
        finally
        {
            FinishPage();
        }
    }

    /// <summary>
    ///     Collects the selected Character/Setting/Scene elements that have
    ///     attached images, paired with a display heading, in selection order.
    /// </summary>
    internal List<(string Heading, List<StoryImage> Images)> GatherAppendixImages()
    {
        var sections = new List<(string, List<StoryImage>)>();
        var model = _appState.CurrentDocument?.Model;
        if (model is null)
        {
            return sections;
        }

        var outlineService = Ioc.Default.GetRequiredService<OutlineService>();
        foreach (var node in SelectedNodes)
        {
            StoryElement element = null;
            try
            {
                element = outlineService.GetStoryElementByGuid(model, node.Uuid);
            }
            catch (InvalidOperationException)
            {
                // Element not found; skip it.
            }
            catch (ArgumentException)
            {
                // Missing/invalid GUID on the node; skip it.
            }

            if (element is null)
            {
                continue;
            }

            List<StoryImage> images = element switch
            {
                CharacterModel character => character.Images,
                SettingModel setting => setting.Images,
                SceneModel scene => scene.Images,
                FolderModel folder => folder.Images,
                _ => null
            };

            if (images is { Count: > 0 })
            {
                sections.Add(($"{node.Type}: {element.Name}", images));
            }
        }

        return sections;
    }

    /// <summary>
    ///     Scales a source image to fit within the given bounds, preserving aspect
    ///     ratio and never enlarging it beyond its natural size. Pure; testable.
    /// </summary>
    internal static (float Width, float Height) ScaleToFit(int sourceWidth, int sourceHeight,
        float maxWidth, float maxHeight)
    {
        if (sourceWidth <= 0 || sourceHeight <= 0)
        {
            return (0f, 0f);
        }

        var scale = Math.Min(maxWidth / sourceWidth, maxHeight / sourceHeight);
        scale = Math.Min(scale, 1f); // don't upscale a small image into a blurry one
        return (sourceWidth * scale, sourceHeight * scale);
    }

    /// <summary>
    ///     Word-wraps text to fit within <paramref name="maxWidth"/> using the
    ///     given font's measured widths. Long single words are kept whole.
    /// </summary>
    private static List<string> WrapText(string text, SKFont font, float maxWidth)
    {
        var lines = new List<string>();
        var current = new StringBuilder();

        foreach (var word in text.Split(' '))
        {
            var candidate = current.Length == 0 ? word : current + " " + word;
            if (current.Length > 0 && font.MeasureText(candidate) > maxWidth)
            {
                lines.Add(current.ToString());
                current.Clear();
                current.Append(word);
            }
            else
            {
                current.Clear();
                current.Append(candidate);
            }
        }

        if (current.Length > 0)
        {
            lines.Add(current.ToString());
        }

        return lines;
    }

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

            var pdfBytes = await GeneratePdfBytesAsync();
            if (pdfBytes.Length == 0)
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

            await File.WriteAllBytesAsync(exportFile.Path, pdfBytes);
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
        else if (elementItem.Type == StoryItemType.StoryWorld)
        {
            CreateStoryWorld = true;
        }
        else
        {
            SelectedNodes.Add(elementItem);
        }

        StartPrintMenu();

        SelectedNodes.Clear();
        CreateOverview = false;
        CreateStoryWorld = false;
    }
}
