using StoryCADLib.Models.StoryWorld;

namespace StoryCADTests.Models;

[TestClass]
public class WorldEntryTests
{
    #region PhysicalWorldEntry Tests

    [TestMethod]
    public void PhysicalWorldEntry_DefaultConstructor_InitializesToEmpty()
    {
        var entry = new PhysicalWorldEntry();

        Assert.AreEqual(string.Empty, entry.Name);
        Assert.AreEqual(string.Empty, entry.Geography);
        Assert.AreEqual(string.Empty, entry.Climate);
        Assert.AreEqual(string.Empty, entry.NaturalResources);
        Assert.AreEqual(string.Empty, entry.Flora);
        Assert.AreEqual(string.Empty, entry.Fauna);
        Assert.AreEqual(string.Empty, entry.Astronomy);
    }

    [TestMethod]
    public void PhysicalWorldEntry_SetProperties_RetainsValues()
    {
        var entry = new PhysicalWorldEntry
        {
            Name = "Mars",
            Geography = "Red desert planet",
            Climate = "Cold and thin atmosphere"
        };

        Assert.AreEqual("Mars", entry.Name);
        Assert.AreEqual("Red desert planet", entry.Geography);
        Assert.AreEqual("Cold and thin atmosphere", entry.Climate);
    }

    [TestMethod]
    public void PhysicalWorldEntry_Clone_CreatesIndependentCopy()
    {
        var original = new PhysicalWorldEntry
        {
            Name = "Earth", Geography = "Varied", Climate = "Temperate",
            NaturalResources = "Abundant", Flora = "Diverse", Fauna = "Rich", Astronomy = "One moon"
        };

        var clone = original.Clone();
        clone.Name = "Modified";

        Assert.AreEqual("Earth", original.Name);
        Assert.AreEqual("Modified", clone.Name);
        Assert.AreEqual("Varied", clone.Geography);
    }

    #endregion

    #region SpeciesEntry Tests

    [TestMethod]
    public void SpeciesEntry_DefaultConstructor_InitializesToEmpty()
    {
        var entry = new SpeciesEntry();

        Assert.AreEqual(string.Empty, entry.Name);
        Assert.AreEqual(string.Empty, entry.PhysicalTraits);
        Assert.AreEqual(string.Empty, entry.Lifespan);
        Assert.AreEqual(string.Empty, entry.Origins);
        Assert.AreEqual(string.Empty, entry.SocialStructure);
        Assert.AreEqual(string.Empty, entry.Diversity);
    }

    [TestMethod]
    public void SpeciesEntry_SetProperties_RetainsValues()
    {
        var entry = new SpeciesEntry
        {
            Name = "Elves",
            PhysicalTraits = "Tall, pointed ears, ageless",
            Lifespan = "Immortal"
        };

        Assert.AreEqual("Elves", entry.Name);
        Assert.AreEqual("Tall, pointed ears, ageless", entry.PhysicalTraits);
        Assert.AreEqual("Immortal", entry.Lifespan);
    }

    [TestMethod]
    public void SpeciesEntry_Clone_CreatesIndependentCopy()
    {
        var original = new SpeciesEntry { Name = "Dwarves", PhysicalTraits = "Short" };
        var clone = original.Clone();
        clone.Name = "Modified";

        Assert.AreEqual("Dwarves", original.Name);
        Assert.AreEqual("Modified", clone.Name);
        Assert.AreEqual("Short", clone.PhysicalTraits);
    }

    #endregion

    #region CultureEntry Tests

    [TestMethod]
    public void CultureEntry_DefaultConstructor_InitializesToEmpty()
    {
        var entry = new CultureEntry();

        Assert.AreEqual(string.Empty, entry.Name);
        Assert.AreEqual(string.Empty, entry.Values);
        Assert.AreEqual(string.Empty, entry.Customs);
        Assert.AreEqual(string.Empty, entry.Taboos);
        Assert.AreEqual(string.Empty, entry.Art);
        Assert.AreEqual(string.Empty, entry.DailyLife);
        Assert.AreEqual(string.Empty, entry.Entertainment);
    }

