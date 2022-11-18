using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using StoryBuilder.Models;
using StoryBuilder.Models.Tools;

namespace StoryBuilder.ViewModels.Tools;

public class MasterPlotsViewModel : ObservableRecipient
{
    #region Properties

    private string _masterPlotName;
    public string MasterPlotName
    {
        get => _masterPlotName;
        set
        {
            SetProperty(ref _masterPlotName, value);
            if (MasterPlots.ContainsKey(value)) { MasterPlotNotes = MasterPlots[value].MasterPlotNotes; }
        }
    }

    private string _masterPlotNotes;
    public string MasterPlotNotes
    {
        get => _masterPlotNotes;
        set => SetProperty(ref _masterPlotNotes, value);
    }

    #endregion

    #region ComboBox and ListBox sources

    public readonly ObservableCollection<string> MasterPlotNames;

    public readonly Dictionary<string, MasterPlotModel> MasterPlots;

    #endregion

    #region Constructor

    public MasterPlotsViewModel()
    {
        List<string> _masterNames = new();
        MasterPlots = new Dictionary<string, MasterPlotModel>();
        foreach (MasterPlotModel _plot in GlobalData.MasterPlotsSource)
        {
            _masterNames.Add(_plot.MasterPlotName);
            MasterPlots.Add(_plot.MasterPlotName, _plot);
        }

        _masterNames.Sort();
        MasterPlotNames = new ObservableCollection<string>();
        foreach (string _name in _masterNames) { MasterPlotNames.Add(_name); }
        MasterPlotName = GlobalData.MasterPlotsSource[0].MasterPlotName;
    }

    #endregion
}