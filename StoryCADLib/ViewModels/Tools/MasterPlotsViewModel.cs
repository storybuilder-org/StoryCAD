using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using StoryCAD.Models.Tools;

namespace StoryCAD.ViewModels.Tools;

public class MasterPlotsViewModel : ObservableRecipient
{
    private readonly ToolsData _toolsData;

    #region Properties

    private string _PlotPatternName;

    public string PlotPatternName
    {
        get => _PlotPatternName;
        set
        {
            SetProperty(ref _PlotPatternName, value);
            if (MasterPlots.ContainsKey(value))
            {
                PlotPatternNotes = MasterPlots[value].PlotPatternNotes;
            }
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

    // Constructor for XAML compatibility - will be removed later
    public MasterPlotsViewModel() : this(Ioc.Default.GetRequiredService<ToolsData>())
    {
    }

    public MasterPlotsViewModel(ToolsData toolsData)
    {
        _toolsData = toolsData;
        List<string> _masterNames = new();
        MasterPlots = new Dictionary<string, PlotPatternModel>();
        foreach (var _plot in _toolsData.MasterPlotsSource)
        {
            _masterNames.Add(_plot.PlotPatternName);
            MasterPlots.Add(_plot.PlotPatternName, _plot);
        }

        _masterNames.Sort();
        PlotPatternNames = new ObservableCollection<string>();
        foreach (var _name in _masterNames)
        {
            PlotPatternNames.Add(_name);
        }

        PlotPatternName = _toolsData.MasterPlotsSource[0].PlotPatternName;
    }

    #endregion
}
