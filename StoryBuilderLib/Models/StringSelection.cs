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
        private readonly string _value;
        public string Value => _value;

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set => _isSelected = value;
        }

        public StringSelection(string value, bool selected = false)
        {
            _value = value;
            _isSelected = selected;
        }
    }
}
