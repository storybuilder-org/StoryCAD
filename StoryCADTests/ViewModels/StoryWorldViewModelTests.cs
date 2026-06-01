using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCADLib.Models;
using StoryCADLib.Models.StoryWorld;
using StoryCADLib.Services;
using StoryCADLib.Services.Navigation;
using StoryCADLib.ViewModels;

namespace StoryCADTests.ViewModels;

[TestClass]
public class StoryWorldViewModelTests
{
    [TestMethod]
    public void StoryWorldViewModel_ImplementsISaveable()
    {
        // Arrange
        var viewModel = Ioc.Default.GetRequiredService<StoryWorldViewModel>();

        // Act
        var saveable = viewModel as ISaveable;

        // Assert
        Assert.IsNotNull(saveable);
    }

    [TestMethod]
    public void StoryWorldViewModel_ImplementsIReloadable()
    {
        // Arrange
        var viewModel = Ioc.Default.GetRequiredService<StoryWorldViewModel>();

        // Act
        var reloadable = viewModel as IReloadable;

        // Assert
        Assert.IsNotNull(reloadable);
    }

    [TestMethod]
    public void StoryWorldViewModel_ImplementsINavigable()
    {
        // Arrange
        var viewModel = Ioc.Default.GetRequiredService<StoryWorldViewModel>();

        // Act
        var navigable = viewModel as INavigable;

        // Assert
        Assert.IsNotNull(navigable);
    }

    [TestMethod]
    public void SaveModel_ExistsAsPublicMethod()
    {
        // Arrange
        var viewModel = Ioc.Default.GetRequiredService<StoryWorldViewModel>();

        // Act
        var method = viewModel.GetType().GetMethod("SaveModel");

        // Assert
        Assert.IsNotNull(method);
        Assert.IsTrue(method.IsPublic);
    }

    [TestMethod]
    public void ReloadFromModel_ExistsAsPublicMethod()
    {
        // Arrange
        var viewModel = Ioc.Default.GetRequiredService<StoryWorldViewModel>();

        // Act
        var method = viewModel.GetType().GetMethod("ReloadFromModel");

        // Assert
        Assert.IsNotNull(method);
        Assert.IsTrue(method.IsPublic);
    }

    [TestMethod]
    public void Activate_LoadsModelData()
    {
        // Arrange
        var viewModel = Ioc.Default.GetRequiredService<StoryWorldViewModel>();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var storyModel = new StoryModel();
        appState.CurrentDocument = new StoryDocument(storyModel);

        // Add OverviewModel to ExplorerView so StoryWorld name can be derived
        var overview = new OverviewModel("Test", storyModel, null);
        storyModel.ExplorerView.Add(new StoryNodeItem(overview, null));

        var model = new StoryWorldModel("Ignored Name", storyModel, null);
        model.WorldType = "Hidden World";
        model.EconomicSystem = "Barter economy";

        // Act
        viewModel.Activate(model);

        // Assert - Name is derived from story name + " Story World", not from model
        Assert.AreEqual("Test Story World", viewModel.Name);
        Assert.AreEqual("Hidden World", viewModel.WorldType);
        Assert.AreEqual("Barter economy", viewModel.EconomicSystem);
    }

    [TestMethod]
    public void SaveModel_PersistsViewModelToModel()
    {
        // Arrange
        var viewModel = Ioc.Default.GetRequiredService<StoryWorldViewModel>();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var storyModel = new StoryModel();
        appState.CurrentDocument = new StoryDocument(storyModel);

        var model = new StoryWorldModel("Test World", storyModel, null);
        viewModel.Activate(model);

        // Act - modify ViewModel properties
        viewModel.WorldType = "Broken World";
        viewModel.Currency = "Bottle caps";
        viewModel.SaveModel();

        // Assert - model should have new values
        Assert.AreEqual("Broken World", model.WorldType);
        Assert.AreEqual("Bottle caps", model.Currency);
    }

    [TestMethod]
    public void WorldTypeList_ContainsEightGestalts()
    {
        // Arrange
        var viewModel = Ioc.Default.GetRequiredService<StoryWorldViewModel>();

        // Assert - should have 8 world types plus blank option
        Assert.IsNotNull(viewModel.WorldTypeList);
        Assert.IsTrue(viewModel.WorldTypeList.Count >= 8);
    }