    [TestMethod]
    public void CultureEntry_SetProperties_RetainsValues()
    {
        var entry = new CultureEntry
        {
            Name = "Wall Street",
            Values = "Wealth, status, winning",
            Taboos = "Showing weakness, admitting failure"
        };

        Assert.AreEqual("Wall Street", entry.Name);
        Assert.AreEqual("Wealth, status, winning", entry.Values);
        Assert.AreEqual("Showing weakness, admitting failure", entry.Taboos);
    }

    [TestMethod]
    public void CultureEntry_Clone_CreatesIndependentCopy()
    {
        var original = new CultureEntry { Name = "Samurai", Values = "Honor" };
        var clone = original.Clone();
        clone.Name = "Modified";

        Assert.AreEqual("Samurai", original.Name);
        Assert.AreEqual("Modified", clone.Name);
        Assert.AreEqual("Honor", clone.Values);
    }

    #endregion

    #region GovernmentEntry Tests

    [TestMethod]
    public void GovernmentEntry_DefaultConstructor_InitializesToEmpty()
    {
        var entry = new GovernmentEntry();

        Assert.AreEqual(string.Empty, entry.Name);
        Assert.AreEqual(string.Empty, entry.Type);
        Assert.AreEqual(string.Empty, entry.PowerStructures);
        Assert.AreEqual(string.Empty, entry.Laws);
        Assert.AreEqual(string.Empty, entry.ClassStructure);
        Assert.AreEqual(string.Empty, entry.ForeignRelations);
    }

    [TestMethod]
    public void GovernmentEntry_SetProperties_RetainsValues()
    {
        var entry = new GovernmentEntry
        {
            Name = "The Ministry of Magic",
            Type = "Bureaucratic oligarchy",
            Laws = "Statute of Secrecy"
        };

        Assert.AreEqual("The Ministry of Magic", entry.Name);
        Assert.AreEqual("Bureaucratic oligarchy", entry.Type);
        Assert.AreEqual("Statute of Secrecy", entry.Laws);
    }

    [TestMethod]
    public void GovernmentEntry_Clone_CreatesIndependentCopy()
    {
        var original = new GovernmentEntry { Name = "Senate", Type = "Republic" };
        var clone = original.Clone();
        clone.Name = "Modified";

        Assert.AreEqual("Senate", original.Name);
        Assert.AreEqual("Modified", clone.Name);
        Assert.AreEqual("Republic", clone.Type);
    }

    #endregion

    #region ReligionEntry Tests

    [TestMethod]
    public void ReligionEntry_DefaultConstructor_InitializesToEmpty()
    {
        var entry = new ReligionEntry();

        Assert.AreEqual(string.Empty, entry.Name);
        Assert.AreEqual(string.Empty, entry.Deities);
        Assert.AreEqual(string.Empty, entry.Beliefs);
        Assert.AreEqual(string.Empty, entry.Practices);
        Assert.AreEqual(string.Empty, entry.Organizations);
        Assert.AreEqual(string.Empty, entry.CreationMyths);
    }

    [TestMethod]
    public void ReligionEntry_SetProperties_RetainsValues()
    {
        var entry = new ReligionEntry
        {
            Name = "Faith of the Seven",
            Deities = "Seven aspects of one god",
            Practices = "Sept worship, trials by combat"
        };

        Assert.AreEqual("Faith of the Seven", entry.Name);
        Assert.AreEqual("Seven aspects of one god", entry.Deities);
        Assert.AreEqual("Sept worship, trials by combat", entry.Practices);
    }

    [TestMethod]
    public void ReligionEntry_Clone_CreatesIndependentCopy()
    {
        var original = new ReligionEntry { Name = "Old Gods", Deities = "Nameless" };
        var clone = original.Clone();
        clone.Name = "Modified";

        Assert.AreEqual("Old Gods", original.Name);
        Assert.AreEqual("Modified", clone.Name);
        Assert.AreEqual("Nameless", clone.Deities);
    }

    #endregion
}
