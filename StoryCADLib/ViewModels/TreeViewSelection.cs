using System.ComponentModel;

namespace StoryCAD.ViewModels;

public partial class TreeViewSelection : DependencyObject, INotifyPropertyChanged
{
    // Use a DependencyProperty as the backing store for SelectedItem
    public static readonly DependencyProperty SelectedItemProperty =
        DependencyProperty.Register(
            nameof(SelectedItem),
            typeof(object),
            typeof(TreeViewSelection),
            new PropertyMetadata(null)
        );

    public object SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set
        {
            SetValue(SelectedItemProperty, value);
            NotifyPropertyChanged("SelectedItem");
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private void NotifyPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
