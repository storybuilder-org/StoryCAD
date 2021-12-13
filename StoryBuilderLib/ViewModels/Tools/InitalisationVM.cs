using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using StoryBuilder.DAL;
using StoryBuilder.Models;
using StoryBuilder.Models.Tools;
using StoryBuilder.Services.Dialogs;
using Windows.Storage;

namespace StoryBuilder.ViewModels
{
    public class InitialisationVM : ObservableRecipient
    {
        private string _name;
        public string Name
        {
            get => _name;
            set { SetProperty(ref _name, value); }
        }

        private string _email;
        public string Email
        {
            get => _email;
            set { SetProperty(ref _email, value); }
        }

        private string _path;
        public string Path
        {
            get => _path;
            set { SetProperty(ref _path, value); }
        }

        private bool _errorlogging;
        public bool ErrorLogging
        {
            get => _errorlogging;
            set { SetProperty(ref _errorlogging, value); }
        }

        private bool _news;
        public bool News
        {
            get => _news;
            set { SetProperty(ref _news, value); }
        }

        public async void Save()
        {
            PreferencesModel prf = new();
            prf.Email = Email;
            prf.ErrorCollectionConsent = ErrorLogging;
            prf.Newsletter = News;
            prf.ProjectDirectory = Path;
            prf.Name = Name;
            PreferencesIO prfIO = new(prf, System.IO.Path.Combine(ApplicationData.Current.RoamingFolder.Path,"Storybuilder"));
            await prfIO.UpdateFile();
            PreferencesIO loader = new(GlobalData.Preferences, System.IO.Path.Combine(ApplicationData.Current.RoamingFolder.Path, "Storybuilder"));
            await loader.UpdateModel();
        }

        public InitialisationVM()
        {

        }
    }
}
