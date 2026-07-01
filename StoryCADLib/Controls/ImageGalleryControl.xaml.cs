using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using StoryCADLib.Services;
using StoryCADLib.ViewModels;

namespace StoryCADLib.Controls;

/// <summary>
///     Reusable gallery for the pictures attached to a story element. Binds to an
///     <see cref="ObservableCollection{ImageGalleryItem}"/> owned by the element's
///     ViewModel; "Add Picture" imports a full-resolution image and each tile can
///     be captioned or removed. Self-contained like <see cref="BrowseTextBox"/>:
///     it picks/decodes via <see cref="ImageService"/> from the IoC container and
///     mutates the bound collection directly, so the ViewModel's
///     CollectionChanged subscription drives change tracking.
/// </summary>
public sealed partial class ImageGalleryControl : UserControl
{
    /// <summary>
    ///     Width (pixels) used to decode gallery thumbnails. The full-resolution
    ///     source remains stored on the model.
    /// </summary>
    private const int ThumbnailDecodeWidth = 400;

    public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
        nameof(ItemsSource), typeof(ObservableCollection<ImageGalleryItem>), typeof(ImageGalleryControl),
        new PropertyMetadata(null));

    public ImageGalleryControl()
    {
        InitializeComponent();
    }

    public ObservableCollection<ImageGalleryItem> ItemsSource
    {
        get => (ObservableCollection<ImageGalleryItem>)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    private async void OnAddClick(object sender, RoutedEventArgs e)
    {
        try
        {
            if (ItemsSource == null)
            {
                return;
            }

            var imageService = Ioc.Default.GetRequiredService<ImageService>();
            var (image, thumbnail) = await imageService.PickImageWithThumbnailAsync(ThumbnailDecodeWidth);
            if (image == null)
            {
                return; // user cancelled
            }

            // The picture is always persisted here even if thumbnail decoding above
            // failed (thumbnail is then null and the tile just shows without a preview).
            ItemsSource.Add(new ImageGalleryItem(image, thumbnail));
        }
        catch (Exception ex)
        {
            Ioc.Default.GetRequiredService<ILogService>()
                .LogException(LogLevel.Error, ex, "ImageGalleryControl add error");
        }
    }

    private void OnRemoveClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: ImageGalleryItem item })
        {
            ItemsSource?.Remove(item);
        }
    }

    // Clicking the thumbnail and the Enlarge button both open the same dialog.
    private async void OnImageClick(object sender, RoutedEventArgs e) => await ShowEnlargedAsync(sender);

    private async void OnEnlargeClick(object sender, RoutedEventArgs e) => await ShowEnlargedAsync(sender);

    /// <summary>
    ///     Shows the picture in a ContentDialog (decoded at full stored resolution,
    ///     ≤ the import cap, for a crisp large view) with a "Close" button so
    ///     dismissal is obvious. Falls back to the thumbnail if the full decode
    ///     fails. Reuses <see cref="Windowing.ShowContentDialog"/> for XamlRoot,
    ///     theming, and the one-dialog-at-a-time rule.
    /// </summary>
    private async Task ShowEnlargedAsync(object sender)
    {
        try
        {
            if (sender is not FrameworkElement { DataContext: ImageGalleryItem item })
            {
                return;
            }

            var imageService = Ioc.Default.GetRequiredService<ImageService>();
            ImageSource source = await imageService.ToImageSourceAsync(item.Model) ?? item.Source;
            if (source == null)
            {
                return; // nothing decoded to show
            }

            var panel = new StackPanel { Spacing = 12 };
            panel.Children.Add(new Image
            {
                Source = source,
                Stretch = Stretch.Uniform,
                MaxWidth = 1000,
                MaxHeight = 760
            });

            if (!string.IsNullOrEmpty(item.Model?.Caption))
            {
                panel.Children.Add(new TextBlock
                {
                    Text = item.Model.Caption,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextWrapping = TextWrapping.Wrap
                });
            }

            var dialog = new ContentDialog
            {
                Title = string.IsNullOrEmpty(item.Model?.FileName) ? "Image" : item.Model.FileName,
                Content = panel,
                CloseButtonText = "Close"
            };

            await Ioc.Default.GetRequiredService<Windowing>().ShowContentDialog(dialog);
        }
        catch (Exception ex)
        {
            Ioc.Default.GetRequiredService<ILogService>()
                .LogException(LogLevel.Error, ex, "ImageGalleryControl enlarge error");
        }
    }
}
