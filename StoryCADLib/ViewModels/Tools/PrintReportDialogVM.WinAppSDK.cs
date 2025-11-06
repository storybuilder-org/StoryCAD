#pragma warning disable CS8632 // Nullable annotations used without nullable context
using Windows.Graphics.Printing;
using Microsoft.UI;
using Microsoft.UI.Xaml.Printing;

namespace StoryCADLib.ViewModels.Tools;

public partial class PrintReportDialogVM
{
    private bool _printHandlerAttached;
    private PrintManager _printManager;
#pragma warning disable CS0414 // Field is used for coordination between async operations
    private volatile bool _printTaskCreated;
#pragma warning restore CS0414

    public PrintDocument? Document;
    public IPrintDocumentSource? PrintDocSource;

    private async void StartPrintMenu()
    {
        if (_isPrinting)
        {
            return;
        }

        _isPrinting = true;
        _printTaskCreated = false;

        try
        {
            await GeneratePrintDocumentReportAsync().ConfigureAwait(false);

            if (PrintManager.IsSupported())
            {
                try
                {
                    RegisterForPrint();
                    await PrintManagerInterop.ShowPrintUIForWindowAsync(Window.WindowHandle);
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
        }
        finally
        {
            _isPrinting = false;
            UnregisterForPrint(); // Always cleanup, regardless of what happened
        }
    }

    public void RegisterForPrint()
    {
        _printManager ??= PrintManagerInterop.GetForWindow(Window.WindowHandle);
        if (!_printHandlerAttached)
        {
            _printManager.PrintTaskRequested += PrintTaskRequested;
            _printHandlerAttached = true;
        }
    }

    private void UnregisterForPrint()
    {
        if (_printManager is not null && _printHandlerAttached)
        {
            _printManager.PrintTaskRequested -= PrintTaskRequested;
            _printHandlerAttached = false;
        }
    }

    /// <summary>
    ///     Public method to check if PrintManager is registered and clean up if needed.
    ///     Used during window shutdown to prevent Win32 exceptions.
    /// </summary>
    public void EnsurePrintManagerCleanup()
    {
        if (_printHandlerAttached)
        {
            _logService.Log(LogLevel.Debug, "PrintManager cleanup required during window close");
            UnregisterForPrint();
        }
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
                    {
                        sourceArgs.SetSource(PrintDocSource);
                    }
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

            // UnregisterForPrint removed - cleanup now happens in StartPrintMenu finally block
            UnhookDocumentEvents();
            Document = null;
            PrintDocSource = null;
            _printTaskCreated = false;
        });
    }

    private void Paginate(object sender, PaginateEventArgs e)
    {
        if (Document is null)
        {
            return;
        }

        var count = Math.Max(1, _printPreviewCache.Count);
        Document.SetPreviewPageCount(count, PreviewPageCountType.Intermediate);
    }

    private void GetPreviewPage(object sender, GetPreviewPageEventArgs e)
    {
        if (Document is null)
        {
            return;
        }

        try
        {
            Document.SetPreviewPage(e.PageNumber, _printPreviewCache[e.PageNumber - 1]); // 1-based
        }
        catch
        {
        }
    }

    private void AddPages(object sender, AddPagesEventArgs e)
    {
        if (Document is null)
        {
            return;
        }

        foreach (var page in _printPreviewCache)
        {
            Document.AddPage(page);
        }

        Document.AddPagesComplete();

        if (_printPreviewCache.Count > 0)
        {
            Document.SetPreviewPage(1, _printPreviewCache[0]);
        }

        Document.SetPreviewPageCount(_printPreviewCache.Count, PreviewPageCountType.Final);
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

    public async Task GeneratePrintDocumentReportAsync()
    {
        UnhookDocumentEvents();

        var pages = await BuildReportPagesAsync();

        await RunOnUIAsync(() =>
        {
            Document = new PrintDocument();
            _printPreviewCache = new List<StackPanel>();

            foreach (var pageLines in pages)
            {
                var displayText = string.Join(Environment.NewLine, pageLines);
                var panel = new StackPanel
                {
                    Children =
                    {
                        new TextBlock
                        {
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
                        new TextBlock
                        {
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
