using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCADLib.Models;
using StoryCADLib.Services;
using StoryCADLib.ViewModels;
using StoryCADLib.ViewModels.Tools;

namespace StoryCADTests.ViewModels;

/// <summary>
///     Covers the pure image-layout math used by the PDF report's Images
///     appendix. The drawing itself needs SkiaSharp + a real document and is
///     verified manually; the scaling logic is isolated here so it can be tested
///     headlessly.
/// </summary>
[TestClass]
public class PrintReportDialogVMTests
{
    private const float Tolerance = 0.01f;

    private static (PrintReportDialogVM Vm, StoryModel Model) NewVm()
    {
        var model = new StoryModel();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        appState.CurrentDocument = new StoryDocument(model);
        return (Ioc.Default.GetRequiredService<PrintReportDialogVM>(), model);
    }

    [TestMethod]
    public void ScaleToFit_WideImage_ConstrainedByWidth_PreservesAspect()
    {
        // 2048x1536 into a 432x700 box: width is the binding constraint.
        var (width, height) = PrintReportDialogVM.ScaleToFit(2048, 1536, 432f, 700f);

        Assert.AreEqual(432f, width, Tolerance, "should fill the available width");
        Assert.AreEqual(1536f / 2048f, height / width, Tolerance, "aspect ratio preserved");
        Assert.IsTrue(height <= 700f + Tolerance, "stays within the height bound");
    }

    [TestMethod]
    public void ScaleToFit_TallImage_ConstrainedByHeight_PreservesAspect()
    {
        // 1000x3000 into a 432x700 box: height is the binding constraint.
        var (width, height) = PrintReportDialogVM.ScaleToFit(1000, 3000, 432f, 700f);

        Assert.AreEqual(700f, height, Tolerance, "should fill the available height");
        Assert.AreEqual(3000f / 1000f, height / width, Tolerance, "aspect ratio preserved");
        Assert.IsTrue(width <= 432f + Tolerance, "stays within the width bound");
    }

    [TestMethod]
    public void ScaleToFit_SmallImage_IsNotUpscaled()
    {
        // Smaller than the box: kept at natural size rather than blown up.
        var (width, height) = PrintReportDialogVM.ScaleToFit(200, 150, 432f, 700f);

        Assert.AreEqual(200f, width, Tolerance);
        Assert.AreEqual(150f, height, Tolerance);
    }

    [TestMethod]
    public void ScaleToFit_InvalidDimensions_ReturnsZero()
    {
        Assert.AreEqual((0f, 0f), PrintReportDialogVM.ScaleToFit(0, 100, 432f, 700f));
        Assert.AreEqual((0f, 0f), PrintReportDialogVM.ScaleToFit(100, 0, 432f, 700f));
        Assert.AreEqual((0f, 0f), PrintReportDialogVM.ScaleToFit(-5, -5, 432f, 700f));
    }

    [TestMethod]
    public void GatherAppendixImages_NotesElementWithImages_IsIncluded()
    {
        var (vm, model) = NewVm();
        var notes = new FolderModel("Test Notes", model, StoryItemType.Notes, null);
        notes.Images.Add(new StoryImage(
            Guid.NewGuid(), Convert.ToBase64String(new byte[] { 1 }), "image/png", "x.png"));

        vm.SelectedNodes = new List<StoryNodeItem> { notes.Node };

        var sections = vm.GatherAppendixImages();

        Assert.AreEqual(1, sections.Count);
        Assert.AreEqual(1, sections[0].Images.Count);
    }

    [TestMethod]
    public void GatherAppendixImages_NodeWithEmptyGuid_IsSkippedNotThrown()
    {
        var (vm, model) = NewVm();
        var character = new CharacterModel("Test Character", model, null);
        character.Images.Add(new StoryImage(
            Guid.NewGuid(), Convert.ToBase64String(new byte[] { 1 }), "image/png", "x.png"));

        var badNode = new StoryNodeItem(character, null) { Uuid = Guid.Empty };

        vm.SelectedNodes = new List<StoryNodeItem> { badNode, character.Node };

        var sections = vm.GatherAppendixImages();

        Assert.AreEqual(1, sections.Count, "the bad node should be skipped, not abort the whole gather");
    }
}
