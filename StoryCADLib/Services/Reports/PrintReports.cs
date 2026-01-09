using System.Text;
using StoryCADLib.Services.Outline;
using StoryCADLib.ViewModels.Tools;

namespace StoryCADLib.Services.Reports;

public class PrintReports
{
    private readonly AppState _appState;
    private readonly ReportFormatter _formatter;
    private readonly ILogService _logger;
    private readonly StoryModel _model;
    private readonly PrintReportDialogVM _vm;
    private string _documentText;

    public PrintReports(ILogService logger)
    {
        _logger = logger;
    }

    #region Constructor

    public PrintReports(PrintReportDialogVM vm, AppState appState, ILogService logger)
    {
        _vm = vm;
        _appState = appState;
        _logger = logger;
        _model = appState.CurrentDocument!.Model;
        _formatter = new ReportFormatter(appState);
    }

    #endregion

    public async Task<string> Generate()
    {
        var rtf = string.Empty;

        //Process single selection (non-Pivot) reports
        if (_vm.CreateOverview)
        {
            rtf = await _formatter.FormatStoryOverviewReport();
            _documentText += FormatText(rtf);
        }

        if (_vm.CreateSummary)
        {
            rtf = await _formatter.FormatSynopsisReport();
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

        foreach (var node in _vm.SelectedNodes)
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
                        rtf = await _formatter.FormatProblemReport(element);
                        break;
                    case StoryItemType.Character:
                        rtf = await _formatter.FormatCharacterReport(element);
                        break;
                    case StoryItemType.Setting:
                        rtf = await _formatter.FormatSettingReport(element);
                        break;
                    case StoryItemType.Scene:
                        rtf = await _formatter.FormatSceneReport(element);
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
            _logger.Log(LogLevel.Warn, "No nodes selected for report generation");
            return "";
        }

        return _documentText;
    }

    /// <summary>
    ///     Formats text for a report, if SummaryMode is set to true
    ///     then some formatting is changed to make summary reports more pleasant
    /// </summary>
    /// <param name="rtfInput"></param>
    /// <param name="summaryMode"></param>
    private string FormatText(string rtfInput, bool summaryMode = false)
    {
        const int MaxLineLength = 72;

        var text = _formatter.GetText(rtfInput, false);
        var lines = text.Split('\n');
        StringBuilder sb = new();

        foreach (var inputLine in lines)
        {
            var line = inputLine.TrimEnd();
            if (line.Equals("\r"))
            {
                continue;
            }

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
    ///     Wraps a line to the specified maximum length and appends to StringBuilder
    /// </summary>
    /// <param name="sb">StringBuilder to append to</param>
    /// <param name="line">Line to wrap</param>
    /// <param name="maxLength">Maximum line length</param>
    private void WrapAndAppendLine(StringBuilder sb, string line, int maxLength)
    {
        while (line.Length > maxLength)
        {
            var segment = line[..maxLength];

            // Ensure we have a space for wrapping
            if (!segment.Contains(" "))
            {
                segment += " ";
            }

            var lastSpaceIndex = segment.LastIndexOf(' ');
            var wrappedPortion = segment[..lastSpaceIndex];

            sb.Append(wrappedPortion);
            sb.Append(Environment.NewLine);

            line = line[lastSpaceIndex..].TrimStart();
        }

        sb.Append(line + Environment.NewLine);
    }
}
