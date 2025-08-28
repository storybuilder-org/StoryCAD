using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using StoryCAD.Models.Tools;

namespace StoryCAD.ViewModels.Tools;

public class StockScenesViewModel : ObservableRecipient
{
    #region Properties

    public ObservableCollection<string> StockSceneCategories;

    private string _categoryName;
    public string CategoryName
    {
        get => _categoryName;
        set
        {
            SetProperty(ref _categoryName, value);
            StockSceneList = _stockScenes[value];
        }
    }

    private ObservableCollection<string> _stockSceneList;
    public ObservableCollection<string> StockSceneList
    {
        get => _stockSceneList;
        set => SetProperty(ref _stockSceneList, value);
    }

    private string _sceneName;
    public string SceneName
    {
        get => _sceneName;
        set => SetProperty(ref _sceneName, value);
    }

    #endregion

    #region Combobox and ListBox sources

    private readonly SortedDictionary<string, ObservableCollection<string>> _stockScenes;

    #endregion

    #region Constructor

    private readonly ToolsData _toolsData;

    // Constructor for XAML compatibility - will be removed later
    public StockScenesViewModel() : this(Ioc.Default.GetRequiredService<ToolsData>())
    {
    }

    public StockScenesViewModel(ToolsData toolsData)
    {
        _toolsData = toolsData;
        StockSceneCategories = new ObservableCollection<string>();
        _stockScenes = _toolsData.StockScenesSource;
        foreach (string _category in _stockScenes.Keys) { StockSceneCategories.Add(_category); }
    }

    #endregion
}