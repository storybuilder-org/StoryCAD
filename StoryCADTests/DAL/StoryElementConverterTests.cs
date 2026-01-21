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
}
