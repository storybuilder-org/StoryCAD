using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using StoryBuilder.Controllers;
using StoryBuilder.DAL;

namespace StoryBuilder.ViewModels.Tools
{
    public class StockScenesViewModel : ObservableRecipient
    {
        private readonly StoryController story;

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

        public StockScenesViewModel()
        {
            StockSceneCategories = new ObservableCollection<string>();
            _stockScenes = StaticData.StockScenesSource;
            foreach (string category in _stockScenes.Keys)
                StockSceneCategories.Add(category);
        }

        #endregion
    }
}
