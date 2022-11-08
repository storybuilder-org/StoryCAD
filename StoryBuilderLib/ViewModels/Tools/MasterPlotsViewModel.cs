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
            if (MasterPlots.ContainsKey(value))
                MasterPlotNotes = MasterPlots[value].MasterPlotNotes;
        }
    }

    private string _masterPlotNotes;
    public string MasterPlotNotes
    {
        get => _masterPlotNotes;
        set => SetProperty(ref _masterPlotNotes, value);
    }

    private IList<MasterPlotScene> _masterPlotScenes;
    public IList<MasterPlotScene> MasterPlotScenes
    {
        get => _masterPlotScenes;
        set => SetProperty(ref _masterPlotScenes, value);
    }

    #endregion

    #region ComboBox and ListBox sources

    public readonly ObservableCollection<string> MasterPlotNames;

    public readonly Dictionary<string, MasterPlotModel> MasterPlots;

    #endregion

    #region Constructor

    public MasterPlotsViewModel()
    {
        List<string> MasterNames = new();
        MasterPlots = new Dictionary<string, MasterPlotModel>();
        foreach (MasterPlotModel plot in GlobalData.MasterPlotsSource)
        {
            MasterNames.Add(plot.MasterPlotName);
            MasterPlots.Add(plot.MasterPlotName, plot);
        }

        MasterNames.Sort();
        MasterPlotNames = new ObservableCollection<string>();
        foreach (string name in MasterNames) { MasterPlotNames.Add(name); }
        MasterPlotName = GlobalData.MasterPlotsSource[0].MasterPlotName;
    }

    #endregion
}