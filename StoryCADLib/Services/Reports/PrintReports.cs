using System.Text;
﻿using System.Drawing;
using System.Drawing.Printing;
using System.Text;
using StoryCAD.Services.Outline;
using StoryCAD.ViewModels.Tools;

namespace StoryCAD.Services.Reports;

public class PrintReports
{
    private PrintReportDialogVM _vm;
    private StoryModel _model;
    private ReportFormatter _formatter;
    private string _documentText;
    private LogService logger = Ioc.Default.GetRequiredService<LogService>();

    public async Task<string> Generate()
    {
        string rtf = string.Empty;
        await _formatter.LoadReportTemplates(); // Load text report templates

        //Process single selection (non-Pivot) reports
        if (_vm.CreateOverview)
        {
            rtf = _formatter.FormatStoryOverviewReport();
            _documentText += FormatText(rtf);
        }
        if (_vm.CreateSummary)
        {
            rtf = _formatter.FormatSynopsisReport();
            _documentText += FormatText(rtf, true);
        }

        if (_vm.CreateStructure)
        {
	        rtf = _formatter.FormatStoryProblemStructureReport();
	        _documentText += FormatText(rtf);
		}

		if (_vm.ProblemList)
        {
            rtf = _formatter.FormatListReport(StoryItemType.Problem);
            _documentText += FormatText(rtf);
        }
        if (_vm.CharacterList)
        {
            rtf = _formatter.FormatListReport(StoryItemType.Character);
            _documentText += FormatText(rtf);
        }
        if (_vm.SettingList)
        {
            rtf = _formatter.FormatListReport(StoryItemType.Setting);
            _documentText += FormatText(rtf);
        }
        if (_vm.SceneList)
        {
            rtf = _formatter.FormatListReport(StoryItemType.Scene);
            _documentText += FormatText(rtf);
        }
        if (_vm.WebList)
        {
            rtf = _formatter.FormatListReport(StoryItemType.Web);
            _documentText += FormatText(rtf);
        }

        foreach (StoryNodeItem node in _vm.SelectedNodes)
        {
            StoryElement element = null;
            var outlineService = Ioc.Default.GetRequiredService<OutlineService>();
            try
            {
                element = outlineService.GetStoryElementByGuid(_model, node.Uuid);
            }
            catch (InvalidOperationException)
            {
                // Element not found, element remains null
            }
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
                _documentText += FormatText(rtf);
            }
        }

        if (string.IsNullOrEmpty(_documentText))
        {
            logger.Log(LogLevel.Warn, "No nodes selected for report generation");
            return "";
        }
        return _documentText;
    }

    /// <summary>
    /// Formats text for a report, if SummaryMode is set to true
    /// then some formatting is changed to make summary reports more pleasant
    /// </summary>
    /// <param name="rtfInput"></param>
    /// <param name="summaryMode"></param>
    private string FormatText(string rtfInput, bool summaryMode = false)
    {
        const int MaxLineLength = 72;
        
        string text = _formatter.GetText(rtfInput, false);
        string[] lines = text.Split('\n');
        StringBuilder sb = new();

        foreach (string inputLine in lines)
        {
            string line = inputLine.TrimEnd();
            if (line.Equals("\r"))
                continue;

            WrapAndAppendLine(sb, line, MaxLineLength);
        }

        if (summaryMode)
        {
            sb.Replace("[", "\r\n[");
            sb.Replace("]", "]\r\n");
        }
        
        sb.Append("\n\\PageBreak\n");
        return sb.ToString();
    }

    /// <summary>
    /// Wraps a line to the specified maximum length and appends to StringBuilder
    /// </summary>
    /// <param name="sb">StringBuilder to append to</param>
    /// <param name="line">Line to wrap</param>
    /// <param name="maxLength">Maximum line length</param>
    private void WrapAndAppendLine(StringBuilder sb, string line, int maxLength)
    {
        while (line.Length > maxLength)
        {
            string segment = line[..maxLength];
            
            // Ensure we have a space for wrapping
            if (!segment.Contains(" "))
            {
                segment += " ";
            }
            
            int lastSpaceIndex = segment.LastIndexOf(' ');
            string wrappedPortion = segment[..lastSpaceIndex];
            
            sb.Append(wrappedPortion);
            sb.Append(Environment.NewLine);
            
            line = line[lastSpaceIndex..].TrimStart();
        }
        
        sb.Append(line + Environment.NewLine);
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