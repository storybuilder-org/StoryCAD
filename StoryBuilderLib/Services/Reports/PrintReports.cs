using System;
using System.Text;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.DependencyInjection;
using StoryBuilder.Models;
using StoryBuilder.Services.Logging;
using StoryBuilder.ViewModels;
using StoryBuilder.ViewModels.Tools;

namespace StoryBuilder.Services.Reports;

public class PrintReports
{
    private PrintReportDialogVM _vm;
    private StoryModel _model;
    private ReportFormatter _formatter;
    private StringReader fileStream;
    private Font printFont;
    private string documentText;

    //PrintHelper helper = new();

    public async Task Generate()
    {
        string rtf = string.Empty;
        await _formatter.LoadReportTemplates(); // Load text report templates

        //Process single selection (non-Pivot) reports
        if (_vm.CreateOverview)
        {
            rtf = _formatter.FormatStoryOverviewReport(Overview());
            documentText = FormatText(rtf);
            Print(documentText);
        }
        if (_vm.CreateSummary)
        {
            rtf =_formatter.FormatSynopsisReport();
            documentText = FormatText(rtf,true);
            Print(documentText);
        }

        if (_vm.ProblemList)
        {
            rtf = _formatter.FormatProblemListReport();
            documentText = FormatText(rtf);
            Print(documentText);
        }
        if (_vm.CharacterList)
        {
            rtf = _formatter.FormatCharacterListReport();
            documentText = FormatText(rtf);
            Print(documentText);
        }
        if (_vm.SettingList)
        {
            rtf = _formatter.FormatSettingListReport();
            documentText = FormatText(rtf);
            Print(documentText);
        }
        if (_vm.SceneList)
        {
            rtf = _formatter.FormatSceneListReport();
            documentText = FormatText(rtf);
            Print(documentText);
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
                }
                documentText = FormatText(rtf);
                Print(documentText);
            }
        }
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
        while (count < linesPerPage &&
               (line = fileStream.ReadLine()) != null)
        {
            float yPos = topMargin + count * printFont.GetHeight(ev.Graphics);
            ev.Graphics.DrawString(line, printFont, Brushes.Black, leftMargin, yPos, new StringFormat());
            count++;
        }

        // If more lines exist, print another page.
        if (line != null)
            ev.HasMorePages = true;
        else
            ev.HasMorePages = false;
    }

    // Print the file.
    public void Print(string file)
    {
        try
        {
            fileStream = new StringReader(file);
            printFont = new Font("Arial", 12, FontStyle.Regular, GraphicsUnit.Pixel);
            PrintDocument pd = new();
            pd.PrintPage += new(pd_PrintPage);
            Margins margins = new(100, 100, 100, 100);
            pd.DefaultPageSettings.Margins = margins;
            float pixelsPerChar = printFont.Size;
            float lineWidth = pd.DefaultPageSettings.PrintableArea.Width;
            int charsPerLine = Convert.ToInt32(lineWidth / pixelsPerChar);
            // Print the document.
            pd.Print();
        }
        catch (Exception ex)
        {
            Ioc.Default.GetService<LogService>().LogException(LogLevel.Error, ex, "Error in Print reports.");
        }
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

        foreach (string t in lines)
        {
            string line = t;
            while (line.Length > 72) 
            {
                string temp = line[..72];
                int j = temp.LastIndexOf(' ');
                temp = temp[..j];
                sb.Append(temp);
                sb.Append('\n');
                line = line[j..];
                line = line.TrimStart();
            }
            sb.Append(line.Trim());
            if (!SummaryMode) { sb.Append('\n'); }
        }

        if (SummaryMode)
        {
            sb.Replace("[", "\n\n[");
            sb.Replace("]", "]\n");
        }
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