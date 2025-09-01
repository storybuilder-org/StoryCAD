using System.ComponentModel;
using Microsoft.UI.Xaml;

namespace StoryCAD.ViewModels;

public partial class TreeViewSelection : DependencyObject, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;
    public object SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set
        {
            SetValue(SelectedItemProperty, value);
            NotifyPropertyChanged("SelectedItem");
        }
    }

    // Use a DependencyProperty as the backing store for SelectedItem
    public static readonly DependencyProperty SelectedItemProperty =
        DependencyProperty.Register(
            nameof(SelectedItem),
            typeof(object),
            typeof(TreeViewSelection),
            new PropertyMetadata(null)
        );

    private void NotifyPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

}