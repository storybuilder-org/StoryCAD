using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

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
            set => _selection = value;
        }

        public StringSelection(string stringName, bool selected = false)
        {
            StringName = stringName;
            Selection = selected;
        }
    }
}
