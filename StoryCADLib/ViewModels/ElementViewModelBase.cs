using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using StoryCADLib.Services;

namespace StoryCADLib.ViewModels;

/// <summary>
///     Shared base for the Character/Scene/Setting/Folder element ViewModels,
///     each of which has an Images tab backed by the same ElementImageGallery:
///     decoding thumbnails and routing gallery changes into this VM's own
///     PropertyChanged(Images) so existing dirty-tracking picks them up.
/// </summary>
public abstract class ElementViewModelBase : ObservableRecipient
{
    /// <summary>Backing for the Images tab gallery.</summary>
    public ElementImageGallery ImageGallery { get; }

    /// <summary>Tiles bound by the Images tab's gallery control.</summary>
    public ObservableCollection<ImageGalleryItem> Images => ImageGallery.Items;

    protected ElementViewModelBase(ImageService imageService, ILogService logger)
    {
        ImageGallery = new ElementImageGallery(imageService, logger, () => OnPropertyChanged(nameof(Images)));
    }
}
