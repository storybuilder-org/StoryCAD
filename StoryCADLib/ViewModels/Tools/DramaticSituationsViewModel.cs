using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
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

    private readonly SortedDictionary<string, DramaticSituationModel> _situations;

    #endregion

    #region Constructor

    private readonly ToolsData _toolsData;

    // Constructor for XAML compatibility - will be removed later
    public DramaticSituationsViewModel() : this(Ioc.Default.GetRequiredService<ToolsData>())
    {
    }

    public DramaticSituationsViewModel(ToolsData toolsData)
    {
        _toolsData = toolsData;
        _situations = _toolsData.DramaticSituationsSource;
        SituationsSource = new ObservableCollection<string>(_situations.Keys);
    }
    #endregion
}