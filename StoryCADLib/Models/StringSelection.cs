using CommunityToolkit.Mvvm.ComponentModel;

namespace StoryCAD.Models;

public class StringSelection : ObservableObject
{
    private bool _selection;
    private string _stringName;

    public StringSelection(string stringName, bool selected = false)
    {
        _stringName = stringName;
        _selection = selected;
    }

    public string StringName
    {
        get => _stringName;
        set => _stringName = value;
    }

    public bool Selection
    {
        get => _selection;
        set
        {
            // ReSharper disable once RedundantCheckBeforeAssignment
            if (_selection == value)
            {
                return;
            }

            _selection = value;
            OnPropertyChanged();
        }
    }
}