    [TestMethod]
    public void WorldTypeList_LoadedFromListData()
    {
        // Arrange
        var viewModel = Ioc.Default.GetRequiredService<StoryWorldViewModel>();
        var listData = Ioc.Default.GetRequiredService<ListData>();

        // Assert - ViewModel list should match ListData
        Assert.IsNotNull(viewModel.WorldTypeList);
        Assert.AreSame(listData.ListControlSource["WorldType"], viewModel.WorldTypeList);
    }

    [TestMethod]
    public void OntologyList_LoadedFromListData()
    {
        // Arrange
        var viewModel = Ioc.Default.GetRequiredService<StoryWorldViewModel>();
        var listData = Ioc.Default.GetRequiredService<ListData>();

        // Assert
        Assert.IsNotNull(viewModel.OntologyList);
        Assert.AreSame(listData.ListControlSource["Ontology"], viewModel.OntologyList);
    }

    [TestMethod]
    public void SystemTypeList_LoadedFromListData()
    {
        // Arrange
        var viewModel = Ioc.Default.GetRequiredService<StoryWorldViewModel>();
        var listData = Ioc.Default.GetRequiredService<ListData>();

        // Assert
        Assert.IsNotNull(viewModel.SystemTypeList);
        Assert.AreSame(listData.ListControlSource["SystemType"], viewModel.SystemTypeList);
    }

    [TestMethod]
    public void HasContentToFontWeightConverter_ReturnsBold_WhenFieldHasContent()
    {
        // Arrange
        var converter = new StoryCADLib.Converters.HasContentToFontWeightConverter();

        // Act
        var result = (Windows.UI.Text.FontWeight)converter.Convert("Some content", typeof(Windows.UI.Text.FontWeight), null, null);

        // Assert - should be bold (weight 700)
        Assert.AreEqual((ushort)700, result.Weight);
    }

    [TestMethod]
    public void HasContentToFontWeightConverter_ReturnsNormal_WhenFieldIsEmpty()
    {
        // Arrange
        var converter = new StoryCADLib.Converters.HasContentToFontWeightConverter();

        // Act - empty string
        var result = (Windows.UI.Text.FontWeight)converter.Convert("", typeof(Windows.UI.Text.FontWeight), null, null);

        // Assert - should be normal (weight 400)
        Assert.AreEqual((ushort)400, result.Weight);
    }

    [TestMethod]
    public void HasContentToFontWeightConverter_ReturnsNormal_WhenFieldIsNull()
    {
        // Arrange
        var converter = new StoryCADLib.Converters.HasContentToFontWeightConverter();

        // Act - null
        var result = (Windows.UI.Text.FontWeight)converter.Convert(null, typeof(Windows.UI.Text.FontWeight), null, null);

        // Assert - should be normal (weight 400)
        Assert.AreEqual((ushort)400, result.Weight);
    }

    // ---------------------------------------------------------------------
    // ListNavigator<T> integration tests (issue #1313)
    // These drive ListNavigator behavior through StoryWorldViewModel's public
    // surface (PhysicalWorldNav + PhysicalWorlds collection). The VM is an IoC
    // singleton, so every test calls Activate(model) first to reset list state
    // deterministically via LoadModel -> Reset().
    // ---------------------------------------------------------------------

    /// <summary>
    /// Resolves the singleton VM, seeds a fresh story/document, and activates a
    /// clean StoryWorldModel so all navigators start empty with CurrentIndex 0.
    /// </summary>
    private static StoryWorldViewModel ArrangeActivatedViewModel(out StoryWorldModel model)
    {
        var viewModel = Ioc.Default.GetRequiredService<StoryWorldViewModel>();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var storyModel = new StoryModel();
        appState.CurrentDocument = new StoryDocument(storyModel);

        // Seed OverviewModel so LoadModel's name derivation does not NRE.
        var overview = new OverviewModel("Test", storyModel, null);
        storyModel.ExplorerView.Add(new StoryNodeItem(overview, null));

        model = new StoryWorldModel(storyModel, null);
        viewModel.Activate(model);
        return viewModel;
    }

    [TestMethod]
    public void Add_FirstEntry_SetsIndexToZero()
    {
        // Arrange
        var viewModel = ArrangeActivatedViewModel(out _);
        var nav = viewModel.PhysicalWorldNav;

        // Act
        nav.Add();

        // Assert
        Assert.AreEqual(0, nav.CurrentIndex);
        Assert.IsTrue(nav.HasItems);
        Assert.IsNotNull(nav.CurrentItem);
        Assert.AreEqual("1 of 1", nav.PositionDisplay);
    }

