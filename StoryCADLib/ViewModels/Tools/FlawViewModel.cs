using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace StoryCAD.ViewModels.Tools;

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

    // Constructor for XAML compatibility - will be removed later
    public FlawViewModel() : this(Ioc.Default.GetRequiredService<ListData>())
    {
    }

    public FlawViewModel(ListData listData)
    {
        _listData = listData;
        Dictionary<string, ObservableCollection<string>> _lists = _listData.ListControlSource;

        WoundCategoryList = _lists["WoundCategory"];
        WoundSummaryList = _lists["Wound"];
    }

    #endregion
}