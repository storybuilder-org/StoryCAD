using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using StoryCAD.Models.Tools;

namespace StoryCAD.ViewModels.Tools;

public class BeatSheetsViewModel : ObservableRecipient
{
    private ToolsData ToolSource = Ioc.Default.GetService<ToolsData>();
    #region Properties

    private string _PlotPatternName;
    public string PlotPatternName
    {
        get => _PlotPatternName;
        set
        {
            SetProperty(ref _PlotPatternName, value);
            if (BeatSheets.ContainsKey(value)) { PlotPatternNotes = BeatSheets[value].PlotPatternNotes; }
        }
    }

    private string _PlotPatternNotes;
    public string PlotPatternNotes
    {
        get => _PlotPatternNotes;
        set => SetProperty(ref _PlotPatternNotes, value);
    }

    #endregion

    #region ComboBox and ListBox sources

    public readonly ObservableCollection<string> PlotPatternNames;

    public readonly Dictionary<string, PlotPatternModel> BeatSheets;

    #endregion

    #region Constructor

    public BeatSheetsViewModel()
    {
        List<string> _beatSheetNames = new();
        BeatSheets = new Dictionary<string, PlotPatternModel>();
        foreach (PlotPatternModel _plot in ToolSource.BeatSheetSource)
        {
            _beatSheetNames.Add(_plot.PlotPatternName);
            BeatSheets.Add(_plot.PlotPatternName, _plot);
        }

        _beatSheetNames.Sort();
        PlotPatternNames = new ObservableCollection<string>();
        foreach (string _name in _beatSheetNames) { PlotPatternNames.Add(_name); }
        PlotPatternName = ToolSource.MasterPlotsSource[0].PlotPatternName;
    }

    #endregion
}