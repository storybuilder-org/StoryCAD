using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCADLib.Models;
using StoryCADLib.Services;
using StoryCADLib.Services.Logging;
using StoryCADLib.ViewModels;

namespace StoryCADTests.ViewModels;

/// <summary>
///     Covers the subscription bookkeeping in <see cref="ElementImageGallery"/>: a stale
///     <see cref="ImageGalleryItem"/> left over from a prior <see cref="ElementImageGallery.Load"/>
///     must not still be able to trigger the shared onChanged callback.
/// </summary>
[TestClass]
public class ElementImageGalleryTests
{
    private static StoryImage NewImage(byte value) =>
        new(Guid.NewGuid(), Convert.ToBase64String(new[] { value }), "image/png", "x.png");

    [TestMethod]
    public void Load_ReplacingItems_UnsubscribesStaleItemFromOnChanged()
    {
        var imageService = Ioc.Default.GetRequiredService<ImageService>();
        var logger = Ioc.Default.GetRequiredService<ILogService>();
        int changedCount = 0;
        var gallery = new ElementImageGallery(imageService, logger, () => changedCount++);

        gallery.Load(new List<StoryImage> { NewImage(1) });
        ImageGalleryItem staleItem = gallery.Items[0];

        gallery.Load(new List<StoryImage> { NewImage(2) });
        changedCount = 0;

        // Simulates a thumbnail decode for the *previous* element completing after the
        // gallery has already moved on: this item is no longer in Items.
        staleItem.Caption = "late update";

        Assert.AreEqual(0, changedCount,
            "a stale item from a prior Load() must not still be able to mark the gallery changed");
    }

    [TestMethod]
    public void Load_ReplacingItems_CurrentItemStillTriggersOnChanged()
    {
        var imageService = Ioc.Default.GetRequiredService<ImageService>();
        var logger = Ioc.Default.GetRequiredService<ILogService>();
        int changedCount = 0;
        var gallery = new ElementImageGallery(imageService, logger, () => changedCount++);

        gallery.Load(new List<StoryImage> { NewImage(1) });
        gallery.Load(new List<StoryImage> { NewImage(2) });
        changedCount = 0;

        gallery.Items[0].Caption = "current element edit";

        Assert.AreEqual(1, changedCount,
            "the currently-loaded item's PropertyChanged must still reach onChanged");
    }
}
