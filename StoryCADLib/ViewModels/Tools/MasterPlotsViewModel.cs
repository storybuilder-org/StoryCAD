using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using StoryCAD.Models.Tools;

namespace StoryCAD.ViewModels.Tools;

public class MasterPlotsViewModel : ObservableRecipient
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
            if (MasterPlots.ContainsKey(value)) { PlotPatternNotes = MasterPlots[value].PlotPatternNotes; }
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

    public readonly Dictionary<string, PlotPatternModel> MasterPlots;

    #endregion

    #region Constructor

    public MasterPlotsViewModel()
    {
        List<string> _masterNames = new();
        MasterPlots = new Dictionary<string, PlotPatternModel>();
        foreach (PlotPatternModel _plot in ToolSource.MasterPlotsSource)
        {
            _masterNames.Add(_plot.PlotPatternName);
            MasterPlots.Add(_plot.PlotPatternName, _plot);
        }

        _masterNames.Sort();
        PlotPatternNames = new ObservableCollection<string>();
        foreach (string _name in _masterNames) { PlotPatternNames.Add(_name); }
        PlotPatternName = ToolSource.MasterPlotsSource[0].PlotPatternName;
    }

    #endregion
}