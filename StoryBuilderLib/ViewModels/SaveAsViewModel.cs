﻿using CommunityToolkit.Mvvm.ComponentModel;
using Windows.Storage;

namespace StoryBuilder.ViewModels
{
    public class SaveAsViewModel : ObservableRecipient
    {
        #region Fields
        #endregion

        #region Properties

        private string _projectName;
        public string ProjectName
        {
            get => _projectName;
            set => SetProperty(ref _projectName, value);
        }

        private string _projectPathName;
        public string ProjectPathName
        {
            get => _projectPathName;
            set => SetProperty(ref _projectPathName, value);
        }

        private bool _saveAsProjectFolderExists;
        public bool SaveAsProjectFolderExists 
        {
            get => _saveAsProjectFolderExists;
            set => _saveAsProjectFolderExists = value;
        }
        
        private StorageFolder _saveAsParentFolder;
        public StorageFolder SaveAsParentFolder 
        {
            get => _saveAsParentFolder;
            set => _saveAsParentFolder = value; 
        }

        private StorageFolder _saveAsProjectFolder;
        public StorageFolder SaveAsProjectFolder 
        {
            get => _saveAsProjectFolder;
            set => _saveAsProjectFolder = value;
        }

        private string _saveAsProjectFolderPath;
        public string SaveAsProjectFolderPath 
        {
            get => _saveAsProjectFolderPath;
            set => _saveAsProjectFolderPath = value;
        }

        #endregion

        #region Public Methods
        #endregion

        #region Constructor
        public SaveAsViewModel()
        {
        }
        #endregion
    }
}
