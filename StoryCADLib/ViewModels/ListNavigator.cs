using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace StoryCADLib.ViewModels;

/// <summary>
/// Generic list navigation helper for StoryWorld entry lists.
/// Manages index tracking, Previous/Next/Add/Remove commands,
/// and PropertyChanged subscription forwarding for change detection.
/// </summary>
public class ListNavigator<T> : ObservableObject where T : ObservableObject
{
    private readonly ObservableCollection<T> _items;
    private readonly Func<T> _factory;
    private readonly Action _onChanged;
    private INotifyPropertyChanged _subscribedEntry;
    private int _currentIndex;

    public ListNavigator(ObservableCollection<T> items, Func<T> factory, Action onChanged)
    {
        _items = items;
        _factory = factory;
        _onChanged = onChanged;

        PreviousCommand = new RelayCommand(() => { if (HasPrevious) CurrentIndex--; });
        NextCommand = new RelayCommand(() => { if (HasNext) CurrentIndex++; });
        AddCommand = new RelayCommand(Add);
        RemoveCurrentCommand = new RelayCommand(RemoveCurrent);
    }

    public int CurrentIndex
    {
        get => _currentIndex;
        set { if (SetProperty(ref _currentIndex, value)) NotifyNavigationChanged(); }
    }

    public bool HasItems => _items?.Count > 0;
    public bool HasPrevious => CurrentIndex > 0;
    public bool HasNext => _items != null && CurrentIndex < _items.Count - 1;
    public string PositionDisplay =>
        _items == null || _items.Count == 0
            ? "0 of 0"
            : $"{CurrentIndex + 1} of {_items.Count}";
    public T CurrentItem =>
        HasItems && CurrentIndex < _items.Count
            ? _items[CurrentIndex] : default;

    public RelayCommand PreviousCommand { get; }
    public RelayCommand NextCommand { get; }
    public RelayCommand AddCommand { get; }
    public RelayCommand RemoveCurrentCommand { get; }

    public void Add()
    {
        _items.Add(_factory());
        CurrentIndex = _items.Count - 1;
        NotifyNavigationChanged();
        _onChanged();
    }

    public void RemoveCurrent()
    {
        if (HasItems && CurrentIndex < _items.Count)
        {
            _items.RemoveAt(CurrentIndex);
            if (CurrentIndex >= _items.Count && _items.Count > 0)
                CurrentIndex = _items.Count - 1;
            NotifyNavigationChanged();
            _onChanged();
        }
    }

    public void Remove(T entry)
    {
        if (entry != null && _items.Contains(entry))
        {
            _items.Remove(entry);
            if (CurrentIndex >= _items.Count && _items.Count > 0)
                CurrentIndex = _items.Count - 1;
            NotifyNavigationChanged();
            _onChanged();
        }
    }

    /// <summary>
    /// Resets the navigator to the first entry and notifies all properties.
    /// Called during LoadModel to sync after collection replacement.
    /// </summary>
    public void Reset()
    {
        _currentIndex = 0;
        NotifyNavigationChanged();
    }

    private void NotifyNavigationChanged()
    {
        // Manage entry PropertyChanged subscription
        if (_subscribedEntry != null)
            _subscribedEntry.PropertyChanged -= OnEntryPropertyChanged;
        _subscribedEntry = CurrentItem;
        if (_subscribedEntry != null)
            _subscribedEntry.PropertyChanged += OnEntryPropertyChanged;

        OnPropertyChanged(nameof(HasItems));
        OnPropertyChanged(nameof(HasPrevious));
        OnPropertyChanged(nameof(HasNext));
        OnPropertyChanged(nameof(PositionDisplay));
        OnPropertyChanged(nameof(CurrentItem));
    }

    private void OnEntryPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        _onChanged();
    }
}
