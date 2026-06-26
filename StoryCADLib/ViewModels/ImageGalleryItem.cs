using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml.Media;

namespace StoryCADLib.ViewModels;

/// <summary>
///     Display wrapper around a <see cref="StoryImage"/> for the image gallery.
///     Not serialized — it pairs the persisted model with a decoded
///     <see cref="ImageSource"/> for the tile, and proxies the caption so edits
///     flow straight back to the model.
/// </summary>
[Microsoft.UI.Xaml.Data.Bindable]
public class ImageGalleryItem : INotifyPropertyChanged
{
    /// <summary>The persisted image this tile represents.</summary>
    public StoryImage Model { get; }

    private ImageSource _source;

    /// <summary>Decoded image used by the tile (typically a thumbnail).</summary>
    public ImageSource Source
    {
        get => _source;
        set => Set(ref _source, value);
    }

    /// <summary>User caption; writes straight through to the model.</summary>
    public string Caption
    {
        get => Model.Caption;
        set
        {
            if (Model.Caption != value)
            {
                Model.Caption = value;
                OnPropertyChanged();
            }
        }
    }

    public ImageGalleryItem(StoryImage model, ImageSource source)
    {
        Model = model;
        _source = source;
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private void Set<T>(ref T field, T value, [CallerMemberName] string name = null)
    {
        if (!Equals(field, value))
        {
            field = value;
            OnPropertyChanged(name);
        }
    }
}
