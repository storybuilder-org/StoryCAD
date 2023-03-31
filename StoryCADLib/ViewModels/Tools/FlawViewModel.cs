using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using StoryCAD.Models;

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
    public FlawViewModel()
    {
        Dictionary<string, ObservableCollection<string>> _lists = GlobalData.ListControlSource;

        WoundCategoryList = _lists["WoundCategory"];
        WoundSummaryList = _lists["Wound"];
    }

    #endregion
}