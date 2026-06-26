using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using StoryCADLib.Services;

namespace StoryCADLib.ViewModels;

/// <summary>
///     Reusable image-gallery backing used by the Character, Setting, and Scene
///     ViewModels. Owns the observable collection bound to
///     <see cref="StoryCADLib.Controls.ImageGalleryControl"/>, decodes thumbnails
///     for the tiles (full-resolution source stays on the model), and raises the
///     supplied <c>onChanged</c> callback when images or captions change so the
///     owning ViewModel can mark the element dirty.
/// </summary>
public class ElementImageGallery
{
    /// <summary>Width (pixels) used to decode gallery thumbnails.</summary>
    private const int ThumbnailWidth = 400;

    private readonly ImageService _imageService;
    private readonly ILogService _logger;
    private readonly Action _onChanged;

    /// <summary>Tiles bound by the gallery control's ItemsSource.</summary>
    public ObservableCollection<ImageGalleryItem> Items { get; } = new();

    public ElementImageGallery(ImageService imageService, ILogService logger, Action onChanged)
    {
        _imageService = imageService;
        _logger = logger;
        _onChanged = onChanged;
        Items.CollectionChanged += OnCollectionChanged;
    }

    /// <summary>Replaces the gallery contents from a model's image list (null-safe).</summary>
    public void Load(List<StoryImage> images)
    {
        Items.Clear();
        if (images != null)
        {
            foreach (StoryImage image in images)
            {
                Items.Add(new ImageGalleryItem(image, null));
            }
        }

        _ = LoadThumbnailsAsync();
    }

    /// <summary>Snapshots the current tiles back to a model image list.</summary>
    public List<StoryImage> ToModelList() => Items.Select(i => i.Model).ToList();

    private async Task LoadThumbnailsAsync()
    {
        foreach (ImageGalleryItem item in Items.ToList())
        {
            try
            {
                item.Source = await _imageService.ToImageSourceAsync(item.Model, ThumbnailWidth);
            }
            catch (Exception ex)
            {
                _logger.LogException(LogLevel.Error, ex, "Failed to decode element image");
            }
        }
    }

    private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (ImageGalleryItem item in e.NewItems)
            {
                item.PropertyChanged += OnItemChanged;
            }
        }

        if (e.OldItems != null)
        {
            foreach (ImageGalleryItem item in e.OldItems)
            {
                item.PropertyChanged -= OnItemChanged;
            }
        }

        _onChanged?.Invoke();
    }

    private void OnItemChanged(object sender, PropertyChangedEventArgs e) => _onChanged?.Invoke();
}
