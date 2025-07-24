using System.Text;
using StoryCAD.ViewModels.Tools;

namespace StoryCAD.Services.Reports;

public class PrintReports
{
    private PrintReportDialogVM _vm;
    private StoryModel _model;
    private ReportFormatter _formatter;
    private string _documentText;

    public async Task<string> Generate()
    {
        string rtf = string.Empty;
        await _formatter.LoadReportTemplates(); // Load text report templates

        //Process single selection (non-Pivot) reports
        if (_vm.CreateOverview)
        {
            rtf = _formatter.FormatStoryOverviewReport(Overview());
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
            rtf = _formatter.FormatProblemListReport();
            _documentText += FormatText(rtf);
        }
        if (_vm.CharacterList)
        {
            rtf = _formatter.FormatCharacterListReport();
            _documentText += FormatText(rtf);
        }
        if (_vm.SettingList)
        {
            rtf = _formatter.FormatSettingListReport();
            _documentText += FormatText(rtf);
        }
        if (_vm.SceneList)
        {
            rtf = _formatter.FormatSceneListReport();
            _documentText += FormatText(rtf);
        }
        if (_vm.SceneList)
        {
            rtf = _formatter.FormatSceneListReport();
            _documentText += FormatText(rtf);
        }
        if (_vm.WebList)
        {
            rtf = _formatter.FormatWebListReport();
            _documentText += FormatText(rtf);
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
                _documentText += FormatText(rtf);
            }
        }

        if (string.IsNullOrEmpty(_documentText))
        {
            Ioc.Default.GetRequiredService<LogService>().Log(LogLevel.Warn, "No nodes selected for report generation");
            return "";
        }
        return _documentText;
    }

    private StoryElement Overview()
    {
        return _model.StoryElements.FirstOrDefault(element => element.ElementType == StoryItemType.StoryOverview);
    }

    /// <summary>
    /// Formats text for a report, if SummaryMode is set to true
    /// then some formatting is changed to make summary reports more pleasant
    /// </summary>
    /// <param name="rtfInput"></param>
    /// <param name="summaryMode"></param>
    private string FormatText(string rtfInput, bool summaryMode = false)
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
        if (summaryMode)
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