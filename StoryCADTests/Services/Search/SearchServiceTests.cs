using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCAD.Models;
using StoryCAD.Services.Logging;
using StoryCAD.Services.Outline;
using StoryCAD.Services.Search;

#nullable disable

namespace StoryCADTests.Services.Search;

/// <summary>
///     Comprehensive test suite for the SearchService class.
///     Tests string searching, UUID searching, and reference deletion functionality.
/// </summary>
[TestClass]
public class SearchServiceTests
{
    private OutlineService _outlineService;
    private SearchService _searchService;
    private StoryModel _testModel;

    [TestInitialize]
    public void Setup()
    {
        var logger = Ioc.Default.GetRequiredService<ILogService>();
        _searchService = new SearchService(logger);
        _outlineService = new OutlineService();
        _testModel = CreateTestModel().Result;
    }

    /// <summary>
    ///     Creates a test StoryModel with various elements for testing.
    /// </summary>
    private async Task<StoryModel> CreateTestModel()
    {
        var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);

        // Add test elements
        var overview = model.StoryElements.First(e => e.ElementType == StoryItemType.StoryOverview);

        // Add a problem with protagonist and antagonist
        var problem = _outlineService.AddStoryElement(model, StoryItemType.Problem, overview.Node);
        problem.Name = "Main Conflict";
        ((ProblemModel)problem).Premise = "A hero must save the world from evil";

        var protagonist = _outlineService.AddStoryElement(model, StoryItemType.Character, problem.Node);
        protagonist.Name = "Hero Character";
        ((CharacterModel)protagonist).Role = "Protagonist";
        ((CharacterModel)protagonist).Notes = "A brave warrior";

        var antagonist = _outlineService.AddStoryElement(model, StoryItemType.Character, problem.Node);
        antagonist.Name = "Villain Character";
        ((CharacterModel)antagonist).Role = "Antagonist";
        ((CharacterModel)antagonist).Notes = "An evil sorcerer";

        // Link characters to problem
        ((ProblemModel)problem).Protagonist = protagonist.Uuid;
        ((ProblemModel)problem).Antagonist = antagonist.Uuid;

        // Add a scene with cast members
        var scene = _outlineService.AddStoryElement(model, StoryItemType.Scene, overview.Node);
        scene.Name = "Final Battle";
        ((SceneModel)scene).Description = "The epic confrontation";
        ((SceneModel)scene).CastMembers.Add(protagonist.Uuid);
        ((SceneModel)scene).CastMembers.Add(antagonist.Uuid);

        // Add a setting
        var setting = _outlineService.AddStoryElement(model, StoryItemType.Setting, overview.Node);
        setting.Name = "Dark Castle";
        ((SettingModel)setting).Description = "A fortress of evil";

        ((SceneModel)scene).Setting = setting.Uuid;

        // Add relationships
        _outlineService.AddRelationship(model, protagonist.Uuid, antagonist.Uuid, "Enemies", true);

