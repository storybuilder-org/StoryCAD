using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace StoryCADLib.ViewModels.Tools;

public class FlawViewModel : ObservableRecipient
{
    #region Properties

    private string _woundCategory;

    public string WoundCategory
    {
        get => _woundCategory;
        set => SetProperty(ref _woundCategory, value);
    }

    private string _woundSummary;

    public string WoundSummary
    {
        get => _woundSummary;
        set => SetProperty(ref _woundSummary, value);
    }

    #endregion

    #region ComboBox sources

    public ObservableCollection<string> WoundCategoryList;
    public ObservableCollection<string> WoundSummaryList;

    #endregion

    #region Constructor

    private readonly ListData _listData;

    public FlawViewModel(ListData listData)
    {
        _listData = listData;
        var _lists = _listData.ListControlSource;

        WoundCategoryList = _lists["WoundCategory"];
        WoundSummaryList = _lists["Wound"];
    }

    #endregion
}