    [TestMethod]
    public void Add_AppendsAndSelectsNewEntry()
    {
        // Arrange - seed two existing entries
        var viewModel = ArrangeActivatedViewModel(out _);
        var nav = viewModel.PhysicalWorldNav;
        viewModel.PhysicalWorlds.Add(new PhysicalWorldEntry { Name = "Alpha" });
        viewModel.PhysicalWorlds.Add(new PhysicalWorldEntry { Name = "Beta" });

        // Act
        nav.Add();

        // Assert - new entry is appended and selected
        Assert.AreEqual(3, viewModel.PhysicalWorlds.Count);
        Assert.AreEqual(2, nav.CurrentIndex);
        Assert.AreSame(viewModel.PhysicalWorlds[2], nav.CurrentItem);
    }

    [TestMethod]
    public void Next_AtLastItem_DoesNotAdvance()
    {
        // Arrange - two entries, positioned at last
        var viewModel = ArrangeActivatedViewModel(out _);
        var nav = viewModel.PhysicalWorldNav;
        viewModel.PhysicalWorlds.Add(new PhysicalWorldEntry { Name = "Alpha" });
        viewModel.PhysicalWorlds.Add(new PhysicalWorldEntry { Name = "Beta" });
        nav.CurrentIndex = 1;

        // Act
        nav.NextCommand.Execute(null);

        // Assert - index unchanged, no further next
        Assert.AreEqual(1, nav.CurrentIndex);
        Assert.IsFalse(nav.HasNext);
    }

    [TestMethod]
    public void Previous_AtFirstItem_DoesNotMoveBelowZero()
    {
        // Arrange - two entries, positioned at first
        var viewModel = ArrangeActivatedViewModel(out _);
        var nav = viewModel.PhysicalWorldNav;
        viewModel.PhysicalWorlds.Add(new PhysicalWorldEntry { Name = "Alpha" });
        viewModel.PhysicalWorlds.Add(new PhysicalWorldEntry { Name = "Beta" });
        nav.CurrentIndex = 0;

        // Act
        nav.PreviousCommand.Execute(null);

        // Assert - index stays 0, no previous available
        Assert.AreEqual(0, nav.CurrentIndex);
        Assert.IsFalse(nav.HasPrevious);
    }

    [TestMethod]
    public void PreviousNext_SingleItem_BothDisabled()
    {
        // Arrange - exactly one entry
        var viewModel = ArrangeActivatedViewModel(out _);
        var nav = viewModel.PhysicalWorldNav;
        nav.Add();

        // Assert initial state
        Assert.IsFalse(nav.HasPrevious);
        Assert.IsFalse(nav.HasNext);

        // Act - both commands are no-ops
        nav.PreviousCommand.Execute(null);
        nav.NextCommand.Execute(null);

        // Assert - still on the single item
        Assert.AreEqual(0, nav.CurrentIndex);
    }

    [TestMethod]
    public void Next_FromFirst_AdvancesAndUpdatesPositionDisplay()
    {
        // Arrange - two entries at first position
        var viewModel = ArrangeActivatedViewModel(out _);
        var nav = viewModel.PhysicalWorldNav;
        viewModel.PhysicalWorlds.Add(new PhysicalWorldEntry { Name = "Alpha" });
        viewModel.PhysicalWorlds.Add(new PhysicalWorldEntry { Name = "Beta" });
        nav.CurrentIndex = 0;
        Assert.AreEqual("1 of 2", nav.PositionDisplay);

        // Act
        nav.NextCommand.Execute(null);

        // Assert
        Assert.AreEqual(1, nav.CurrentIndex);
        Assert.AreEqual("2 of 2", nav.PositionDisplay);
    }