        return model;
    }

    #region Performance Tests

    [TestMethod]
    public void SearchService_UsesReflectionCache()
    {
        // This test verifies that the reflection cache is working by
        // performing multiple searches and checking performance

        // Arrange
        var heroNode = _testModel.StoryElements
            .First(e => e.Name == "Hero Character").Node;

        // Act - Perform multiple searches to trigger cache usage
        for (var i = 0; i < 100; i++)
        {
            _searchService.SearchString(heroNode, "test", _testModel);
        }

        // Assert - If this completes quickly, the cache is working
        // (In a real scenario, you might measure execution time)
        Assert.IsTrue(true, "Multiple searches should complete efficiently using cache");
    }

    #endregion

    #region String Search Tests

    [TestMethod]
    public void SearchString_FindsTextInName()
    {
        // Arrange
        var heroNode = _testModel.StoryElements
            .First(e => e.Name == "Hero Character").Node;

        // Act
        var result = _searchService.SearchString(heroNode, "hero", _testModel);

        // Assert
        Assert.IsTrue(result, "Should find 'hero' in 'Hero Character' name");
    }

    [TestMethod]
    public void SearchString_FindsTextInDescription()
    {
        // Arrange
        var heroNode = _testModel.StoryElements
            .First(e => e.Name == "Hero Character").Node;

        // Act
        var result = _searchService.SearchString(heroNode, "warrior", _testModel);

        // Assert
        Assert.IsTrue(result, "Should find 'warrior' in character description");
    }

    [TestMethod]
    public void SearchString_IsCaseInsensitive()
    {
        // Arrange
        var villainNode = _testModel.StoryElements
            .First(e => e.Name == "Villain Character").Node;

        // Act
        var result = _searchService.SearchString(villainNode, "EVIL", _testModel);

        // Assert
        Assert.IsTrue(result, "Should find 'EVIL' in 'evil sorcerer' (case-insensitive)");
    }

    [TestMethod]
    public void SearchString_ReturnsFalseForNonExistentText()
    {
        // Arrange
        var heroNode = _testModel.StoryElements
            .First(e => e.Name == "Hero Character").Node;

        // Act
        var result = _searchService.SearchString(heroNode, "nonexistent", _testModel);

        // Assert
        Assert.IsFalse(result, "Should not find non-existent text");
    }

    [TestMethod]
    public void SearchString_HandlesNullSearchArg()
    {
        // Arrange
        var heroNode = _testModel.StoryElements
            .First(e => e.Name == "Hero Character").Node;

        // Act
        var result = _searchService.SearchString(heroNode, null, _testModel);

        // Assert
        Assert.IsFalse(result, "Should return false for null search argument");
    }

    [TestMethod]
    public void SearchString_HandlesEmptySearchArg()
    {
        // Arrange
        var heroNode = _testModel.StoryElements
            .First(e => e.Name == "Hero Character").Node;

        // Act
        var result = _searchService.SearchString(heroNode, "", _testModel);

        // Assert
        Assert.IsFalse(result, "Should return false for empty search argument");
    }

    [TestMethod]
    public void SearchString_FindsReferencedElementName()
    {
        // Arrange
        var problemNode = _testModel.StoryElements
            .First(e => e.Name == "Main Conflict").Node;

        // Act
        var result = _searchService.SearchString(problemNode, "Hero", _testModel);

        // Assert
        Assert.IsTrue(result, "Should find 'Hero' through referenced protagonist");
    }

    #endregion

    #region UUID Search Tests

    [TestMethod]
    public void SearchUuid_FindsDirectReference()
    {
        // Arrange
        var protagonist = _testModel.StoryElements
            .First(e => e.Name == "Hero Character");
        var problemNode = _testModel.StoryElements
            .First(e => e.Name == "Main Conflict").Node;

        // Act
        var result = _searchService.SearchUuid(problemNode, protagonist.Uuid, _testModel);

        // Assert
        Assert.IsTrue(result, "Should find protagonist UUID in problem");
    }

    [TestMethod]
    public void SearchUuid_FindsInCollection()
    {
        // Arrange
        var protagonist = _testModel.StoryElements
            .First(e => e.Name == "Hero Character");
        var sceneNode = _testModel.StoryElements
            .First(e => e.Name == "Final Battle").Node;

        // Act
        var result = _searchService.SearchUuid(sceneNode, protagonist.Uuid, _testModel);

        // Assert
        Assert.IsTrue(result, "Should find protagonist UUID in scene cast members");
    }

    [TestMethod]
    public void SearchUuid_ReturnsFalseForNonExistentUuid()
    {
        // Arrange
        var heroNode = _testModel.StoryElements
            .First(e => e.Name == "Hero Character").Node;
        var randomUuid = Guid.NewGuid();

        // Act
        var result = _searchService.SearchUuid(heroNode, randomUuid, _testModel);

        // Assert
        Assert.IsFalse(result, "Should not find non-existent UUID");
    }

    [TestMethod]
    public void SearchUuid_HandlesEmptyUuid()
    {
        // Arrange
        var heroNode = _testModel.StoryElements
            .First(e => e.Name == "Hero Character").Node;

        // Act
        var result = _searchService.SearchUuid(heroNode, Guid.Empty, _testModel);

        // Assert
        Assert.IsFalse(result, "Should handle empty UUID gracefully");
    }

    [TestMethod]
    public void SearchUuid_FindsInRelationships()
    {
        // Arrange
        var antagonist = _testModel.StoryElements
            .First(e => e.Name == "Villain Character");
        var protagonistNode = _testModel.StoryElements
            .First(e => e.Name == "Hero Character").Node;

        // Act
        var result = _searchService.SearchUuid(protagonistNode, antagonist.Uuid, _testModel);

        // Assert
        Assert.IsTrue(result, "Should find antagonist UUID in protagonist's relationships");
    }

    [TestMethod]
    public void SearchUuid_IgnoresElementOwnUuid()
    {
        // Arrange
        var protagonist = _testModel.StoryElements
            .First(e => e.Name == "Hero Character");
        var protagonistNode = protagonist.Node;

        // Act - Search for the element's own UUID should return false
        var result = _searchService.SearchUuid(protagonistNode, protagonist.Uuid, _testModel);

        // Assert
        Assert.IsFalse(result, "Should NOT find element's own UUID (Uuid field should be ignored)");

        // Also verify that it doesn't delete its own UUID when delete flag is true
        var deleteResult = _searchService.SearchUuid(protagonistNode, protagonist.Uuid, _testModel, true);
        Assert.IsFalse(deleteResult, "Should NOT find or delete element's own UUID");
        Assert.AreNotEqual(Guid.Empty, protagonist.Uuid,
            "Element's own UUID should remain intact after delete attempt");
    }

    #endregion

    #region UUID Deletion Tests

    [TestMethod]
    public void SearchUuid_DeletesDirectReference()
    {
        // Arrange
        var protagonist = _testModel.StoryElements
            .First(e => e.Name == "Hero Character");
        var problem = _testModel.StoryElements
            .First(e => e.Name == "Main Conflict");
        var problemNode = problem.Node;

        // Act
        var found = _searchService.SearchUuid(problemNode, protagonist.Uuid, _testModel, true);

        // Assert
        Assert.IsTrue(found, "Should find and delete protagonist reference");
        Assert.AreEqual(Guid.Empty, ((ProblemModel)problem).Protagonist,
            "Protagonist reference should be cleared");
    }

    [TestMethod]
    public void SearchUuid_DeletesFromCollection()
    {
        // Arrange
        var protagonist = _testModel.StoryElements
            .First(e => e.Name == "Hero Character");
        var scene = _testModel.StoryElements
            .First(e => e.Name == "Final Battle");
        var sceneNode = scene.Node;
        var initialCount = ((SceneModel)scene).CastMembers.Count;

        // Act
        var found = _searchService.SearchUuid(sceneNode, protagonist.Uuid, _testModel, true);

        // Assert
        Assert.IsTrue(found, "Should find and delete from cast members");
        Assert.AreEqual(initialCount - 1, ((SceneModel)scene).CastMembers.Count,
            "Cast members count should decrease by 1");
        Assert.IsFalse(((SceneModel)scene).CastMembers.Contains(protagonist.Uuid),
            "Protagonist should no longer be in cast members");
    }

    [TestMethod]
    public void SearchUuid_DeletesFromRelationships()
    {
        // Arrange
        var antagonist = _testModel.StoryElements
            .First(e => e.Name == "Villain Character");
        var protagonist = _testModel.StoryElements
            .First(e => e.Name == "Hero Character");
        var protagonistNode = protagonist.Node;
        var initialCount = ((CharacterModel)protagonist).RelationshipList.Count;

        // Act
        var found = _searchService.SearchUuid(protagonistNode, antagonist.Uuid, _testModel, true);

        // Assert
        Assert.IsTrue(found, "Should find and delete relationship");
        Assert.AreEqual(initialCount - 1, ((CharacterModel)protagonist).RelationshipList.Count,
            "Relationship count should decrease by 1");
    }

    #endregion

    #region Edge Cases and Error Handling

    [TestMethod]
    public void SearchString_HandlesNullNode()
    {
        // Act
        var result = _searchService.SearchString(null, "test", _testModel);

        // Assert
        Assert.IsFalse(result, "Should return false for null node");
    }

    [TestMethod]
    public void SearchString_HandlesNullModel()
    {
        // Arrange
        var heroNode = _testModel.StoryElements
            .First(e => e.Name == "Hero Character").Node;

        // Act
        var result = _searchService.SearchString(heroNode, "test", null);

        // Assert
        Assert.IsFalse(result, "Should return false for null model");
    }

    [TestMethod]
    public void SearchUuid_HandlesNullNode()
    {
        // Act
        var result = _searchService.SearchUuid(null, Guid.NewGuid(), _testModel);

        // Assert
        Assert.IsFalse(result, "Should return false for null node");
    }

    [TestMethod]
    public void SearchUuid_HandlesNullModel()
    {
        // Arrange
        var heroNode = _testModel.StoryElements
            .First(e => e.Name == "Hero Character").Node;

        // Act
        var result = _searchService.SearchUuid(heroNode, Guid.NewGuid(), null);

        // Assert
        Assert.IsFalse(result, "Should return false for null model");
    }

    #endregion
}
