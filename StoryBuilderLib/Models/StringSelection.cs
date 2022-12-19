using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using StoryBuilder.Services.Messages;

namespace StoryBuilder.Models
{
    public class StringSelection : ObservableObject
    {
        private string _stringName;
        public string StringName
        {
            get => _stringName;
            set => _stringName = value;
        }
        
        private bool _selection;
        public bool Selection
        {
            get => _selection;
            set
            {
                // ReSharper disable once RedundantCheckBeforeAssignment
                if (_selection == value)
                    return;
                _selection = value;
                OnPropertyChanged("Selection");
            }
        }

        public StringSelection(string stringName, bool selected = false)
        {
            _stringName = stringName;
            _selection = selected;
        }
    }
}
