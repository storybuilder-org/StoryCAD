using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCAD.Models;
using StoryCAD.Models.Tools;

namespace StoryCAD.ViewModels.Tools;

public class DramaticSituationsViewModel : ObservableRecipient
{
    #region Fields

    private bool _changed;
    #endregion

    #region Properties
    public bool Changed
    {
        get => _changed;
        // ReSharper disable once ValueParameterNotUsed
        set => _changed = false;
    }

    private DramaticSituationModel _situation;

    public DramaticSituationModel Situation
    {
        get => _situation;
        set => SetProperty(ref _situation, value);
    }

    private string _situationName;
    public string SituationName
    {
        get => _situationName;
        set
        {
            SetProperty(ref _situationName, value);
            Situation = _situations[value];
        }
    }
    private string _notes;
    public string Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value);
    }

    #endregion

    #region Combobox and ListBox sources

    public ObservableCollection<string> SituationsSource;

    private SortedDictionary<string, DramaticSituationModel> _situations;

    #endregion

    #region Constructor

    public DramaticSituationsViewModel()
    {
        _situations = Ioc.Default.GetService<ToolsData>().DramaticSituationsSource;

        SituationsSource = new ObservableCollection<string>();
        foreach (string _situationKey in _situations.Keys) {SituationsSource.Add(_situationKey);}
    }
    #endregion
}