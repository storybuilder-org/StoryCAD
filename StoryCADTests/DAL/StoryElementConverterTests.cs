using System.Text.Json;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCADLib.DAL;
using StoryCADLib.Models;
using StoryCADLib.Models.StoryWorld;

#nullable disable

namespace StoryCADTests.DAL;

/// <summary>
/// Tests for StoryElementConverter JSON serialization/deserialization.
/// Each StoryElement type must be registered in both Read() and Write() switch statements.
/// </summary>
[TestClass]
public class StoryElementConverterTests
{
    private JsonSerializerOptions _serializerOptions;

    [TestInitialize]
    public void Setup()
    {
        _serializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters =
            {
                new EmptyGuidConverter(),
                new StoryElementConverter(),
                new JsonStringEnumConverter()
            }
        };
    }

    #region StoryWorld Serialization Tests

    [TestMethod]
    public void Write_StoryWorldModel_ProducesCorrectTypeDiscriminator()
    {
        // Arrange
        var model = new StoryModel();
        var storyWorld = new StoryWorldModel(model, null);
        storyWorld.WorldType = "Hidden World";

        // Act
        var json = JsonSerializer.Serialize<StoryElement>(storyWorld, _serializerOptions);

        // Assert
        Assert.IsTrue(json.Contains("\"Type\": \"StoryWorld\""),
            $"JSON should contain StoryWorld type discriminator. Actual JSON: {json}");
    }

    [TestMethod]
    public void Read_StoryWorldJson_DeserializesToStoryWorldModel()
    {
        // Arrange
        var json = """
        {
            "Type": "StoryWorld",
            "Uuid": "12345678-1234-1234-1234-123456789abc",
            "Name": "Story World",
            "WorldType": "Hidden World"
        }
        """;

        // Act
        var element = JsonSerializer.Deserialize<StoryElement>(json, _serializerOptions);

        // Assert
        Assert.IsNotNull(element, "Deserialized element should not be null");
        Assert.IsInstanceOfType(element, typeof(StoryWorldModel),
            $"Element should be StoryWorldModel, but was {element?.GetType().Name}");
        var storyWorld = (StoryWorldModel)element;
        Assert.AreEqual("Hidden World", storyWorld.WorldType);
    }

    [TestMethod]
    public void RoundTrip_StoryWorldModel_PreservesAllProperties()
    {
        // Arrange
        var model = new StoryModel();
        var storyWorld = new StoryWorldModel(model, null);
        storyWorld.WorldType = "Constructed World";
        storyWorld.Ontology = "Scientific Speculative";
        storyWorld.WorldRelation = "Fully Independent";

        // Add a physical world entry
        var physicalWorld = new PhysicalWorldEntry
        {
            Name = "Middle Earth",
            Geography = "Mountains and forests",
            Climate = "Temperate"
        };
        storyWorld.PhysicalWorlds.Add(physicalWorld);

        // Act - Serialize then deserialize
        var json = JsonSerializer.Serialize<StoryElement>(storyWorld, _serializerOptions);
        var deserialized = JsonSerializer.Deserialize<StoryElement>(json, _serializerOptions);

        // Assert
        Assert.IsInstanceOfType(deserialized, typeof(StoryWorldModel));
        var result = (StoryWorldModel)deserialized;
        Assert.AreEqual("Constructed World", result.WorldType);
        Assert.AreEqual("Scientific Speculative", result.Ontology);
        Assert.AreEqual("Fully Independent", result.WorldRelation);
        Assert.AreEqual(1, result.PhysicalWorlds.Count);
        Assert.AreEqual("Middle Earth", result.PhysicalWorlds[0].Name);
        Assert.AreEqual("Mountains and forests", result.PhysicalWorlds[0].Geography);
    }

    #endregion

    #region All Element Types Round-Trip Test

    [TestMethod]
    public void RoundTrip_AllElementTypes_SerializeAndDeserializeCorrectly()
    {
        // Arrange - Create one of each element type
        var model = new StoryModel();
        var elements = new List<StoryElement>
        {
            new OverviewModel("Test Overview", model, null),
            new ProblemModel("Test Problem", model, null),
            new CharacterModel("Test Character", model, null),
            new SettingModel("Test Setting", model, null),
            new SceneModel("Test Scene", model, null),
            new FolderModel("Test Folder", model, StoryItemType.Folder, null),
            new FolderModel("Test Section", model, StoryItemType.Section, null),
            new FolderModel("Test Notes", model, StoryItemType.Notes, null),
            new WebModel(model, null),
            new TrashCanModel(model, null),
            new StoryWorldModel(model, null)
        };

        // Act & Assert - Each element should round-trip correctly
        foreach (var element in elements)
        {
            var json = JsonSerializer.Serialize<StoryElement>(element, _serializerOptions);
            var deserialized = JsonSerializer.Deserialize<StoryElement>(json, _serializerOptions);

            Assert.IsNotNull(deserialized,
                $"Failed to deserialize {element.GetType().Name}");
            Assert.AreEqual(element.GetType(), deserialized.GetType(),
                $"Type mismatch for {element.GetType().Name}: expected {element.GetType().Name}, got {deserialized.GetType().Name}");
            Assert.AreEqual(element.Name, deserialized.Name,
                $"Name mismatch for {element.GetType().Name}");
        }
    }

    #endregion

    #region Existing Element Types (Regression Tests)

    [TestMethod]
    public void RoundTrip_OverviewModel_PreservesProperties()
    {
        // Arrange
        var model = new StoryModel();
        var overview = new OverviewModel("My Story", model, null)
        {
            Author = "Test Author",
            StoryGenre = "Fantasy"
        };

        // Act
        var json = JsonSerializer.Serialize<StoryElement>(overview, _serializerOptions);
        var deserialized = (OverviewModel)JsonSerializer.Deserialize<StoryElement>(json, _serializerOptions);

        // Assert
        Assert.AreEqual("My Story", deserialized.Name);
        Assert.AreEqual("Test Author", deserialized.Author);
        Assert.AreEqual("Fantasy", deserialized.StoryGenre);
    }

    [TestMethod]
    public void RoundTrip_CharacterModel_PreservesProperties()
    {
        // Arrange
        var model = new StoryModel();
        var character = new CharacterModel("John Doe", model, null)
        {
            Role = "Protagonist",
            Age = "35"
        };

        // Act
        var json = JsonSerializer.Serialize<StoryElement>(character, _serializerOptions);
        var deserialized = (CharacterModel)JsonSerializer.Deserialize<StoryElement>(json, _serializerOptions);

        // Assert
        Assert.AreEqual("John Doe", deserialized.Name);
        Assert.AreEqual("Protagonist", deserialized.Role);
        Assert.AreEqual("35", deserialized.Age);
    }

    #endregion

    #region Description Field Migration Tests

    [TestMethod]
    public void Read_LegacyStoryIdea_MigratesToDescription()
    {
        // Arrange — legacy StoryOverview with StoryIdea but no Description
        var json = """
        {
            "Type": "StoryOverview",
            "Uuid": "11111111-1111-1111-1111-111111111111",
            "Name": "My Story",
            "StoryIdea": "A hero's journey through time"
        }
        """;

        // Act
        var element = JsonSerializer.Deserialize<StoryElement>(json, _serializerOptions);

        // Assert
        Assert.IsNotNull(element);
        Assert.IsInstanceOfType(element, typeof(OverviewModel));
        Assert.AreEqual("A hero's journey through time", element.Description);
    }

    [TestMethod]
    public void Read_AllLegacyFieldNames_MigrateToDescription()
    {
        // Each tuple: (Type discriminator, legacy field name, legacy value)
        var legacyCases = new (string type, string legacyField, string value)[]
        {
            ("StoryOverview", "StoryIdea", "idea text"),
            ("Problem", "StoryQuestion", "question text"),
            ("Character", "CharacterSketch", "sketch text"),
            ("Setting", "Summary", "summary text"),
            ("Scene", "Remarks", "remarks text"),
            ("Folder", "Notes", "notes text"),
        };

        foreach (var (type, legacyField, value) in legacyCases)
        {
            var json = $$"""
            {
                "Type": "{{type}}",
                "Uuid": "22222222-2222-2222-2222-222222222222",
                "Name": "Test Element",
                "{{legacyField}}": "{{value}}"
            }
            """;

            var element = JsonSerializer.Deserialize<StoryElement>(json, _serializerOptions);

            Assert.IsNotNull(element, $"Failed for type {type}");
            Assert.AreEqual(value, element.Description,
                $"Migration failed for type={type}, field={legacyField}");
        }
    }

    #endregion

    #region Unknown Element Type Test

    [TestMethod]
    public void Read_UnknownType_ThrowsJsonException()
    {
        var json = """
        {
            "Type": "FutureElement",
            "Uuid": "33333333-3333-3333-3333-333333333333",
            "Name": "Unknown"
        }
        """;

        var ex = Assert.ThrowsExactly<JsonException>(
            () => JsonSerializer.Deserialize<StoryElement>(json, _serializerOptions));
        Assert.IsTrue(ex.Message.Contains("FutureElement"),
            $"Exception message should mention the unknown type. Actual: {ex.Message}");
    }

    #endregion

    #region StoryWorld List Tests

    [TestMethod]
    public void RoundTrip_StoryWorldModel_PreservesSpeciesList()
    {
        // Arrange
        var model = new StoryModel();
        var storyWorld = new StoryWorldModel(model, null);
        storyWorld.PhysicalWorlds.Add(new PhysicalWorldEntry { Name = "Continent A", Geography = "Plains" });
        storyWorld.PhysicalWorlds.Add(new PhysicalWorldEntry { Name = "Continent B", Climate = "Tropical" });
        storyWorld.Species.Add(new SpeciesEntry { Name = "Elves", Lifespan = "Immortal" });
        storyWorld.Species.Add(new SpeciesEntry { Name = "Dwarves", PhysicalTraits = "Short and stocky" });

        // Act
        var json = JsonSerializer.Serialize<StoryElement>(storyWorld, _serializerOptions);
        var deserialized = (StoryWorldModel)JsonSerializer.Deserialize<StoryElement>(json, _serializerOptions);

        // Assert
        Assert.AreEqual(2, deserialized.PhysicalWorlds.Count);
        Assert.AreEqual("Continent A", deserialized.PhysicalWorlds[0].Name);
        Assert.AreEqual("Continent B", deserialized.PhysicalWorlds[1].Name);
        Assert.AreEqual(2, deserialized.Species.Count);
        Assert.AreEqual("Elves", deserialized.Species[0].Name);
        Assert.AreEqual("Immortal", deserialized.Species[0].Lifespan);
        Assert.AreEqual("Dwarves", deserialized.Species[1].Name);
        Assert.AreEqual("Short and stocky", deserialized.Species[1].PhysicalTraits);
    }

    [TestMethod]
    public void Deserialize_StoryWorldModel_MissingLists_InitializesEmpty()
    {
        // Arrange — JSON with no PhysicalWorlds or Species keys
        var json = """
        {
            "Type": "StoryWorld",
            "Uuid": "44444444-4444-4444-4444-444444444444",
            "Name": "Bare World",
            "WorldType": "Natural"
        }
        """;

        // Act
        var element = JsonSerializer.Deserialize<StoryElement>(json, _serializerOptions);

        // Assert
        Assert.IsInstanceOfType(element, typeof(StoryWorldModel));
        var storyWorld = (StoryWorldModel)element;
        Assert.IsNotNull(storyWorld.PhysicalWorlds, "PhysicalWorlds should not be null");
        Assert.IsNotNull(storyWorld.Species, "Species should not be null");
        Assert.AreEqual(0, storyWorld.PhysicalWorlds.Count);
        Assert.AreEqual(0, storyWorld.Species.Count);
    }

    #endregion
}
