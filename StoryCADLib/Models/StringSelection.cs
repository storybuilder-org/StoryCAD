using CommunityToolkit.Mvvm.ComponentModel;

namespace StoryCAD.Models
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
                OnPropertyChanged();
            }
        }

        public StringSelection(string stringName, bool selected = false)
        {
            _stringName = stringName;
            _selection = selected;
        }
    }
}