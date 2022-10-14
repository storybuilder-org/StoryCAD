using System;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Text;
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
    private StringReader _fileStream;
    private Font _printFont;
    private string _documentText;
    private PrintDocument _printDoc = new();

    //PrintHelper helper = new();

    public async Task Generate()
    {
        string _Rtf = string.Empty;
        await _formatter.LoadReportTemplates(); // Load text report templates

        //Process single selection (non-Pivot) reports
        if (_vm.CreateOverview)
        {
            _Rtf = _formatter.FormatStoryOverviewReport(Overview());
            _documentText += FormatText(_Rtf);
        }
        if (_vm.CreateSummary)
        {
            _Rtf = _formatter.FormatSynopsisReport();
            //documentText = FormatText(rtf);
            _documentText += FormatText(_Rtf, true);
        }

        if (_vm.ProblemList)
        {
            _Rtf = _formatter.FormatProblemListReport();
            _documentText += FormatText(_Rtf);
        }
        if (_vm.CharacterList)
        {
            _Rtf = _formatter.FormatCharacterListReport();
            _documentText += FormatText(_Rtf);
        }
        if (_vm.SettingList)
        {
            _Rtf = _formatter.FormatSettingListReport();
            _documentText += FormatText(_Rtf);
        }
        if (_vm.SceneList)
        {
            _Rtf = _formatter.FormatSceneListReport();
            _documentText += FormatText(_Rtf);
        }
        if (_vm.SceneList)
        {
            _Rtf = _formatter.FormatSceneListReport();
            _documentText += FormatText(_Rtf);
        }
        if (_vm.WebList)
        {
            _Rtf = _formatter.FormatWebListReport();
            _documentText += FormatText(_Rtf);
        }

        foreach (StoryNodeItem _Node in _vm.SelectedNodes)
        {
            StoryElement _Element = null;
            if (_model.StoryElements.StoryElementGuids.ContainsKey(_Node.Uuid))
                _Element = _model.StoryElements.StoryElementGuids[_Node.Uuid];
            if (_Element != null)
            {
                switch (_Node.Type)
                {
                    case StoryItemType.Problem:
                        _Rtf = _formatter.FormatProblemReport(_Element);
                        break;
                    case StoryItemType.Character:
                        _Rtf = _formatter.FormatCharacterReport(_Element);
                        break;
                    case StoryItemType.Setting:
                        _Rtf = _formatter.FormatSettingReport(_Element);
                        break;
                    case StoryItemType.Scene:
                        _Rtf = _formatter.FormatSceneReport(_Element);
                        break;
                    case StoryItemType.Web:
                        _Rtf = _formatter.FormatWebReport(_Element);
                        break;
                }
                _documentText += FormatText(_Rtf);
            }
        }

        if (string.IsNullOrEmpty(_documentText))
        {
            Ioc.Default.GetRequiredService<ShellViewModel>().ShowMessage(LogLevel.Warn, "No nodes selected for report generation", true);
            return;
        }
        Print(_documentText);
        Ioc.Default.GetRequiredService<ShellViewModel>().ShowMessage(LogLevel.Info, "Generate Print Reports complete", true);

    }

    private StoryElement Overview()
    {
        foreach (StoryElement _Element in _model.StoryElements)
        {
            if (_Element.Type == StoryItemType.StoryOverview) { return _Element; }
        }
        return null;
    }

    // The PrintPage event is raised for each page to be printed.
    private void pd_PrintPage(object sender, PrintPageEventArgs ev)
    {
        int _Count = 0;
        float _LeftMargin = ev.MarginBounds.Left;
        float _TopMargin = ev.MarginBounds.Top;
        string _Line = null;

        // Calculate the number of lines per page.
        float _LinesPerPage = ev.MarginBounds.Height / _printFont.GetHeight(ev.Graphics);

        // Iterate over the file, printing each line.
        while (_Count < _LinesPerPage && (_Line = _fileStream.ReadLine()) != null)
        {
            if (_Line == @"\PageBreak")
            {
                ev.HasMorePages = true;
                break;
            }
            float _YPos = _TopMargin + _Count * _printFont.GetHeight(ev.Graphics);
            ev.Graphics.DrawString(_Line, _printFont, Brushes.Black, _LeftMargin, _YPos, new StringFormat());
            _Count++;
        }

        // If more lines exist, print another page.
        if (_Line != null) { ev.HasMorePages = true; }
        else { ev.HasMorePages = false; }
    }

    // Print the file.
    public void Print(string file)
    {
        try
        {
            _fileStream = new StringReader(file);
            _printFont = new Font("Arial", 12, FontStyle.Regular, GraphicsUnit.Pixel);
            _printDoc.PrintPage += pd_PrintPage;
            Margins _Margins = new(100, 100, 100, 100);
            _printDoc.DefaultPageSettings.Margins = _Margins;
            // Print the document.
            _printDoc.Print();
        }
        catch (Exception _Ex)
        {
            Ioc.Default.GetRequiredService<LogService>().LogException(LogLevel.Error, _Ex, "Error in Print reports.");
        }
    }
    /// <summary>
    /// Formats text for a report, if SummaryMode is set to true
    /// then some formatting is changed to make summary reports more pleasant
    /// </summary>
    /// <param name="rtfInput"></param>
    /// <param name="summaryMode"></param>
    /// <returns></returns>
    private string FormatText(string rtfInput, bool summaryMode = false)
    {
        string _Text = _formatter.GetText(rtfInput, false);
        string[] _Lines = _Text.Split('\n');
        StringBuilder _Sb = new();

        //TODO: rewrite this for readability and maintainability
        foreach (string _T in _Lines)
        {
            string _Line = _T.TrimEnd();
            if (_Line.Equals("\r"))
                continue;
            while (_Line.Length > 72)
            {
                string _Temp = _Line[..72];
                if (!_Temp.Contains(" ")) { _Temp += " "; } //wrapping fix
                int _J = _Temp.LastIndexOf(' ');
                _Temp = _Temp[.._J];
                _Sb.Append(_Temp);
                _Sb.Append(Environment.NewLine);
                _Line = _Line[_J..];
                _Line = _Line.TrimStart();
            }
            _Sb.Append(_Line + Environment.NewLine);
        }
        if (summaryMode)
        {
            _Sb.Replace("[", "\r\n[");
            _Sb.Replace("]", "]\r\n");
        }
        _Sb.Append("\n\\PageBreak\n");
        return _Sb.ToString();
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