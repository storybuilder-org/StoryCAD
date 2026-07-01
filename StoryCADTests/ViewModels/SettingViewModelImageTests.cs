using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCADLib.Models;
using StoryCADLib.ViewModels;

namespace StoryCADTests.ViewModels;

/// <summary>
///     Covers the gallery wiring on the element ViewModel using Setting (the
///     simplest LoadModel/SaveModel of the three image-bearing elements).
///     Thumbnail decoding needs the UI thread, so these assert on the persisted
///     model/caption data rather than the decoded image source.
/// </summary>
[TestClass]
public class SettingViewModelImageTests
{
    private static SettingModel NewSetting(out StoryModel model)
    {
        model = new StoryModel();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        appState.CurrentDocument = new StoryDocument(model);
        return new SettingModel(model, null);
    }

    [TestMethod]
    public void LoadModel_PopulatesGalleryFromModel()
    {
        var vm = Ioc.Default.GetRequiredService<SettingViewModel>();
        SettingModel setting = NewSetting(out _);
        setting.Images.Add(new StoryImage(
            Guid.NewGuid(), Convert.ToBase64String(new byte[] { 9, 8, 7 }), "image/png", "x.png")
        {
            Caption = "cap"
        });

        vm.Activate(setting);

        Assert.AreEqual(1, vm.Images.Count);
        Assert.AreEqual("cap", vm.Images[0].Caption);
        Assert.AreEqual(setting.Images[0].ImageData, vm.Images[0].Model.ImageData);
    }

    [TestMethod]
    public void SaveModel_PersistsAddedImageAndCaptionBackToModel()
    {
        var vm = Ioc.Default.GetRequiredService<SettingViewModel>();
        SettingModel setting = NewSetting(out _);
        vm.Activate(setting);

        // Simulate the gallery control importing an image, then captioning it.
        var added = new StoryImage(
            Guid.NewGuid(), Convert.ToBase64String(new byte[] { 1, 1, 1 }), "image/jpeg", "a.jpg");
        vm.Images.Add(new ImageGalleryItem(added, null));
        vm.Images[^1].Caption = "new caption";

        vm.SaveModel();

        Assert.AreEqual(1, setting.Images.Count);
        Assert.AreEqual(added.ImageData, setting.Images[0].ImageData);
        Assert.AreEqual("new caption", setting.Images[0].Caption);
    }

    [TestMethod]
    public void SaveModel_PersistsImageRemoval()
    {
        var vm = Ioc.Default.GetRequiredService<SettingViewModel>();
        SettingModel setting = NewSetting(out _);
        setting.Images.Add(new StoryImage(
            Guid.NewGuid(), Convert.ToBase64String(new byte[] { 5 }), "image/png", "x.png"));
        vm.Activate(setting);
        Assert.AreEqual(1, vm.Images.Count);

        vm.Images.Clear();
        vm.SaveModel();

        Assert.AreEqual(0, setting.Images.Count);
    }

    /// <summary>
    ///     The gallery's onChanged callback routes through the VM's own PropertyChanged
    ///     pipeline (raising "Images") rather than a bespoke OnImagesChanged method, so
    ///     it reaches the same dirty-tracking every other property already uses.
    /// </summary>
    [TestMethod]
    public void ImageGalleryChange_RaisesPropertyChangedForImages()
    {
        var vm = Ioc.Default.GetRequiredService<SettingViewModel>();
        SettingModel setting = NewSetting(out _);
        vm.Activate(setting);

        var raised = new List<string>();
        vm.PropertyChanged += (_, e) => raised.Add(e.PropertyName ?? string.Empty);

        var added = new StoryImage(
            Guid.NewGuid(), Convert.ToBase64String(new byte[] { 1 }), "image/png", "y.png");
        vm.Images.Add(new ImageGalleryItem(added, null));

        Assert.IsTrue(raised.Contains(nameof(vm.Images)),
            "adding an image should raise PropertyChanged(Images)");
    }
}
