using System.ComponentModel;
using StoryCADLib.Models;
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
    public void PhysicalWorldEntry_PropertyChanged_RaisesNotification()
    {
        var entry = new PhysicalWorldEntry();
        var changed = new List<string>();
        entry.PropertyChanged += (_, e) => changed.Add(e.PropertyName!);

        entry.Name = "Terra";
        entry.Geography = "Mountains";

        CollectionAssert.Contains(changed, nameof(PhysicalWorldEntry.Name));
        CollectionAssert.Contains(changed, nameof(PhysicalWorldEntry.Geography));
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
    public void SpeciesEntry_PropertyChanged_RaisesNotification()
    {
        var entry = new SpeciesEntry();
        var changed = new List<string>();
        entry.PropertyChanged += (_, e) => changed.Add(e.PropertyName!);

        entry.Name = "Dwarves";
        entry.PhysicalTraits = "Short and sturdy";

        CollectionAssert.Contains(changed, nameof(SpeciesEntry.Name));
        CollectionAssert.Contains(changed, nameof(SpeciesEntry.PhysicalTraits));
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
    public void CultureEntry_PropertyChanged_RaisesNotification()
    {
        var entry = new CultureEntry();
        var changed = new List<string>();
        entry.PropertyChanged += (_, e) => changed.Add(e.PropertyName!);

        entry.Name = "Samurai";
        entry.Values = "Honor, discipline";

        CollectionAssert.Contains(changed, nameof(CultureEntry.Name));
        CollectionAssert.Contains(changed, nameof(CultureEntry.Values));
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
    public void GovernmentEntry_PropertyChanged_RaisesNotification()
    {
        var entry = new GovernmentEntry();
        var changed = new List<string>();
        entry.PropertyChanged += (_, e) => changed.Add(e.PropertyName!);

        entry.Name = "Senate";
        entry.Type = "Republic";

        CollectionAssert.Contains(changed, nameof(GovernmentEntry.Name));
        CollectionAssert.Contains(changed, nameof(GovernmentEntry.Type));
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
    public void ReligionEntry_PropertyChanged_RaisesNotification()
    {
        var entry = new ReligionEntry();
        var changed = new List<string>();
        entry.PropertyChanged += (_, e) => changed.Add(e.PropertyName!);

        entry.Name = "Old Gods";
        entry.Deities = "Nameless nature spirits";

        CollectionAssert.Contains(changed, nameof(ReligionEntry.Name));
        CollectionAssert.Contains(changed, nameof(ReligionEntry.Deities));
    }

    #endregion
}
