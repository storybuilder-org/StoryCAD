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
    public void FontWeight_IsBold_WhenFieldHasContent()
    {
        // Arrange
        var viewModel = Ioc.Default.GetRequiredService<StoryWorldViewModel>();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var storyModel = new StoryModel();
        appState.CurrentDocument = new StoryDocument(storyModel);
        var model = new StoryWorldModel("Test", storyModel, null);
        viewModel.Activate(model);

        // Add a culture entry
        viewModel.AddCultureCommand.Execute(null);

        // Act - set content (RichEditBoxExtended normalizes to RTF, but any non-empty string works)
        viewModel.CurrentCultureValues = "Some content";

        // Assert - should be bold (weight 700)
        Assert.AreEqual((ushort)700, viewModel.CultureValuesFontWeight.Weight);
    }

    [TestMethod]
    public void FontWeight_IsNormal_WhenFieldIsEmpty()
    {
        // Arrange
        var viewModel = Ioc.Default.GetRequiredService<StoryWorldViewModel>();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var storyModel = new StoryModel();
        appState.CurrentDocument = new StoryDocument(storyModel);
        var model = new StoryWorldModel("Test", storyModel, null);
        viewModel.Activate(model);

        // Add a culture entry with content
        viewModel.AddCultureCommand.Execute(null);
        viewModel.CurrentCultureValues = "Some content";

        // Act - clear content (RichEditBoxExtended sets to "" when plain text is empty)
        viewModel.CurrentCultureValues = "";

        // Assert - should be normal (weight 400)
        Assert.AreEqual((ushort)400, viewModel.CultureValuesFontWeight.Weight);
    }
}
