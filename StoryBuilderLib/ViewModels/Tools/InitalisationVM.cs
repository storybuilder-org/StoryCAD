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

        private string _errorlogging;
        public string ErrorLogging
        {
            get => _errorlogging;
            set { SetProperty(ref _errorlogging, value); }
        }

        private string _news;
        public string News
        {
            get => _news;
            set { SetProperty(ref _news, value); }
        }

        public void Check()
        {
            //if (Path != String.IsNullOrWhiteSpace )
        }

        public InitialisationVM()
        {

        }
    }
}
