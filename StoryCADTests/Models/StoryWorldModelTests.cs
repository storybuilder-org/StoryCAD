using StoryCADLib.Models;
using StoryCADLib.Models.StoryWorld;

namespace StoryCADTests.Models;

[TestClass]
public class StoryWorldModelTests
{
    private StoryModel _storyModel = null!;

    [TestInitialize]
    public void Setup()
    {
        _storyModel = new StoryModel();
    }

    #region Constructor Tests

    [TestMethod]
    public void Constructor_WithDefaultName_SetsNameToStoryWorld()
    {
        var world = new StoryWorldModel(_storyModel, null);

        Assert.AreEqual("Story World", world.Name);
    }

    [TestMethod]
    public void Constructor_WithCustomName_SetsName()
    {
        var world = new StoryWorldModel("Middle Earth", _storyModel, null);

        Assert.AreEqual("Middle Earth", world.Name);
    }

    [TestMethod]
    public void Constructor_SetsElementType_ToStoryWorld()
    {
        var world = new StoryWorldModel(_storyModel, null);

        Assert.AreEqual(StoryItemType.StoryWorld, world.ElementType);
    }

    [TestMethod]
    public void Constructor_InitializesAllPropertiesToEmpty()
    {
        var world = new StoryWorldModel(_storyModel, null);

        // Structure tab
        Assert.AreEqual(string.Empty, world.WorldType);
        Assert.AreEqual(string.Empty, world.Ontology);
        Assert.AreEqual(string.Empty, world.WorldRelation);
        Assert.AreEqual(string.Empty, world.RuleTransparency);
        Assert.AreEqual(string.Empty, world.ScaleOfDifference);
        Assert.AreEqual(string.Empty, world.AgencySource);
        Assert.AreEqual(string.Empty, world.ToneLogic);

        // History tab (single)
        Assert.AreEqual(string.Empty, world.FoundingEvents);
        Assert.AreEqual(string.Empty, world.MajorConflicts);
        Assert.AreEqual(string.Empty, world.Eras);
        Assert.AreEqual(string.Empty, world.TechnologicalShifts);
        Assert.AreEqual(string.Empty, world.LostKnowledge);

        // Economy tab (single)
        Assert.AreEqual(string.Empty, world.EconomicSystem);
        Assert.AreEqual(string.Empty, world.Currency);
        Assert.AreEqual(string.Empty, world.TradeRoutes);
        Assert.AreEqual(string.Empty, world.Professions);
        Assert.AreEqual(string.Empty, world.WealthDistribution);

        // Magic/Technology tab (single)
        Assert.AreEqual(string.Empty, world.SystemType);
        Assert.AreEqual(string.Empty, world.Source);
        Assert.AreEqual(string.Empty, world.Rules);
        Assert.AreEqual(string.Empty, world.Limitations);
        Assert.AreEqual(string.Empty, world.Cost);
        Assert.AreEqual(string.Empty, world.Practitioners);
        Assert.AreEqual(string.Empty, world.SocialImpact);
    }

    [TestMethod]
    public void Constructor_InitializesListsToEmpty()
    {
        var world = new StoryWorldModel(_storyModel, null);

        Assert.IsNotNull(world.PhysicalWorlds);
        Assert.AreEqual(0, world.PhysicalWorlds.Count);

        Assert.IsNotNull(world.Species);
        Assert.AreEqual(0, world.Species.Count);

        Assert.IsNotNull(world.Cultures);
        Assert.AreEqual(0, world.Cultures.Count);

        Assert.IsNotNull(world.Governments);
        Assert.AreEqual(0, world.Governments.Count);

        Assert.IsNotNull(world.Religions);
        Assert.AreEqual(0, world.Religions.Count);
    }

    #endregion

    #region Property Tests

    [TestMethod]
    public void WorldType_SetAndGet_ReturnsValue()
    {
        var world = new StoryWorldModel(_storyModel, null);

        world.WorldType = "Hidden World";

        Assert.AreEqual("Hidden World", world.WorldType);
    }

    [TestMethod]
    public void PhysicalWorlds_CanAddEntry()
    {
        var world = new StoryWorldModel(_storyModel, null);
        var entry = new PhysicalWorldEntry { Name = "Earth" };

        world.PhysicalWorlds.Add(entry);

        Assert.AreEqual(1, world.PhysicalWorlds.Count);
        Assert.AreEqual("Earth", world.PhysicalWorlds[0].Name);
    }

    [TestMethod]
    public void Cultures_CanAddEntry()
    {
        var world = new StoryWorldModel(_storyModel, null);
        var entry = new CultureEntry { Name = "Wizarding World" };

        world.Cultures.Add(entry);

        Assert.AreEqual(1, world.Cultures.Count);
        Assert.AreEqual("Wizarding World", world.Cultures[0].Name);
    }

    #endregion

    #region Serialization Tests

    [TestMethod]
    public void ParameterlessConstructor_ExistsForDeserialization()
    {
        var world = new StoryWorldModel();

        Assert.IsNotNull(world);
    }

    #endregion
}
