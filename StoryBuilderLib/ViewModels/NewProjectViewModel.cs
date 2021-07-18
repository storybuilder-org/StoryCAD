using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using StoryBuilder.Controllers;
using StoryBuilder.DAL;
using StoryBuilder.Models.Tools;

namespace StoryBuilder.ViewModels
{
    public class NewProjectViewModel: ObservableRecipient
    {
        private readonly StoryController story;


        private string _selectedTemplate;

        public string SelectedTemplate 
        {
            get => _selectedTemplate;
            set 
            {   
                SetProperty(ref _selectedTemplate, value);
            }
        }
        private string _projectName;
        public string ProjectName
        {
            get => _projectName;
            set
            {
                SetProperty(ref _projectName, value);
            }
        }

        private string _parentPathName;
        public string ParentPathName
        {
            get => _parentPathName;
            set
            {
                SetProperty(ref _parentPathName, value);
            }
        }

        public NewProjectViewModel()
        {
            ProjectName = string.Empty;
            PreferencesModel prefs = GlobalData.Preferences;
            ParentPathName = prefs.ProjectDirectory;
        }
    }
}