    [TestMethod]
    public void RemoveCurrent_LastItem_ClampsIndexToNewLast()
    {
        // Arrange - three entries, positioned at last
        var viewModel = ArrangeActivatedViewModel(out _);
        var nav = viewModel.PhysicalWorldNav;
        viewModel.PhysicalWorlds.Add(new PhysicalWorldEntry { Name = "Alpha" });
        viewModel.PhysicalWorlds.Add(new PhysicalWorldEntry { Name = "Beta" });
        viewModel.PhysicalWorlds.Add(new PhysicalWorldEntry { Name = "Gamma" });
        nav.CurrentIndex = 2;

        // Act
        nav.RemoveCurrent();

        // Assert - index clamps down to the new last item
        Assert.AreEqual(2, viewModel.PhysicalWorlds.Count);
        Assert.AreEqual(1, nav.CurrentIndex);
    }

    [TestMethod]
    public void RemoveCurrent_MiddleItem_KeepsIndexPointingToNext()
    {
        // Arrange - three distinct entries, positioned in the middle
        var viewModel = ArrangeActivatedViewModel(out _);
        var nav = viewModel.PhysicalWorldNav;
        viewModel.PhysicalWorlds.Add(new PhysicalWorldEntry { Name = "Alpha" });
        viewModel.PhysicalWorlds.Add(new PhysicalWorldEntry { Name = "Beta" });
        viewModel.PhysicalWorlds.Add(new PhysicalWorldEntry { Name = "Gamma" });
        nav.CurrentIndex = 1;

        // Act
        nav.RemoveCurrent();

        // Assert - index stays put; CurrentItem now points at former index 2 (Gamma).
        // This documents ListNavigator.RemoveCurrent's no-decrement-on-middle behavior.
        Assert.AreEqual(2, viewModel.PhysicalWorlds.Count);
        Assert.AreEqual(1, nav.CurrentIndex);
        Assert.AreEqual("Gamma", nav.CurrentItem.Name);
    }

    [TestMethod]
    public void RemoveCurrent_OnlyItem_LeavesEmptyNavigator()
    {
        // Arrange - single entry
        var viewModel = ArrangeActivatedViewModel(out _);
        var nav = viewModel.PhysicalWorldNav;
        nav.Add();

        // Act
        nav.RemoveCurrent();

        // Assert - empty navigator state
        Assert.AreEqual(0, viewModel.PhysicalWorlds.Count);
        Assert.IsFalse(nav.HasItems);
        Assert.IsNull(nav.CurrentItem);
        Assert.AreEqual("0 of 0", nav.PositionDisplay);
    }

    [TestMethod]
    public void Navigate_WithZeroEntries_IsNoOp()
    {
        // Arrange - freshly activated model with empty lists
        var viewModel = ArrangeActivatedViewModel(out _);
        var nav = viewModel.PhysicalWorldNav;

        // Assert empty state
        Assert.IsFalse(nav.HasItems);
        Assert.IsFalse(nav.HasPrevious);
        Assert.IsFalse(nav.HasNext);
        Assert.IsNull(nav.CurrentItem);
        Assert.AreEqual("0 of 0", nav.PositionDisplay);

        // Act - commands must not throw or move the index
        nav.PreviousCommand.Execute(null);
        nav.NextCommand.Execute(null);

        // Assert
        Assert.AreEqual(0, nav.CurrentIndex);
    }

    [TestMethod]
    public void LoadModel_EditViaNavigator_SaveModel_PreservesData()
    {
        // Arrange - activate, add an entry, edit it via VM proxy properties
        var viewModel = ArrangeActivatedViewModel(out var model);
        viewModel.PhysicalWorldNav.Add();
        viewModel.CurrentPhysicalWorldName = "Terra";
        viewModel.CurrentPhysicalWorldGeography = "Mountainous";

        // Act - persist VM collections back into the model
        viewModel.SaveModel();

        // Assert - model captured the edited entry
        Assert.AreEqual(1, model.PhysicalWorlds.Count);
        Assert.AreEqual("Terra", model.PhysicalWorlds[0].Name);
        Assert.AreEqual("Mountainous", model.PhysicalWorlds[0].Geography);

        // Act - re-activate the same model to prove Reset + ReloadCollection round-trips
        viewModel.Activate(model);

        // Assert - navigator re-loads the same entry
        Assert.AreEqual(1, viewModel.PhysicalWorlds.Count);
        Assert.AreEqual(0, viewModel.PhysicalWorldNav.CurrentIndex);
        Assert.AreEqual("Terra", viewModel.PhysicalWorldNav.CurrentItem.Name);
        Assert.AreEqual("Mountainous", viewModel.PhysicalWorldNav.CurrentItem.Geography);
    }
}
