using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Printing;
using Octokit;
using StoryBuilder.Models;
using StoryBuilder.Services.Logging;
using StoryBuilder.ViewModels;
using StoryBuilder.ViewModels.Tools;
using Colors = Microsoft.UI.Colors;
using PrintDocument = Microsoft.UI.Xaml.Printing.PrintDocument;

namespace StoryBuilder.Services.Reports;

public class PrintReports
{
    private PrintReportDialogVM _vm;
    private StoryModel _model;
    private ReportFormatter _formatter;
    private StringReader fileStream;
    private Font printFont;
    private string documentText;

    PrintDocument _document = new();
    List<StackPanel> _printPreviewCache; //This stores a list of pages for print preview
    public async Task<string> Generate()
    {

        string rtf = string.Empty;
        await _formatter.LoadReportTemplates(); // Load text report templates

        //Process single selection (non-Pivot) reports
        if (_vm.CreateOverview)
        {
            rtf = _formatter.FormatStoryOverviewReport(Overview());
            documentText += FormatText(rtf);
        }
        if (_vm.CreateSummary)
        {
            rtf = _formatter.FormatSynopsisReport();
            documentText += FormatText(rtf, true);
        }

        if (_vm.ProblemList)
        {
            rtf = _formatter.FormatProblemListReport();
            documentText += FormatText(rtf);
        }
        if (_vm.CharacterList)
        {
            rtf = _formatter.FormatCharacterListReport();
            documentText += FormatText(rtf);
        }
        if (_vm.SettingList)
        {
            rtf = _formatter.FormatSettingListReport();
            documentText += FormatText(rtf);
        }
        if (_vm.SceneList)
        {
            rtf = _formatter.FormatSceneListReport();
            documentText += FormatText(rtf);
        }
        if (_vm.SceneList)
        {
            rtf = _formatter.FormatSceneListReport();
            documentText += FormatText(rtf);
        }
        if (_vm.WebList)
        {
            rtf = _formatter.FormatWebListReport();
            documentText += FormatText(rtf);
        }

        foreach (StoryNodeItem node in _vm.SelectedNodes)
        {
            StoryElement element = null;
            if (_model.StoryElements.StoryElementGuids.ContainsKey(node.Uuid))
                element = _model.StoryElements.StoryElementGuids[node.Uuid];
            if (element != null)
            {
                switch (node.Type)
                {
                    case StoryItemType.Problem:
                        rtf = _formatter.FormatProblemReport(element);
                        break;
                    case StoryItemType.Character:
                        rtf = _formatter.FormatCharacterReport(element);
                        break;
                    case StoryItemType.Setting:
                        rtf = _formatter.FormatSettingReport(element);
                        break;
                    case StoryItemType.Scene:
                        rtf = _formatter.FormatSceneReport(element);
                        break;
                    case StoryItemType.Web:
                        rtf = _formatter.FormatWebReport(element);
                        break;
                }
                documentText += FormatText(rtf);
            }
        }

        if (string.IsNullOrEmpty(documentText))
        {
            Ioc.Default.GetRequiredService<LogService>().Log(LogLevel.Warn, "No nodes selected for report generation");
            return "";
        }
        return documentText;
    }

    public async Task<PrintDocument> GenerateWinUIReport()
    {
        _document = new();
        _printPreviewCache = new();

        //Treat each page break as
        foreach (string pageText in (await Generate()).Split(@"\PageBreak"))
        {
            //We specify black text as it will default to white on dark mode, making it look like nothing was printed
            //(and leading to a week of wondering why noting was being printed.). 

            StackPanel panel = new()
            {
                Children =
                {
                    new TextBlock() {
                        Text = pageText,
                        Foreground = new SolidColorBrush(Colors.Black),
                        Margin = new(120, 50, 0, 0),
                        HorizontalAlignment = HorizontalAlignment.Left,
                        FontSize = 10
                    }
                }
            };

            _printPreviewCache.Add(panel); //Add page to cache.
        }


        //Add the text as pages.
        _document.AddPages += ((_, _) =>
        {
            //Treat each page break as
            foreach (StackPanel page in _printPreviewCache) { _document.AddPage(page); }

            //All text has been handled, so we mark add pages as complete.
            _document.AddPagesComplete();
            _document.SetPreviewPage(0, _printPreviewCache[0]);

            //Set preview count
            _document.SetPreviewPageCount(_printPreviewCache.Count, PreviewPageCountType.Final);
        });

        //Fetch preview page
        _document.GetPreviewPage += ((_, e) =>
        {
            try //Try Catched as this code may be called before AddPages is done.
            {
                _document.SetPreviewPage(e.PageNumber, _printPreviewCache[e.PageNumber - 1]);
            } catch { }
        });

        //As each page gets added through AddPages, and keeps preview count in line 
        _document.Paginate += (_, _) => { _document.SetPreviewPageCount(_printPreviewCache.Count, PreviewPageCountType.Intermediate); };  

        return _document;
    }

    private StoryElement Overview()
    {
        foreach (StoryElement element in _model.StoryElements)
            if (element.Type == StoryItemType.StoryOverview)
                return element;
        return null;
    }

    // The PrintPage event is raised for each page to be printed.
    private void pd_PrintPage(object sender, PrintPageEventArgs ev)
    {
        int count = 0;
        float leftMargin = ev.MarginBounds.Left;
        float topMargin = ev.MarginBounds.Top;
        string line = null;

        // Calculate the number of lines per page.
        float linesPerPage = ev.MarginBounds.Height / printFont.GetHeight(ev.Graphics);

        // Iterate over the file, printing each line.
        while (count < linesPerPage && (line = fileStream.ReadLine()) != null)
        {
            if (line == @"\PageBreak")
            {
                ev.HasMorePages = true;
                break;
            }
            float yPos = topMargin + count * printFont.GetHeight(ev.Graphics);
            ev.Graphics.DrawString(line, printFont, Brushes.Black, leftMargin, yPos, new StringFormat());
            count++;
        }

        // If more lines exist, print another page.
        if (line != null) { ev.HasMorePages = true; }
        else { ev.HasMorePages = false; }
    }

    /// <summary>
    /// Formats text for a report, if SummaryMode is set to true
    /// then some formatting is changed to make summary reports more pleasant
    /// </summary>
    /// <param name="rtfInput"></param>
    /// <param name="SummaryMode"></param>
    /// <returns></returns>
    private string FormatText(string rtfInput, bool SummaryMode = false)
    {
        string text = _formatter.GetText(rtfInput, false);
        string[] lines = text.Split('\n');
        StringBuilder sb = new();

        //TODO: rewrite this for readability and maintainability
        foreach (string t in lines)
        {
            string line = t.TrimEnd();
            if (line.Equals("\r"))
                continue;
            while (line.Length > 72)
            {
                string temp = line[..72];
                if (!temp.Contains(" ")) { temp += " "; } //wrapping fix
                int j = temp.LastIndexOf(' ');
                temp = temp[..j];
                sb.Append(temp);
                sb.Append(Environment.NewLine);
                line = line[j..];
                line = line.TrimStart();
            }
            sb.Append(line + Environment.NewLine);
        }
        if (SummaryMode)
        {
            sb.Replace("[", "\r\n[");
            sb.Replace("]", "]\r\n");
        }
        sb.Append("\n\\PageBreak\n");
        return sb.ToString();
    }

    #region Constructor

    public PrintReports(PrintReportDialogVM vm, StoryModel model)
    {
        _vm = vm;
        _model = model;
        _formatter = new ReportFormatter();
    }

    #endregion
}