using System.Text.Json;
using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCADLib.Models;
using StoryCADLib.Models.Tools;
using StoryCADLib.Services.API;
using StoryCADLib.Services.Outline;
using StoryCADLib.ViewModels;

#nullable disable

namespace StoryCADTests.Services.API;

[TestClass]
public class SemanticKernelApiTests
{
    private readonly SemanticKernelApi _api = new(Ioc.Default.GetRequiredService<OutlineService>(), Ioc.Default.GetRequiredService<ListData>(), Ioc.Default.GetRequiredService<ControlData>(), Ioc.Default.GetRequiredService<ToolsData>());

    [TestMethod]
    public async Task CreateOutlineWithInvalidTemplate()
    {
        // Arrange
        var name = "Test Outline";
        var author = "Test Author";
        var invalidTemplateIndex = "abc";

        // Act
        var result = await _api.CreateEmptyOutline(name, author, invalidTemplateIndex);

        // Assert
        Assert.IsFalse(result.IsSuccess, "Result should be a failure for an invalid template index.");
        Assert.IsTrue(result.ErrorMessage.Contains("is not a valid template index"),
            "Error message should indicate the template index is invalid.");
    }

    [TestMethod]
    public async Task CreateEmptyOutlineWithValidTemplate()
    {
        // Arrange
        var name = "Test Outline";
        var author = "Test Author";
        var validTemplateIndex = "0";

        // Act
        var result = await _api.CreateEmptyOutline(name, author, validTemplateIndex);

        // Assert
        Assert.IsTrue(result.IsSuccess, "Result should succeed for a valid template index.");
        Assert.IsNotNull(result.Payload, "Payload should not be null.");
        Assert.IsTrue(result.Payload.Count > 0, "Payload should contain one or more GUIDs.");
        foreach (var guid in result.Payload)
        {
            Assert.AreNotEqual(Guid.Empty, guid, "Each GUID in the payload should be non-empty.");
        }
    }

    [TestMethod]
    public async Task WriteOutline_WithoutModel_ReturnsFailure()
    {
        // Arrange
        // Do not create a model before writing.
        var filePath = Path.Combine(App.InputDir, "NoModel.stbx");

        // Act
        var result = await _api.WriteOutline(filePath);

        // Assert
        Assert.IsFalse(result.IsSuccess, "WriteOutline should fail if no model is available.");
        Assert.IsTrue(result.ErrorMessage.Contains("Deserialized StoryModel is null"),
            "Error message should indicate a missing model.");
    }

    [TestMethod]
    public async Task WriteOutline_WithModel_WritesFileSuccessfully()
    {
        // Arrange
        var outlineName = "Test Outline";
        var author = "Test Author";
        var createResult = await _api.CreateEmptyOutline(outlineName, author, "0");
        Assert.IsTrue(createResult.IsSuccess, "Model creation should succeed.");
        var filePath = Path.Combine(App.InputDir, "Outline.stbx");

        // Act
        var writeResult = await _api.WriteOutline(filePath);

        // Assert
        Assert.IsTrue(writeResult.IsSuccess, "WriteOutline should succeed when a model is available.");
        Assert.IsTrue(File.Exists(filePath), "The outline file should exist after writing.");
        var fileContent = File.ReadAllText(filePath);
        Assert.IsTrue(fileContent.Contains(outlineName), "The file content should include the outline name.");
    }

    [TestMethod]
    public void GetAllElements_WithoutModel_ReturnsFailure()
    {
        // Act
        var result = _api.GetAllElements();

        // Assert
        Assert.IsFalse(result.IsSuccess, "GetAllElements should fail without a model");
        Assert.IsNull(result.Payload, "Payload should be null");
        Assert.AreEqual("No StoryModel available. Create a model first.", result.ErrorMessage);
    }

    [TestMethod]
    public async Task GetAllElements_WithModel_ReturnsElementsCollection()
    {
        // Arrange
        var outlineName = "Test Outline";
        var author = "Test Author";
        var createResult = await _api.CreateEmptyOutline(outlineName, author, "0");
        Assert.IsTrue(createResult.IsSuccess, "Model creation should succeed.");

        // Act
        var result = _api.GetAllElements();

        // Assert
        Assert.IsTrue(result.IsSuccess, "GetAllElements should succeed");
        Assert.IsNotNull(result.Payload, "The returned collection should not be null.");
        Assert.IsTrue(result.Payload.Count > 0, "There should be at least one StoryElement in the collection.");
    }

    // This test has been removed because DeleteStoryElement is now implemented

    [TestMethod]
    public async Task UpdateStoryElement_UpdatesElementNameSuccessfully()
    {
        // Arrange
        var originalName = "Original Outline";
        var updatedName = "Updated Outline";
        var author = "Test Author";
        var createResult = await _api.CreateEmptyOutline(originalName, author, "0");
        Assert.IsTrue(createResult.IsSuccess, "Model creation should succeed.");
        var elementGuid = createResult.Payload.First();

        // Retrieve current element JSON.
        var getResult = _api.GetElement(elementGuid);
        Assert.IsTrue(getResult.IsSuccess, "GetElement should succeed");
        var elementJson = (string)getResult.Payload;
        // Deserialize to get element type information.
        var element = JsonSerializer.Deserialize<StoryElement>(elementJson);
        // Create updated JSON with the same GUID and type but a new Name.
        var updatedJson = JsonSerializer.Serialize(new
        {
            GUID = element.Uuid,
            Name = updatedName,
            Type = element.ElementType.ToString()
        });

        // Act
        var updateResult = _api.UpdateStoryElement(updatedJson, elementGuid);
        Assert.IsTrue(updateResult.IsSuccess, "UpdateStoryElement should succeed");

        // Assert: Retrieve element again and verify its name.
        var getUpdatedResult = _api.GetElement(elementGuid);
        Assert.IsTrue(getUpdatedResult.IsSuccess, "GetElement should succeed");
        var updatedElementJson = (string)getUpdatedResult.Payload;
        var updatedElement = JsonSerializer.Deserialize<StoryElement>(updatedElementJson);
        Assert.AreEqual(updatedName, updatedElement.Name, "The element's name should have been updated.");
    }

    [TestMethod]
    public async Task UpdateElementProperties_UpdatesElementNameSuccessfully()
    {
        // Arrange
        var originalName = "Original Outline";
        var updatedName = "Updated via Properties";
        var author = "Test Author";
        var createResult = await _api.CreateEmptyOutline(originalName, author, "0");
        Assert.IsTrue(createResult.IsSuccess, "Model creation should succeed.");
        var elementGuid = createResult.Payload.First();

        // Act: Update the "Name" property using the dictionary wrapper.
        var propertiesToUpdate = new Dictionary<string, object>
        {
            { "Name", updatedName }
        };
        var updateResult = _api.UpdateElementProperties(elementGuid, propertiesToUpdate);
        Assert.IsTrue(updateResult.IsSuccess, "UpdateElementProperties should succeed");

        // Assert: Verify that the element's name is updated.
        var getResult = _api.GetElement(elementGuid);
        Assert.IsTrue(getResult.IsSuccess, "GetElement should succeed");
        var updatedElementJson = (string)getResult.Payload;
        var updatedElement = JsonSerializer.Deserialize<StoryElement>(updatedElementJson);
        Assert.AreEqual(updatedName, updatedElement.Name,
            "The element's name should be updated via UpdateElementProperties.");
    }

    [TestMethod]
    public async Task GetElement_ReturnsSerializedElement()
    {
        // Arrange
        var outlineName = "Test Outline";
        var author = "Test Author";
        var createResult = await _api.CreateEmptyOutline(outlineName, author, "0");
        Assert.IsTrue(createResult.IsSuccess, "Model creation should succeed.");
        var elementGuid = createResult.Payload.First();

        // Act
        var getResult = _api.GetElement(elementGuid);

        // Assert
        Assert.IsTrue(getResult.IsSuccess, "GetElement should succeed");
        Assert.IsNotNull(getResult.Payload, "GetElement should return a non-null payload");
        var elementJson = getResult.Payload as string;
        Assert.IsNotNull(elementJson, "GetElement should return a JSON string.");
        Assert.IsTrue(elementJson.Contains(outlineName), "The returned JSON should include the element's name.");
        Assert.IsTrue(elementJson.Contains(elementGuid.ToString()),
            "The returned JSON should include the element's GUID.");
    }

    [TestMethod]
    public async Task AddInvalidParent()
    {
        // Arrange
        // Provide an invalid GUID string as parent.
        var invalidParentGuid = "not-a-guid";

        //open file first
        var file = Path.Combine(App.InputDir, "AddElement.stbx");
        Assert.IsTrue(File.Exists(file));
        await _api.OpenOutline(file);

        // Act
        var addResult = _api.AddElement(StoryItemType.Section, invalidParentGuid, "Added Section");

        // Assert
        Assert.IsFalse(addResult.IsSuccess, "AddElement should fail with an invalid parent GUID.");
        Assert.IsTrue(addResult.ErrorMessage.Contains("Invalid parent GUID"),
            "Error message should indicate invalid parent GUID.");
    }

    [TestMethod]
    public async Task AddElementWithValidParent()
    {
        // Arrange
        var outlineName = "Test Outline";
        var author = "Test Author";
        var createResult = await _api.CreateEmptyOutline(outlineName, author, "0");
        Assert.IsTrue(createResult.IsSuccess, "Model creation should succeed.");
        // Use one of the existing element GUIDs as the parent.
        var parentGuid = createResult.Payload.First();

        // Act
        var addResult = _api.AddElement(StoryItemType.Folder, parentGuid.ToString(), "Added Section");

        // Assert
        Assert.IsTrue(addResult.IsSuccess, "AddElement should succeed with a valid parent.");
        Assert.IsNotNull(addResult.Payload, "The payload should not be null for a successfully added element.");

        // Optionally verify that the added element's type is Section.
        var newElement = _api.CurrentModel.StoryElements.StoryElementGuids[addResult.Payload];
        Assert.AreEqual(StoryItemType.Folder, newElement.ElementType, "The new element should be of type Section.");

        var elementGuid = createResult.Payload.First();

        // Act: Update the "Name" property using the dictionary wrapper.
        var propertiesToUpdate = new Dictionary<string, object>
        {
            { "Name", "Renamed Section" }
        };
        var updateResult = _api.UpdateElementProperties(elementGuid, propertiesToUpdate);
        Assert.IsTrue(updateResult.IsSuccess, "UpdateElementProperties should succeed");

        // Assert: Verify that the element's name is updated.
        var getResult = _api.GetElement(elementGuid);
        Assert.IsTrue(getResult.IsSuccess, "GetElement should succeed");
        var updatedElementJson = (string)getResult.Payload;
        var updatedElement = JsonSerializer.Deserialize<StoryElement>(updatedElementJson);
        Assert.AreEqual("Renamed Section", updatedElement.Name,
            "The element's name should be updated via UpdateElementProperties.");
    }

    [TestMethod]
    public async Task UpdateElementProperty()
    {
        // Arrange
        var originalName = "Original Outline";
        var updatedName = "Updated via Property Method";
        var author = "Test Author";
        var createResult = await _api.CreateEmptyOutline(originalName, author, "0");
        Assert.IsTrue(createResult.IsSuccess, "Model creation should succeed.");
        var elementGuid = createResult.Payload.First();

        // Act
        var updateResult = _api.UpdateElementProperty(elementGuid, "Name", updatedName);

        // Assert
        Assert.IsTrue(updateResult.IsSuccess, "UpdateElementProperty should succeed.");
        Assert.AreEqual(updatedName, updateResult.Payload.Name, "The element's name should be updated.");
    }

    [TestMethod]
    public async Task OpenOutlineWithInvalidPath()
    {
        // Arrange
        var invalidPath = ""; // empty path

        // Act
        var result = await _api.OpenOutline(invalidPath);

        // Assert
        Assert.IsFalse(result.IsSuccess, "OpenOutline should fail with an invalid path.");
        Assert.IsTrue(result.ErrorMessage.Contains("Invalid path"), "Error message should indicate an invalid path.");
    }

    [TestMethod]
    public async Task OpenOutline_ValidFile_OpensModelSuccessfully()
    {
        // Arrange: Create and write a model file using WriteOutline.
        var outlineName = "Test Outline";
        var author = "Test Author";
        var createResult = await _api.CreateEmptyOutline(outlineName, author, "0");
        Assert.IsTrue(createResult.IsSuccess, "Model creation should succeed.");
        var filePath = Path.Combine(App.InputDir, "OpenedOutline.stbx");
        var writeResult = await _api.WriteOutline(filePath);
        Assert.IsTrue(writeResult.IsSuccess, "WriteOutline should succeed.");

        // Act: Create a new API instance and open the written file.
        var newApi = new SemanticKernelApi(Ioc.Default.GetRequiredService<OutlineService>(), Ioc.Default.GetRequiredService<ListData>(), Ioc.Default.GetRequiredService<ControlData>(), Ioc.Default.GetRequiredService<ToolsData>());
        var openResult = await newApi.OpenOutline(filePath);

        // Assert
        Assert.IsTrue(openResult.IsSuccess, "OpenOutline should succeed with a valid file.");
        Assert.IsTrue(openResult.Payload, "The payload should indicate a successful open operation.");
    }

    [TestMethod]
    public async Task DeleteElementTest()
    {
        // Arrange
        var outlineName = "Test Outline";
        var author = "Test Author";
        var createResult = await _api.CreateEmptyOutline(outlineName, author, "0");
        Assert.IsTrue(createResult.IsSuccess, "Model creation should succeed.");

        // Add a deletable element (character)
        var overviewGuid = _api.CurrentModel.ExplorerView.First().Uuid;
        var addResult = _api.AddElement(StoryItemType.Character, overviewGuid.ToString(), "Test Character");
        Assert.IsTrue(addResult.IsSuccess, "Adding character should succeed.");
        var elementGuid = addResult.Payload;

        // Act
        // Assume deletion from ExplorerView.
        var deleteResult = await _api.DeleteElement(elementGuid);

        // Assert
        Assert.IsTrue(deleteResult.IsSuccess, "DeleteElement should succeed.");
        Assert.IsTrue(deleteResult.Payload, "The payload should indicate that the deletion was successful.");
    }

    [TestMethod]
    public async Task AddCastMember()
    {
        // Arrange
        var outlineName = "Test Outline";
        var author = "Test Author";
        var createResult = await _api.CreateEmptyOutline(outlineName, author, "0");
        Assert.IsTrue(createResult.IsSuccess, "Model creation should succeed.");
        var parentGuid = createResult.Payload.First();

        // Add a Scene element.
        var sceneResult = _api.AddElement(StoryItemType.Scene, parentGuid.ToString(), "Added Scene");
        Assert.IsTrue(sceneResult.IsSuccess, "Scene element should be added successfully.");
        var sceneElement = _api.CurrentModel.StoryElements.StoryElementGuids[sceneResult.Payload];

        // Add a Character element.
        var characterGuid = _api.AddElement(StoryItemType.Character, parentGuid.ToString(), "Added Character").Payload;
        var characterResult = _api.CurrentModel.StoryElements.StoryElementGuids[characterGuid];
        Assert.IsTrue(characterResult.ElementType == StoryItemType.Character,
            "Character element should be added successfully.");

        // Act: Add the character as a cast member to the scene.
        var castResult = _api.AddCastMember(sceneElement.Uuid, characterResult.Uuid);

        // Assert
        Assert.IsTrue(castResult.IsSuccess, "AddCastMember should succeed.");
        Assert.IsTrue(castResult.Payload, "The payload should indicate success.");
    }

    [TestMethod]
    public async Task AddRelationship()
    {
        // Arrange
        var outlineName = "Test Outline";
        var author = "Test Author";
        var createResult = await _api.CreateEmptyOutline(outlineName, author, "0");
        Assert.IsTrue(createResult.IsSuccess, "Model creation should succeed.");
        var parentGuid = createResult.Payload.First();

        // Add two Character elements.
        var charResult1 = _api.AddElement(StoryItemType.Character, parentGuid.ToString(), "Character 1");
        var charResult2 = _api.AddElement(StoryItemType.Character, parentGuid.ToString(), "Character 2");
        Assert.IsTrue(charResult1.IsSuccess && charResult2.IsSuccess,
            "Both character elements should be added successfully.");

        var charElement1 = _api.CurrentModel.StoryElements.StoryElementGuids[charResult1.Payload];
        var charElement2 = _api.CurrentModel.StoryElements.StoryElementGuids[charResult2.Payload];

        // Act: Add a relationship between the two characters.
        var relationshipResult = _api.AddRelationship(charElement1.Uuid,
            charElement2.Uuid, "Friendship");

        // Assert
        Assert.IsTrue(relationshipResult.IsSuccess, "AddRelationship should succeed");

        // Also verify that calling with Guid.Empty returns failure.
        var failureResult = _api.AddRelationship(Guid.Empty, charElement2.Uuid, "Invalid");
        Assert.IsFalse(failureResult.IsSuccess, "AddRelationship should fail with empty GUID");
        Assert.AreEqual("Source GUID cannot be empty", failureResult.ErrorMessage);
    }

    /// <summary>
    ///     Tests that SetCurrentModel correctly sets the CurrentModel property
    /// </summary>
    [TestMethod]
    public async Task SetCurrentModel_WithValidModel_SetsCurrentModel()
    {
        // Arrange
        var api = new SemanticKernelApi(Ioc.Default.GetRequiredService<OutlineService>(), Ioc.Default.GetRequiredService<ListData>(), Ioc.Default.GetRequiredService<ControlData>(), Ioc.Default.GetRequiredService<ToolsData>());

        // Create a test model
        var createResult = await api.CreateEmptyOutline("Test Story", "Test Author", "0");
        Assert.IsTrue(createResult.IsSuccess, "Model creation should succeed");

        // Get the created model
        var originalModel = api.CurrentModel;
        Assert.IsNotNull(originalModel, "Model should be created");

        // Create a second model to switch to
        var secondResult = await api.CreateEmptyOutline("Second Story", "Another Author", "1");
        Assert.IsTrue(secondResult.IsSuccess, "Second model creation should succeed");
        var secondModel = api.CurrentModel;

        // Act - Set back to the first model
        api.SetCurrentModel(originalModel);

        // Assert
        Assert.AreSame(originalModel, api.CurrentModel, "CurrentModel should be set to the original model");
        Assert.AreNotSame(secondModel, api.CurrentModel, "CurrentModel should not be the second model");
        var overview = api.CurrentModel.StoryElements.FirstOrDefault(e => e.ElementType == StoryItemType.StoryOverview);
        Assert.AreEqual("Test Story", overview?.Name);
    }

    /// <summary>
    ///     Tests that SetCurrentModel handles null model gracefully
    /// </summary>
    [TestMethod]
    public void SetCurrentModel_WithNullModel_SetsCurrentModelToNull()
    {
        // Arrange
        var api = new SemanticKernelApi(Ioc.Default.GetRequiredService<OutlineService>(), Ioc.Default.GetRequiredService<ListData>(), Ioc.Default.GetRequiredService<ControlData>(), Ioc.Default.GetRequiredService<ToolsData>());

        // Act
        api.SetCurrentModel(null);

        // Assert
        Assert.IsNull(api.CurrentModel, "CurrentModel should be null");
    }

    /// <summary>
    ///     Tests that API operations work correctly after SetCurrentModel
    /// </summary>
    [TestMethod]
    public async Task SetCurrentModel_AllowsOperationsOnNewModel()
    {
        // Arrange
        var api = new SemanticKernelApi(Ioc.Default.GetRequiredService<OutlineService>(), Ioc.Default.GetRequiredService<ListData>(), Ioc.Default.GetRequiredService<ControlData>(), Ioc.Default.GetRequiredService<ToolsData>());

        // Create first model
        var firstResult = await api.CreateEmptyOutline("First Story", "Author 1", "0");
        Assert.IsTrue(firstResult.IsSuccess);
        var firstModel = api.CurrentModel;
        var firstOverviewGuid = firstResult.Payload.First();

        // Add an element to the first model
        var firstElementResult =
            api.AddElement(StoryItemType.Character, firstOverviewGuid.ToString(), "First Character");
        Assert.IsTrue(firstElementResult.IsSuccess);

        // Create second model
        var secondResult = await api.CreateEmptyOutline("Second Story", "Author 2", "0");
        Assert.IsTrue(secondResult.IsSuccess);
        var secondModel = api.CurrentModel;
        var secondOverviewGuid = secondResult.Payload.First();

        // Add an element to the second model
        var secondElementResult = api.AddElement(StoryItemType.Scene, secondOverviewGuid.ToString(), "Second Scene");
        Assert.IsTrue(secondElementResult.IsSuccess);

        // Act - Switch back to first model
        api.SetCurrentModel(firstModel);

        // Add another element to verify we're working with the first model
        var thirdElementResult = api.AddElement(StoryItemType.Problem, firstOverviewGuid.ToString(), "First Problem");

        // Assert
        Assert.IsTrue(thirdElementResult.IsSuccess, "Should be able to add element after SetCurrentModel");
        Assert.AreEqual(5, firstModel.StoryElements.Count,
            "First model should have 5 elements (overview, trash, narrative folder, character, problem)");
        Assert.AreEqual(4, secondModel.StoryElements.Count,
            "Second model should still have 4 elements (overview, trash, narrative folder, scene)");

        // Verify the new element is in the first model
        var problemElement = firstModel.StoryElements.StoryElementGuids[thirdElementResult.Payload];
        Assert.IsNotNull(problemElement);
        Assert.AreEqual("First Problem", problemElement.Name);
        Assert.AreEqual(StoryItemType.Problem, problemElement.ElementType);
    }

    /// <summary>
    ///     Tests that DeleteStoryElement moves an element to trash
    /// </summary>
    [TestMethod]
    public async Task DeleteStoryElement_WithValidElement_MovesToTrash()
    {
        // Arrange
        var createResult = await _api.CreateEmptyOutline("Test Story", "Test Author", "0");
        Assert.IsTrue(createResult.IsSuccess);

        var overviewGuid = _api.CurrentModel.ExplorerView.First().Uuid;
        var addResult = _api.AddElement(StoryItemType.Character, overviewGuid.ToString(), "Test Character");
        Assert.IsTrue(addResult.IsSuccess);
        var characterGuid = addResult.Payload;

        // Act
        var deleteResult = _api.DeleteStoryElement(characterGuid.ToString());

        // Assert
        Assert.IsTrue(deleteResult.IsSuccess, "DeleteStoryElement should succeed");
        var trashNode = _api.CurrentModel.TrashView.First();
        Assert.IsTrue(trashNode.Children.Any(n => n.Uuid == characterGuid), "Character should be in trash");
        var overviewNode = _api.CurrentModel.ExplorerView.First();
        Assert.IsFalse(overviewNode.Children.Any(n => n.Uuid == characterGuid), "Character should not be in explorer");
    }

    /// <summary>
    ///     Tests that DeleteStoryElement throws exception for invalid UUID
    /// </summary>
    [TestMethod]
    public async Task DeleteStoryElement_WithInvalidUuid_ReturnsFailure()
    {
        // Arrange
        var createResult = await _api.CreateEmptyOutline("Test Story", "Test Author", "0");
        Assert.IsTrue(createResult.IsSuccess);

        // Act
        var result = _api.DeleteStoryElement("invalid-uuid");

        // Assert
        Assert.IsFalse(result.IsSuccess, "DeleteStoryElement should fail with invalid UUID");
        Assert.IsTrue(result.ErrorMessage.Contains("Invalid UUID"), "Error message should indicate invalid UUID");
    }

    /// <summary>
    ///     Tests that DeleteStoryElement throws exception when no model is loaded
    /// </summary>
    [TestMethod]
    public void DeleteStoryElement_WithNoModel_ReturnsFailure()
    {
        // Arrange
        var api = new SemanticKernelApi(Ioc.Default.GetRequiredService<OutlineService>(), Ioc.Default.GetRequiredService<ListData>(), Ioc.Default.GetRequiredService<ControlData>(), Ioc.Default.GetRequiredService<ToolsData>());

        // Act
        var result = api.DeleteStoryElement(Guid.NewGuid().ToString());

        // Assert
        Assert.IsFalse(result.IsSuccess, "DeleteStoryElement should fail with no model");
        Assert.AreEqual("No StoryModel available. Create a model first.", result.ErrorMessage);
    }

    /// <summary>
    ///     Tests that DeleteElement moves an element to trash using OperationResult
    /// </summary>
    [TestMethod]
    public async Task DeleteElement_WithValidElement_ReturnsSuccess()
    {
        // Arrange
        var createResult = await _api.CreateEmptyOutline("Test Story", "Test Author", "0");
        Assert.IsTrue(createResult.IsSuccess);

        var overviewGuid = _api.CurrentModel.ExplorerView.First().Uuid;
        var addResult = _api.AddElement(StoryItemType.Scene, overviewGuid.ToString(), "Test Scene");
        Assert.IsTrue(addResult.IsSuccess);
        var sceneGuid = addResult.Payload;

        // Act
        var deleteResult = await _api.DeleteElement(sceneGuid);

        // Assert
        Assert.IsTrue(deleteResult.IsSuccess, "DeleteElement should succeed");
        var trashNode = _api.CurrentModel.TrashView.First();
        Assert.IsTrue(trashNode.Children.Any(n => n.Uuid == sceneGuid), "Scene should be in trash");
    }

    /// <summary>
    ///     Tests that DeleteElement returns failure when no model is loaded
    /// </summary>
    [TestMethod]
    public async Task DeleteElement_WithNoModel_ReturnsFailure()
    {
        // Arrange
        var api = new SemanticKernelApi(Ioc.Default.GetRequiredService<OutlineService>(), Ioc.Default.GetRequiredService<ListData>(), Ioc.Default.GetRequiredService<ControlData>(), Ioc.Default.GetRequiredService<ToolsData>());

        // Act
        var result = await api.DeleteElement(Guid.NewGuid());

        // Assert
        Assert.IsFalse(result.IsSuccess, "DeleteElement should fail");
        Assert.AreEqual("No outline is opened", result.ErrorMessage);
    }

    /// <summary>
    ///     Tests that RestoreFromTrash restores an element from trash
    /// </summary>
    [TestMethod]
    public async Task RestoreFromTrash_WithValidElement_ReturnsSuccess()
    {
        // Arrange
        var createResult = await _api.CreateEmptyOutline("Test Story", "Test Author", "0");
        Assert.IsTrue(createResult.IsSuccess);

        var overviewGuid = _api.CurrentModel.ExplorerView.First().Uuid;
        var addResult = _api.AddElement(StoryItemType.Problem, overviewGuid.ToString(), "Test Problem");
        Assert.IsTrue(addResult.IsSuccess);
        var problemGuid = addResult.Payload;

        // Move to trash first
        await _api.DeleteElement(problemGuid);

        // Act
        var restoreResult = await _api.RestoreFromTrash(problemGuid);

        // Assert
        Assert.IsTrue(restoreResult.IsSuccess, "RestoreFromTrash should succeed");
        var overviewNode = _api.CurrentModel.ExplorerView.First();
        Assert.IsTrue(overviewNode.Children.Any(n => n.Uuid == problemGuid), "Problem should be back in explorer");
        var trashNode = _api.CurrentModel.TrashView.First();
        Assert.IsFalse(trashNode.Children.Any(n => n.Uuid == problemGuid), "Problem should not be in trash");
    }

    /// <summary>
    ///     Tests that RestoreFromTrash returns failure for element not in trash
    /// </summary>
    [TestMethod]
    public async Task RestoreFromTrash_WithElementNotInTrash_ReturnsFailure()
    {
        // Arrange
        var createResult = await _api.CreateEmptyOutline("Test Story", "Test Author", "0");
        Assert.IsTrue(createResult.IsSuccess);

        // Act
        var result = await _api.RestoreFromTrash(Guid.NewGuid());

        // Assert
        Assert.IsFalse(result.IsSuccess, "RestoreFromTrash should fail");
        Assert.AreEqual("Element not found in trash", result.ErrorMessage);
    }

    /// <summary>
    ///     Tests that RestoreFromTrash returns failure when no model is loaded
    /// </summary>
    [TestMethod]
    public async Task RestoreFromTrash_WithNoModel_ReturnsFailure()
    {
        // Arrange
        var api = new SemanticKernelApi(Ioc.Default.GetRequiredService<OutlineService>(), Ioc.Default.GetRequiredService<ListData>(), Ioc.Default.GetRequiredService<ControlData>(), Ioc.Default.GetRequiredService<ToolsData>());

        // Act
        var result = await api.RestoreFromTrash(Guid.NewGuid());

        // Assert
        Assert.IsFalse(result.IsSuccess, "RestoreFromTrash should fail");
        Assert.AreEqual("No outline is opened", result.ErrorMessage);
    }

    /// <summary>
    ///     Tests that EmptyTrash removes all items from trash
    /// </summary>
    [TestMethod]
    public async Task EmptyTrash_WithItemsInTrash_ReturnsSuccess()
    {
        // Arrange
        var createResult = await _api.CreateEmptyOutline("Test Story", "Test Author", "0");
        Assert.IsTrue(createResult.IsSuccess);

        var overviewGuid = _api.CurrentModel.ExplorerView.First().Uuid;
        var addResult1 = _api.AddElement(StoryItemType.Character, overviewGuid.ToString(), "Character 1");
        var addResult2 = _api.AddElement(StoryItemType.Scene, overviewGuid.ToString(), "Scene 1");
        Assert.IsTrue(addResult1.IsSuccess);
        Assert.IsTrue(addResult2.IsSuccess);

        // Move both to trash
        await _api.DeleteElement(addResult1.Payload);
        await _api.DeleteElement(addResult2.Payload);

        var trashNode = _api.CurrentModel.TrashView.First();
        Assert.AreEqual(2, trashNode.Children.Count, "Should have 2 items in trash");

        // Act
        var emptyResult = await _api.EmptyTrash();

        // Assert
        Assert.IsTrue(emptyResult.IsSuccess, "EmptyTrash should succeed");
        Assert.AreEqual(0, trashNode.Children.Count, "Trash should be empty");
    }

    /// <summary>
    ///     Tests that EmptyTrash succeeds even when trash is already empty
    /// </summary>
    [TestMethod]
    public async Task EmptyTrash_WithNoItemsInTrash_ReturnsSuccess()
    {
        // Arrange
        var createResult = await _api.CreateEmptyOutline("Test Story", "Test Author", "0");
        Assert.IsTrue(createResult.IsSuccess);

        // Act
        var result = await _api.EmptyTrash();

        // Assert
        Assert.IsTrue(result.IsSuccess, "EmptyTrash should succeed even when empty");
        var trashNode = _api.CurrentModel.TrashView.First();
        Assert.AreEqual(0, trashNode.Children.Count, "Trash should remain empty");
    }

    /// <summary>
    ///     Tests that EmptyTrash returns failure when no model is loaded
    /// </summary>
    [TestMethod]
    public async Task EmptyTrash_WithNoModel_ReturnsFailure()
    {
        // Arrange
        var api = new SemanticKernelApi(Ioc.Default.GetRequiredService<OutlineService>(), Ioc.Default.GetRequiredService<ListData>(), Ioc.Default.GetRequiredService<ControlData>(), Ioc.Default.GetRequiredService<ToolsData>());

        // Act
        var result = await api.EmptyTrash();

        // Assert
        Assert.IsFalse(result.IsSuccess, "EmptyTrash should fail");
        Assert.AreEqual("No outline is opened", result.ErrorMessage);
    }

    #region GetStoryElement Tests

    /// <summary>
    ///     Tests that GetStoryElement returns success with the correct element for a valid GUID
    /// </summary>
    [TestMethod]
    public async Task GetStoryElement_WithValidGuid_ReturnsSuccess()
    {
        // Arrange
        var createResult =
            await _api.CreateEmptyOutline("Test Story", "Test Author", "1"); // Use template 1 to get a character
        Assert.IsTrue(createResult.IsSuccess);

        // Get a character element from the model
        var character = _api.CurrentModel.StoryElements.FirstOrDefault(e => e.ElementType == StoryItemType.Character);
        Assert.IsNotNull(character, "Should have at least one character in the model");

        // Act
        var result = _api.GetStoryElement(character.Uuid);

        // Assert
        Assert.IsTrue(result.IsSuccess, "Should return success");
        Assert.IsNotNull(result.Payload, "Should return a story element");
        Assert.AreEqual(character.Uuid, result.Payload.Uuid, "Should return the element with matching GUID");
        Assert.AreEqual(character.ElementType, result.Payload.ElementType, "Should return element with correct type");
        Assert.AreEqual(character.Name, result.Payload.Name, "Should return element with correct name");
    }

    /// <summary>
    ///     Tests that GetStoryElement returns failure when no model is loaded
    /// </summary>
    [TestMethod]
    public void GetStoryElement_WithNoCurrentModel_ReturnsFailure()
    {
        // Arrange
        var api = new SemanticKernelApi(Ioc.Default.GetRequiredService<OutlineService>(), Ioc.Default.GetRequiredService<ListData>(), Ioc.Default.GetRequiredService<ControlData>(), Ioc.Default.GetRequiredService<ToolsData>());
        var someGuid = Guid.NewGuid();

        // Act
        var result = api.GetStoryElement(someGuid);

        // Assert
        Assert.IsFalse(result.IsSuccess, "Should return failure");
        Assert.IsNull(result.Payload, "Payload should be null");
        Assert.AreEqual("No StoryModel available. Create a model first.", result.ErrorMessage);
    }

    /// <summary>
    ///     Tests that GetStoryElement returns failure for empty GUID
    /// </summary>
    [TestMethod]
    public async Task GetStoryElement_WithEmptyGuid_ReturnsFailure()
    {
        // Arrange
        var createResult = await _api.CreateEmptyOutline("Test Story", "Test Author", "0");
        Assert.IsTrue(createResult.IsSuccess);

        // Act
        var result = _api.GetStoryElement(Guid.Empty);

        // Assert
        Assert.IsFalse(result.IsSuccess, "Should return failure");
        Assert.IsNull(result.Payload, "Payload should be null");
        Assert.AreEqual("GUID cannot be empty", result.ErrorMessage);
    }

    /// <summary>
    ///     Tests that GetStoryElement returns failure for non-existent GUID
    /// </summary>
    [TestMethod]
    public async Task GetStoryElement_WithNonExistentGuid_ReturnsFailure()
    {
        // Arrange
        var createResult = await _api.CreateEmptyOutline("Test Story", "Test Author", "0");
        Assert.IsTrue(createResult.IsSuccess);
        var nonExistentGuid = Guid.NewGuid();

        // Act
        var result = _api.GetStoryElement(nonExistentGuid);

        // Assert
        Assert.IsFalse(result.IsSuccess, "Should return failure");
        Assert.IsNull(result.Payload, "Payload should be null");
        Assert.AreEqual("Element not found", result.ErrorMessage);
    }

    #endregion

    #region Search Methods Tests

    [TestMethod]
    public void SearchForText_WithNoModel_ReturnsFailure()
    {
        // Arrange
        var api = new SemanticKernelApi(Ioc.Default.GetRequiredService<OutlineService>(), Ioc.Default.GetRequiredService<ListData>(), Ioc.Default.GetRequiredService<ControlData>(), Ioc.Default.GetRequiredService<ToolsData>());

        // Act
        var result = api.SearchForText("test");

        // Assert
        Assert.IsFalse(result.IsSuccess, "SearchForText should fail with no model");
        Assert.IsNull(result.Payload, "Payload should be null");
        Assert.AreEqual("No outline is opened", result.ErrorMessage);
    }

    [TestMethod]
    public async Task SearchForText_WithEmptyText_ReturnsFailure()
    {
        // Arrange
        var createResult = await _api.CreateEmptyOutline("Test Story", "Test Author", "0");
        Assert.IsTrue(createResult.IsSuccess);

        // Act
        var result = _api.SearchForText("");

        // Assert
        Assert.IsFalse(result.IsSuccess, "SearchForText should fail with empty text");
        Assert.IsNull(result.Payload, "Payload should be null");
        Assert.AreEqual("Search text cannot be empty", result.ErrorMessage);
    }

    [TestMethod]
    public async Task SearchForText_WithNullText_ReturnsFailure()
    {
        // Arrange
        var createResult = await _api.CreateEmptyOutline("Test Story", "Test Author", "0");
        Assert.IsTrue(createResult.IsSuccess);

        // Act
        var result = _api.SearchForText(null);

        // Assert
        Assert.IsFalse(result.IsSuccess, "SearchForText should fail with null text");
        Assert.IsNull(result.Payload, "Payload should be null");
        Assert.AreEqual("Search text cannot be empty", result.ErrorMessage);
    }

    [TestMethod]
    public async Task SearchForText_WithValidText_ReturnsFormattedResults()
    {
        // Arrange
        var createResult = await _api.CreateEmptyOutline("Test Story", "Test Author", "0");
        Assert.IsTrue(createResult.IsSuccess);

        var overviewGuid = _api.CurrentModel.ExplorerView.First().Uuid;
        var addCharResult = _api.AddElement(StoryItemType.Character, overviewGuid.ToString(), "Hero Character");
        Assert.IsTrue(addCharResult.IsSuccess);

        var addSceneResult = _api.AddElement(StoryItemType.Scene, overviewGuid.ToString(), "Hero's Journey");
        Assert.IsTrue(addSceneResult.IsSuccess);

        // Act
        var result = _api.SearchForText("Hero");

        // Assert
        Assert.IsTrue(result.IsSuccess, "SearchForText should succeed");
        Assert.IsNotNull(result.Payload, "Payload should not be null");
        Assert.AreEqual(2, result.Payload.Count, "Should find 2 elements containing 'Hero'");

        // Verify the format of returned data
        foreach (var item in result.Payload)
        {
            Assert.IsTrue(item.ContainsKey("Guid"), "Each result should have a Guid");
            Assert.IsTrue(item.ContainsKey("Name"), "Each result should have a Name");
            Assert.IsTrue(item.ContainsKey("Type"), "Each result should have a Type");

            var name = item["Name"].ToString();
            Assert.IsTrue(name.Contains("Hero"), $"Name '{name}' should contain 'Hero'");
        }
    }

    [TestMethod]
    public async Task SearchForText_CaseInsensitive_ReturnsMatches()
    {
        // Arrange
        var createResult = await _api.CreateEmptyOutline("Test Story", "Test Author", "0");
        Assert.IsTrue(createResult.IsSuccess);

        var overviewGuid = _api.CurrentModel.ExplorerView.First().Uuid;
        var addResult = _api.AddElement(StoryItemType.Character, overviewGuid.ToString(), "HERO CHARACTER");
        Assert.IsTrue(addResult.IsSuccess);

        // Act
        var result = _api.SearchForText("hero");

        // Assert
        Assert.IsTrue(result.IsSuccess, "SearchForText should succeed");
        Assert.IsNotNull(result.Payload, "Payload should not be null");
        Assert.AreEqual(1, result.Payload.Count, "Should find 1 element (case-insensitive)");
        Assert.AreEqual("HERO CHARACTER", result.Payload[0]["Name"].ToString());
    }

    [TestMethod]
    public void SearchForReferences_WithNoModel_ReturnsFailure()
    {
        // Arrange
        var api = new SemanticKernelApi(Ioc.Default.GetRequiredService<OutlineService>(), Ioc.Default.GetRequiredService<ListData>(), Ioc.Default.GetRequiredService<ControlData>(), Ioc.Default.GetRequiredService<ToolsData>());

        // Act
        var result = api.SearchForReferences(Guid.NewGuid());

        // Assert
        Assert.IsFalse(result.IsSuccess, "SearchForReferences should fail with no model");
        Assert.IsNull(result.Payload, "Payload should be null");
        Assert.AreEqual("No outline is opened", result.ErrorMessage);
    }

    [TestMethod]
    public async Task SearchForReferences_WithEmptyGuid_ReturnsFailure()
    {
        // Arrange
        var createResult = await _api.CreateEmptyOutline("Test Story", "Test Author", "0");
        Assert.IsTrue(createResult.IsSuccess);

        // Act
        var result = _api.SearchForReferences(Guid.Empty);

        // Assert
        Assert.IsFalse(result.IsSuccess, "SearchForReferences should fail with empty GUID");
        Assert.IsNull(result.Payload, "Payload should be null");
        Assert.AreEqual("Target UUID cannot be empty", result.ErrorMessage);
    }

    [TestMethod]
    public async Task SearchForReferences_WithNoReferences_ReturnsEmptyList()
    {
        // Arrange
        var createResult = await _api.CreateEmptyOutline("Test Story", "Test Author", "0");
        Assert.IsTrue(createResult.IsSuccess);
        var randomUuid = Guid.NewGuid();

        // Act
        var result = _api.SearchForReferences(randomUuid);

        // Assert
        Assert.IsTrue(result.IsSuccess, "SearchForReferences should succeed even with no matches");
        Assert.IsNotNull(result.Payload, "Payload should not be null");
        Assert.AreEqual(0, result.Payload.Count, "Should return empty list for UUID with no references");
    }

    [TestMethod]
    public async Task SearchForReferences_WithValidReferences_ReturnsFormattedResults()
    {
        // Arrange
        var createResult = await _api.CreateEmptyOutline("Test Story", "Test Author", "0");
        Assert.IsTrue(createResult.IsSuccess);

        var overviewGuid = _api.CurrentModel.ExplorerView.First().Uuid;

        // Add a character
        var addCharResult = _api.AddElement(StoryItemType.Character, overviewGuid.ToString(), "Hero");
        Assert.IsTrue(addCharResult.IsSuccess);
        var characterGuid = addCharResult.Payload;

        // Add a scene that references the character
        var addSceneResult = _api.AddElement(StoryItemType.Scene, overviewGuid.ToString(), "Battle Scene");
        Assert.IsTrue(addSceneResult.IsSuccess);
        var sceneGuid = addSceneResult.Payload;

        // Add the character to the scene's cast
        var sceneElement = _api.CurrentModel.StoryElements.First(e => e.Uuid == sceneGuid);
        ((SceneModel)sceneElement).CastMembers.Add(characterGuid);

        // Act
        var result = _api.SearchForReferences(characterGuid);

        // Assert
        Assert.IsTrue(result.IsSuccess, "SearchForReferences should succeed");
        Assert.IsNotNull(result.Payload, "Payload should not be null");
        Assert.AreEqual(1, result.Payload.Count, "Should find 1 element referencing the character");

        var reference = result.Payload[0];
        Assert.IsTrue(reference.ContainsKey("Guid"), "Result should have a Guid");
        Assert.IsTrue(reference.ContainsKey("Name"), "Result should have a Name");
        Assert.IsTrue(reference.ContainsKey("Type"), "Result should have a Type");
        Assert.AreEqual("Battle Scene", reference["Name"].ToString());
        Assert.AreEqual("Scene", reference["Type"].ToString());
    }

    [TestMethod]
    public void RemoveReferences_WithNoModel_ReturnsFailure()
    {
        // Arrange
        var api = new SemanticKernelApi(Ioc.Default.GetRequiredService<OutlineService>(), Ioc.Default.GetRequiredService<ListData>(), Ioc.Default.GetRequiredService<ControlData>(), Ioc.Default.GetRequiredService<ToolsData>());

        // Act
        var result = api.RemoveReferences(Guid.NewGuid());

        // Assert
        Assert.IsFalse(result.IsSuccess, "RemoveReferences should fail with no model");
        Assert.AreEqual(0, result.Payload, "Payload should be 0");
        Assert.AreEqual("No outline is opened", result.ErrorMessage);
    }

    [TestMethod]
    public async Task RemoveReferences_WithEmptyGuid_ReturnsFailure()
    {
        // Arrange
        var createResult = await _api.CreateEmptyOutline("Test Story", "Test Author", "0");
        Assert.IsTrue(createResult.IsSuccess);

        // Act
        var result = _api.RemoveReferences(Guid.Empty);

        // Assert
        Assert.IsFalse(result.IsSuccess, "RemoveReferences should fail with empty GUID");
        Assert.AreEqual(0, result.Payload, "Payload should be 0");
        Assert.AreEqual("Target UUID cannot be empty", result.ErrorMessage);
    }

    [TestMethod]
    public async Task RemoveReferences_WithNoReferences_ReturnsZero()
    {
        // Arrange
        var createResult = await _api.CreateEmptyOutline("Test Story", "Test Author", "0");
        Assert.IsTrue(createResult.IsSuccess);
        var randomUuid = Guid.NewGuid();

        // Act
        var result = _api.RemoveReferences(randomUuid);

        // Assert
        Assert.IsTrue(result.IsSuccess, "RemoveReferences should succeed even with no matches");
        Assert.AreEqual(0, result.Payload, "Should return 0 for UUID with no references");
    }

    [TestMethod]
    public async Task RemoveReferences_WithValidReferences_ReturnsAffectedCount()
    {
        // Arrange
        var createResult = await _api.CreateEmptyOutline("Test Story", "Test Author", "0");
        Assert.IsTrue(createResult.IsSuccess);

        var overviewGuid = _api.CurrentModel.ExplorerView.First().Uuid;

        // Add a character
        var addCharResult = _api.AddElement(StoryItemType.Character, overviewGuid.ToString(), "Hero");
        Assert.IsTrue(addCharResult.IsSuccess);
        var characterGuid = addCharResult.Payload;

        // Add two scenes that reference the character
        var addScene1Result = _api.AddElement(StoryItemType.Scene, overviewGuid.ToString(), "Scene 1");
        Assert.IsTrue(addScene1Result.IsSuccess);
        var scene1Guid = addScene1Result.Payload;

        var addScene2Result = _api.AddElement(StoryItemType.Scene, overviewGuid.ToString(), "Scene 2");
        Assert.IsTrue(addScene2Result.IsSuccess);
        var scene2Guid = addScene2Result.Payload;

        // Add the character to both scenes' cast
        var scene1Element = _api.CurrentModel.StoryElements.First(e => e.Uuid == scene1Guid);
        ((SceneModel)scene1Element).CastMembers.Add(characterGuid);

        var scene2Element = _api.CurrentModel.StoryElements.First(e => e.Uuid == scene2Guid);
        ((SceneModel)scene2Element).CastMembers.Add(characterGuid);

        // Act
        var result = _api.RemoveReferences(characterGuid);

        // Assert
        Assert.IsTrue(result.IsSuccess, "RemoveReferences should succeed");
        Assert.AreEqual(2, result.Payload, "Should return 2 affected elements");

        // Verify references were actually removed
        Assert.IsFalse(((SceneModel)scene1Element).CastMembers.Contains(characterGuid),
            "Character should be removed from scene 1 cast");
        Assert.IsFalse(((SceneModel)scene2Element).CastMembers.Contains(characterGuid),
            "Character should be removed from scene 2 cast");
    }

    [TestMethod]
    public void SearchInSubtree_WithNoModel_ReturnsFailure()
    {
        // Arrange
        var api = new SemanticKernelApi(Ioc.Default.GetRequiredService<OutlineService>(), Ioc.Default.GetRequiredService<ListData>(), Ioc.Default.GetRequiredService<ControlData>(), Ioc.Default.GetRequiredService<ToolsData>());

        // Act
        var result = api.SearchInSubtree(Guid.NewGuid(), "test");

        // Assert
        Assert.IsFalse(result.IsSuccess, "SearchInSubtree should fail with no model");
        Assert.IsNull(result.Payload, "Payload should be null");
        Assert.AreEqual("No outline is opened", result.ErrorMessage);
    }

    [TestMethod]
    public async Task SearchInSubtree_WithEmptyRootGuid_ReturnsFailure()
    {
        // Arrange
        var createResult = await _api.CreateEmptyOutline("Test Story", "Test Author", "0");
        Assert.IsTrue(createResult.IsSuccess);

        // Act
        var result = _api.SearchInSubtree(Guid.Empty, "test");

        // Assert
        Assert.IsFalse(result.IsSuccess, "SearchInSubtree should fail with empty root GUID");
        Assert.IsNull(result.Payload, "Payload should be null");
        Assert.AreEqual("Root node GUID cannot be empty", result.ErrorMessage);
    }

    [TestMethod]
    public async Task SearchInSubtree_WithEmptySearchText_ReturnsFailure()
    {
        // Arrange
        var createResult = await _api.CreateEmptyOutline("Test Story", "Test Author", "0");
        Assert.IsTrue(createResult.IsSuccess);

        var overviewGuid = _api.CurrentModel.ExplorerView.First().Uuid;

        // Act
        var result = _api.SearchInSubtree(overviewGuid, "");

        // Assert
        Assert.IsFalse(result.IsSuccess, "SearchInSubtree should fail with empty search text");
        Assert.IsNull(result.Payload, "Payload should be null");
        Assert.AreEqual("Search text cannot be empty", result.ErrorMessage);
    }

    [TestMethod]
    public async Task SearchInSubtree_WithNonExistentRoot_ReturnsFailure()
    {
        // Arrange
        var createResult = await _api.CreateEmptyOutline("Test Story", "Test Author", "0");
        Assert.IsTrue(createResult.IsSuccess);

        var randomGuid = Guid.NewGuid();

        // Act
        var result = _api.SearchInSubtree(randomGuid, "test");

        // Assert
        Assert.IsFalse(result.IsSuccess, "SearchInSubtree should fail with non-existent root");
        Assert.IsNull(result.Payload, "Payload should be null");
        Assert.IsTrue(result.ErrorMessage.Contains("not found"),
            $"Error message should indicate element not found, but was: {result.ErrorMessage}");
    }

    [TestMethod]
    public async Task SearchInSubtree_WithValidRoot_ReturnsSubtreeMatchesOnly()
    {
        // Arrange
        var createResult = await _api.CreateEmptyOutline("Test Story", "Test Author", "0");
        Assert.IsTrue(createResult.IsSuccess);

        var overviewGuid = _api.CurrentModel.ExplorerView.First().Uuid;

        // Add a folder as subtree root
        var addFolderResult = _api.AddElement(StoryItemType.Folder, overviewGuid.ToString(), "Hero Folder");
        Assert.IsTrue(addFolderResult.IsSuccess);
        var folderGuid = addFolderResult.Payload;

        // Add items inside the folder (should be found)
        var addChar1Result = _api.AddElement(StoryItemType.Character, folderGuid.ToString(), "Hero Character");
        Assert.IsTrue(addChar1Result.IsSuccess);

        var addChar2Result = _api.AddElement(StoryItemType.Character, folderGuid.ToString(), "Sidekick");
        Assert.IsTrue(addChar2Result.IsSuccess);
        var sidekickGuid = addChar2Result.Payload;

        // Add notes to sidekick that contain "hero"
        var sidekickElement = _api.CurrentModel.StoryElements.First(e => e.Uuid == sidekickGuid);
        ((CharacterModel)sidekickElement).Notes = "Helps the hero";

        // Add item outside the folder (should NOT be found)
        var addChar3Result = _api.AddElement(StoryItemType.Character, overviewGuid.ToString(), "Another Hero");
        Assert.IsTrue(addChar3Result.IsSuccess);

        // Act
        var result = _api.SearchInSubtree(folderGuid, "hero");

        // Assert
        Assert.IsTrue(result.IsSuccess, "SearchInSubtree should succeed");
        Assert.IsNotNull(result.Payload, "Payload should not be null");
        Assert.AreEqual(3, result.Payload.Count, "Should find 3 elements (folder itself + 2 characters inside)");

        // Verify results are formatted correctly
        foreach (var item in result.Payload)
        {
            Assert.IsTrue(item.ContainsKey("Guid"), "Each result should have a Guid");
            Assert.IsTrue(item.ContainsKey("Name"), "Each result should have a Name");
            Assert.IsTrue(item.ContainsKey("Type"), "Each result should have a Type");
        }

        // Verify the correct items were found
        var names = result.Payload.Select(r => r["Name"].ToString()).ToList();
        Assert.IsTrue(names.Contains("Hero Folder"), "Should find the folder itself");
        Assert.IsTrue(names.Contains("Hero Character"), "Should find Hero Character inside folder");
        Assert.IsTrue(names.Contains("Sidekick"), "Should find Sidekick (has 'hero' in notes)");
        Assert.IsFalse(names.Contains("Another Hero"), "Should NOT find Another Hero (outside folder)");
    }

    #endregion

    #region GetExamples Tests (Lists API - Issue #1223)

    /// <summary>
    /// Tests that GetExamples returns list values for a valid property name
    /// </summary>
    [TestMethod]
    public void GetExamples_ValidProperty_ReturnsListValues()
    {
        // Arrange
        var propertyName = "Tone"; // Known list key from Lists.json

        // Act
        var result = _api.GetExamples(propertyName);

        // Assert
        Assert.IsTrue(result.IsSuccess, "GetExamples should succeed for valid property");
        Assert.IsNotNull(result.Payload, "Payload should not be null");
        Assert.IsTrue(result.Payload.Any(), "Should return at least one value");
        // Verify some known tone values exist
        var values = result.Payload.ToList();
        Assert.IsTrue(values.Contains("Angry") || values.Contains("Calm") || values.Contains("Cheerful"),
            "Should contain expected tone values");
    }

    /// <summary>
    /// Tests that GetExamples returns failure for an invalid property name
    /// </summary>
    [TestMethod]
    public void GetExamples_InvalidProperty_ReturnsFailure()
    {
        // Arrange
        var propertyName = "NonExistentProperty";

        // Act
        var result = _api.GetExamples(propertyName);

        // Assert
        Assert.IsFalse(result.IsSuccess, "GetExamples should fail for invalid property");
        Assert.IsNull(result.Payload, "Payload should be null on failure");
        Assert.AreEqual($"No list found for property '{propertyName}'", result.ErrorMessage);
    }

    #endregion

    #region Conflicts API Tests (Issue #1223)

    /// <summary>
    /// Tests that GetConflictCategories returns all categories
    /// </summary>
    [TestMethod]
    public void GetConflictCategories_WhenCalled_ReturnsAllCategories()
    {
        // Act
        var result = _api.GetConflictCategories();

        // Assert
        Assert.IsTrue(result.IsSuccess, "GetConflictCategories should succeed");
        Assert.IsNotNull(result.Payload, "Payload should not be null");
        var categories = result.Payload.ToList();
        Assert.IsTrue(categories.Count >= 8, "Should have at least 8 conflict categories");
        // Verify some known categories exist
        Assert.IsTrue(categories.Contains("Relationship"), "Should contain Relationship category");
        Assert.IsTrue(categories.Contains("Criminal activities"), "Should contain Criminal activities category");
    }

    /// <summary>
    /// Tests that GetConflictSubcategories returns subcategories for a valid category
    /// </summary>
    [TestMethod]
    public void GetConflictSubcategories_ValidCategory_ReturnsSubcategories()
    {
        // Arrange
        var category = "Relationship";

        // Act
        var result = _api.GetConflictSubcategories(category);

        // Assert
        Assert.IsTrue(result.IsSuccess, "GetConflictSubcategories should succeed for valid category");
        Assert.IsNotNull(result.Payload, "Payload should not be null");
        Assert.IsTrue(result.Payload.Any(), "Should return at least one subcategory");
    }

    /// <summary>
    /// Tests that GetConflictSubcategories returns failure for invalid category
    /// </summary>
    [TestMethod]
    public void GetConflictSubcategories_InvalidCategory_ReturnsFailure()
    {
        // Arrange
        var category = "NonExistentCategory";

        // Act
        var result = _api.GetConflictSubcategories(category);

        // Assert
        Assert.IsFalse(result.IsSuccess, "GetConflictSubcategories should fail for invalid category");
        Assert.IsNull(result.Payload, "Payload should be null on failure");
        Assert.AreEqual($"No conflict category '{category}' found", result.ErrorMessage);
    }

    /// <summary>
    /// Tests that GetConflictExamples returns examples for valid inputs
    /// </summary>
    [TestMethod]
    public void GetConflictExamples_ValidInputs_ReturnsExamples()
    {
        // Arrange - first get a valid subcategory
        var category = "Relationship";
        var subcatResult = _api.GetConflictSubcategories(category);
        Assert.IsTrue(subcatResult.IsSuccess, "Need valid subcategory for test");
        var subcategory = subcatResult.Payload.First();

        // Act
        var result = _api.GetConflictExamples(category, subcategory);

        // Assert
        Assert.IsTrue(result.IsSuccess, "GetConflictExamples should succeed for valid inputs");
        Assert.IsNotNull(result.Payload, "Payload should not be null");
        Assert.IsTrue(result.Payload.Any(), "Should return at least one example");
    }

    /// <summary>
    /// Tests that GetConflictExamples returns failure for invalid category
    /// </summary>
    [TestMethod]
    public void GetConflictExamples_InvalidCategory_ReturnsFailure()
    {
        // Arrange
        var category = "NonExistentCategory";
        var subcategory = "SomeSubcategory";

        // Act
        var result = _api.GetConflictExamples(category, subcategory);

        // Assert
        Assert.IsFalse(result.IsSuccess, "GetConflictExamples should fail for invalid category");
        Assert.AreEqual($"No conflict category '{category}' found", result.ErrorMessage);
    }

    /// <summary>
    /// Tests that GetConflictExamples returns failure for invalid subcategory
    /// </summary>
    [TestMethod]
    public void GetConflictExamples_InvalidSubcategory_ReturnsFailure()
    {
        // Arrange
        var category = "Relationship";
        var subcategory = "NonExistentSubcategory";

        // Act
        var result = _api.GetConflictExamples(category, subcategory);

        // Assert
        Assert.IsFalse(result.IsSuccess, "GetConflictExamples should fail for invalid subcategory");
        Assert.AreEqual($"No subcategory '{subcategory}' in category '{category}'", result.ErrorMessage);
    }

    #endregion

    #region Key Questions API Tests (Issue #1223)

    /// <summary>
    /// Tests that GetKeyQuestionElements returns all element types with key questions
    /// </summary>
    [TestMethod]
    public void GetKeyQuestionElements_WhenCalled_ReturnsElementTypes()
    {
        // Act
        var result = _api.GetKeyQuestionElements();

        // Assert
        Assert.IsTrue(result.IsSuccess, "GetKeyQuestionElements should succeed");
        Assert.IsNotNull(result.Payload, "Payload should not be null");
        var elements = result.Payload.ToList();
        Assert.IsTrue(elements.Count >= 1, "Should have at least one element type with key questions");
        // Verify some known element types exist
        Assert.IsTrue(elements.Contains("Character") || elements.Contains("Problem") || elements.Contains("Scene"),
            "Should contain common element types");
    }

    /// <summary>
    /// Tests that GetKeyQuestions returns questions for a valid element type
    /// </summary>
    [TestMethod]
    public void GetKeyQuestions_ValidElementType_ReturnsQuestions()
    {
        // Arrange - get a valid element type first
        var elementsResult = _api.GetKeyQuestionElements();
        Assert.IsTrue(elementsResult.IsSuccess, "Need valid element type for test");
        var elementType = elementsResult.Payload.First();

        // Act
        var result = _api.GetKeyQuestions(elementType);

        // Assert
        Assert.IsTrue(result.IsSuccess, "GetKeyQuestions should succeed for valid element type");
        Assert.IsNotNull(result.Payload, "Payload should not be null");
        var questions = result.Payload.ToList();
        Assert.IsTrue(questions.Count >= 1, "Should have at least one question");
        // Verify tuple structure
        var firstQuestion = questions.First();
        Assert.IsFalse(string.IsNullOrEmpty(firstQuestion.Topic), "Topic should not be empty");
        Assert.IsFalse(string.IsNullOrEmpty(firstQuestion.Question), "Question should not be empty");
    }

    /// <summary>
    /// Tests that GetKeyQuestions returns failure for invalid element type
    /// </summary>
    [TestMethod]
    public void GetKeyQuestions_InvalidElementType_ReturnsFailure()
    {
        // Arrange
        var elementType = "NonExistentElement";

        // Act
        var result = _api.GetKeyQuestions(elementType);

        // Assert
        Assert.IsFalse(result.IsSuccess, "GetKeyQuestions should fail for invalid element type");
        Assert.IsNull(result.Payload, "Payload should be null on failure");
        Assert.AreEqual($"No key questions for element type '{elementType}'", result.ErrorMessage);
    }

    #endregion

    #region Master Plots API Tests (Issue #1223)

    /// <summary>
    /// Tests that GetMasterPlotNames returns all master plot names
    /// </summary>
    [TestMethod]
    public void GetMasterPlotNames_WhenCalled_ReturnsPlotNames()
    {
        // Act
        var result = _api.GetMasterPlotNames();

        // Assert
        Assert.IsTrue(result.IsSuccess, "GetMasterPlotNames should succeed");
        Assert.IsNotNull(result.Payload, "Payload should not be null");
        var names = result.Payload.ToList();
        Assert.IsTrue(names.Count >= 1, "Should have at least one master plot");
        // Verify some known master plots exist (from Tobias's 20 Master Plots)
        Assert.IsTrue(names.Contains("Quest") || names.Contains("Adventure") || names.Contains("Pursuit"),
            "Should contain known master plot names");
    }

    /// <summary>
    /// Tests that GetMasterPlotNotes returns notes for a valid plot name
    /// </summary>
    [TestMethod]
    public void GetMasterPlotNotes_ValidName_ReturnsNotes()
    {
        // Arrange - get a valid plot name first
        var namesResult = _api.GetMasterPlotNames();
        Assert.IsTrue(namesResult.IsSuccess, "Need valid plot name for test");
        var plotName = namesResult.Payload.First();

        // Act
        var result = _api.GetMasterPlotNotes(plotName);

        // Assert
        Assert.IsTrue(result.IsSuccess, "GetMasterPlotNotes should succeed for valid name");
        Assert.IsNotNull(result.Payload, "Payload should not be null");
    }

    /// <summary>
    /// Tests that GetMasterPlotNotes returns failure for invalid plot name
    /// </summary>
    [TestMethod]
    public void GetMasterPlotNotes_InvalidName_ReturnsFailure()
    {
        // Arrange
        var plotName = "NonExistentPlot";

        // Act
        var result = _api.GetMasterPlotNotes(plotName);

        // Assert
        Assert.IsFalse(result.IsSuccess, "GetMasterPlotNotes should fail for invalid name");
        Assert.IsNull(result.Payload, "Payload should be null on failure");
        Assert.AreEqual($"No master plot '{plotName}' found", result.ErrorMessage);
    }

    #endregion

    #region Stock Scenes API Tests (Issue #1223)

    /// <summary>
    /// Tests that GetStockSceneCategories returns all categories
    /// </summary>
    [TestMethod]
    public void GetStockSceneCategories_WhenCalled_ReturnsCategories()
    {
        // Act
        var result = _api.GetStockSceneCategories();

        // Assert
        Assert.IsTrue(result.IsSuccess, "GetStockSceneCategories should succeed");
        Assert.IsNotNull(result.Payload, "Payload should not be null");
        var categories = result.Payload.ToList();
        Assert.IsTrue(categories.Count >= 1, "Should have at least one category");
    }

    /// <summary>
    /// Tests that GetStockScenes returns scenes for a valid category
    /// </summary>
    [TestMethod]
    public void GetStockScenes_ValidCategory_ReturnsScenes()
    {
        // Arrange - get a valid category first
        var categoriesResult = _api.GetStockSceneCategories();
        Assert.IsTrue(categoriesResult.IsSuccess, "Need valid category for test");
        var category = categoriesResult.Payload.First();

        // Act
        var result = _api.GetStockScenes(category);

        // Assert
        Assert.IsTrue(result.IsSuccess, "GetStockScenes should succeed for valid category");
        Assert.IsNotNull(result.Payload, "Payload should not be null");
    }

    /// <summary>
    /// Tests that GetStockScenes returns failure for invalid category
    /// </summary>
    [TestMethod]
    public void GetStockScenes_InvalidCategory_ReturnsFailure()
    {
        // Arrange
        var category = "NonExistentCategory";

        // Act
        var result = _api.GetStockScenes(category);

        // Assert
        Assert.IsFalse(result.IsSuccess, "GetStockScenes should fail for invalid category");
        Assert.IsNull(result.Payload, "Payload should be null on failure");
        Assert.AreEqual($"No stock scene category '{category}' found", result.ErrorMessage);
    }

    #endregion

    #region Beat Sheets API Tests (Issue #1223)

    // ===== Read Operations - Templates =====

    /// <summary>
    /// Tests that GetBeatSheetNames returns all beat sheet template names
    /// </summary>
    [TestMethod]
    public void GetBeatSheetNames_WhenCalled_ReturnsNames()
    {
        // Act
        var result = _api.GetBeatSheetNames();

        // Assert
        Assert.IsTrue(result.IsSuccess, "GetBeatSheetNames should succeed");
        Assert.IsNotNull(result.Payload, "Payload should not be null");
        var names = result.Payload.ToList();
        Assert.IsTrue(names.Count >= 1, "Should have at least one beat sheet");
    }

    /// <summary>
    /// Tests that GetBeatSheet returns description and beats for a valid template
    /// </summary>
    [TestMethod]
    public void GetBeatSheet_ValidName_ReturnsDescriptionAndBeats()
    {
        // Arrange
        var namesResult = _api.GetBeatSheetNames();
        Assert.IsTrue(namesResult.IsSuccess, "Need valid beat sheet name for test");
        var beatSheetName = namesResult.Payload.First();

        // Act
        var result = _api.GetBeatSheet(beatSheetName);

        // Assert
        Assert.IsTrue(result.IsSuccess, "GetBeatSheet should succeed for valid name");
        Assert.IsNotNull(result.Payload.Description, "Description should not be null");
        Assert.IsNotNull(result.Payload.Beats, "Beats should not be null");
        var beats = result.Payload.Beats.ToList();
        Assert.IsTrue(beats.Count >= 1, "Should have at least one beat");
    }

    /// <summary>
    /// Tests that GetBeatSheet returns failure for invalid template name
    /// </summary>
    [TestMethod]
    public void GetBeatSheet_InvalidName_ReturnsFailure()
    {
        // Act
        var result = _api.GetBeatSheet("NonExistentBeatSheet");

        // Assert
        Assert.IsFalse(result.IsSuccess, "GetBeatSheet should fail for invalid name");
        Assert.AreEqual("No beat sheet 'NonExistentBeatSheet' found", result.ErrorMessage);
    }

    // ===== Apply Template to Problem =====

    /// <summary>
    /// Tests that ApplyBeatSheetToProblem applies a template to a Problem
    /// </summary>
    [TestMethod]
    public async Task ApplyBeatSheetToProblem_ValidInputs_AppliesTemplate()
    {
        // Arrange - create model with Problem
        await _api.CreateEmptyOutline("Test Story", "Test Author", "0");
        var overviewGuid = _api.CurrentModel.ExplorerView.First().Uuid;
        var addProblemResult = _api.AddElement(StoryItemType.Problem, overviewGuid.ToString(), "Test Problem");
        Assert.IsTrue(addProblemResult.IsSuccess, "Problem creation should succeed");
        var problemGuid = addProblemResult.Payload;
        var beatSheetName = _api.GetBeatSheetNames().Payload.First();

        // Act
        var result = _api.ApplyBeatSheetToProblem(problemGuid, beatSheetName);

        // Assert
        Assert.IsTrue(result.IsSuccess, "ApplyBeatSheetToProblem should succeed");

        // Verify structure was applied
        var structureResult = _api.GetProblemStructure(problemGuid);
        Assert.IsTrue(structureResult.IsSuccess, "Should be able to get structure after applying");
        Assert.AreEqual(beatSheetName, structureResult.Payload.Title, "Structure title should match beat sheet name");
    }

    /// <summary>
    /// Tests that ApplyBeatSheetToProblem fails for invalid Problem GUID
    /// </summary>
    [TestMethod]
    public async Task ApplyBeatSheetToProblem_InvalidProblem_ReturnsFailure()
    {
        // Arrange
        await _api.CreateEmptyOutline("Test Story", "Test Author", "0");
        var invalidGuid = Guid.NewGuid();
        var beatSheetName = _api.GetBeatSheetNames().Payload.First();

        // Act
        var result = _api.ApplyBeatSheetToProblem(invalidGuid, beatSheetName);

        // Assert
        Assert.IsFalse(result.IsSuccess, "Should fail for invalid Problem GUID");
    }

    /// <summary>
    /// Tests that ApplyBeatSheetToProblem fails for invalid beat sheet name
    /// </summary>
    [TestMethod]
    public async Task ApplyBeatSheetToProblem_InvalidBeatSheet_ReturnsFailure()
    {
        // Arrange
        await _api.CreateEmptyOutline("Test Story", "Test Author", "0");
        var overviewGuid = _api.CurrentModel.ExplorerView.First().Uuid;
        var addProblemResult = _api.AddElement(StoryItemType.Problem, overviewGuid.ToString(), "Test Problem");
        var problemGuid = addProblemResult.Payload;

        // Act
        var result = _api.ApplyBeatSheetToProblem(problemGuid, "NonExistentBeatSheet");

        // Assert
        Assert.IsFalse(result.IsSuccess, "Should fail for invalid beat sheet name");
    }

    // ===== Assign/Clear Element =====

    /// <summary>
    /// Tests that AssignElementToBeat assigns a Scene to a beat
    /// </summary>
    [TestMethod]
    public async Task AssignElementToBeat_ValidInputs_AssignsElement()
    {
        // Arrange - create model with Problem and Scene
        await _api.CreateEmptyOutline("Test Story", "Test Author", "0");
        var overviewGuid = _api.CurrentModel.ExplorerView.First().Uuid;
        var addProblemResult = _api.AddElement(StoryItemType.Problem, overviewGuid.ToString(), "Test Problem");
        var problemGuid = addProblemResult.Payload;
        var addSceneResult = _api.AddElement(StoryItemType.Scene, overviewGuid.ToString(), "Test Scene");
        var sceneGuid = addSceneResult.Payload;

        var beatSheetName = _api.GetBeatSheetNames().Payload.First();
        _api.ApplyBeatSheetToProblem(problemGuid, beatSheetName);

        // Act
        var result = _api.AssignElementToBeat(problemGuid, 0, sceneGuid);

        // Assert
        Assert.IsTrue(result.IsSuccess, "AssignElementToBeat should succeed");

        // Verify assignment
        var structureResult = _api.GetProblemStructure(problemGuid);
        var firstBeat = structureResult.Payload.Beats.First();
        Assert.AreEqual(sceneGuid, firstBeat.LinkedElement, "Beat should have assigned element");
    }

    /// <summary>
    /// Tests that ClearBeatAssignment clears an element assignment
    /// </summary>
    [TestMethod]
    public async Task ClearBeatAssignment_ValidInputs_ClearsAssignment()
    {
        // Arrange - create model, apply beat sheet, and assign element
        await _api.CreateEmptyOutline("Test Story", "Test Author", "0");
        var overviewGuid = _api.CurrentModel.ExplorerView.First().Uuid;
        var addProblemResult = _api.AddElement(StoryItemType.Problem, overviewGuid.ToString(), "Test Problem");
        var problemGuid = addProblemResult.Payload;
        var addSceneResult = _api.AddElement(StoryItemType.Scene, overviewGuid.ToString(), "Test Scene");
        var sceneGuid = addSceneResult.Payload;

        var beatSheetName = _api.GetBeatSheetNames().Payload.First();
        _api.ApplyBeatSheetToProblem(problemGuid, beatSheetName);
        _api.AssignElementToBeat(problemGuid, 0, sceneGuid);

        // Act
        var result = _api.ClearBeatAssignment(problemGuid, 0);

        // Assert
        Assert.IsTrue(result.IsSuccess, "ClearBeatAssignment should succeed");

        // Verify cleared
        var structureResult = _api.GetProblemStructure(problemGuid);
        var firstBeat = structureResult.Payload.Beats.First();
        Assert.IsNull(firstBeat.LinkedElement, "Beat should have no assigned element");
    }

    // ===== CRUD Operations =====

    /// <summary>
    /// Tests that CreateBeat adds a new beat to a Problem's structure
    /// </summary>
    [TestMethod]
    public async Task CreateBeat_ValidInputs_AddsBeat()
    {
        // Arrange - create model and apply beat sheet
        await _api.CreateEmptyOutline("Test Story", "Test Author", "0");
        var overviewGuid = _api.CurrentModel.ExplorerView.First().Uuid;
        var addProblemResult = _api.AddElement(StoryItemType.Problem, overviewGuid.ToString(), "Test Problem");
        var problemGuid = addProblemResult.Payload;

        var beatSheetName = _api.GetBeatSheetNames().Payload.First();
        _api.ApplyBeatSheetToProblem(problemGuid, beatSheetName);

        var initialCount = _api.GetProblemStructure(problemGuid).Payload.Beats.Count();

        // Act
        var result = _api.CreateBeat(problemGuid, "New Beat", "Beat description");

        // Assert
        Assert.IsTrue(result.IsSuccess, "CreateBeat should succeed");

        var newCount = _api.GetProblemStructure(problemGuid).Payload.Beats.Count();
        Assert.AreEqual(initialCount + 1, newCount, "Should have one more beat");
    }

    /// <summary>
    /// Tests that UpdateBeat modifies a beat's title and description
    /// </summary>
    [TestMethod]
    public async Task UpdateBeat_ValidInputs_UpdatesBeat()
    {
        // Arrange - create model and apply beat sheet
        await _api.CreateEmptyOutline("Test Story", "Test Author", "0");
        var overviewGuid = _api.CurrentModel.ExplorerView.First().Uuid;
        var addProblemResult = _api.AddElement(StoryItemType.Problem, overviewGuid.ToString(), "Test Problem");
        var problemGuid = addProblemResult.Payload;

        var beatSheetName = _api.GetBeatSheetNames().Payload.First();
        _api.ApplyBeatSheetToProblem(problemGuid, beatSheetName);

        // Act
        var result = _api.UpdateBeat(problemGuid, 0, "Updated Title", "Updated Description");

        // Assert
        Assert.IsTrue(result.IsSuccess, "UpdateBeat should succeed");

        var structureResult = _api.GetProblemStructure(problemGuid);
        var firstBeat = structureResult.Payload.Beats.First();
        Assert.AreEqual("Updated Title", firstBeat.BeatTitle, "Title should be updated");
        Assert.AreEqual("Updated Description", firstBeat.BeatDescription, "Description should be updated");
    }

    /// <summary>
    /// Tests that DeleteBeat removes a beat from a Problem's structure
    /// </summary>
    [TestMethod]
    public async Task DeleteBeat_ValidInputs_RemovesBeat()
    {
        // Arrange - create model and apply beat sheet
        await _api.CreateEmptyOutline("Test Story", "Test Author", "0");
        var overviewGuid = _api.CurrentModel.ExplorerView.First().Uuid;
        var addProblemResult = _api.AddElement(StoryItemType.Problem, overviewGuid.ToString(), "Test Problem");
        var problemGuid = addProblemResult.Payload;

        var beatSheetName = _api.GetBeatSheetNames().Payload.First();
        _api.ApplyBeatSheetToProblem(problemGuid, beatSheetName);

        var initialCount = _api.GetProblemStructure(problemGuid).Payload.Beats.Count();

        // Act
        var result = _api.DeleteBeat(problemGuid, 0);

        // Assert
        Assert.IsTrue(result.IsSuccess, "DeleteBeat should succeed");

        var newCount = _api.GetProblemStructure(problemGuid).Payload.Beats.Count();
        Assert.AreEqual(initialCount - 1, newCount, "Should have one fewer beat");
    }

    /// <summary>
    /// Tests that MoveBeat reorders beats in a Problem's structure
    /// </summary>
    [TestMethod]
    public async Task MoveBeat_ValidInputs_ReordersBeat()
    {
        // Arrange - create model and apply beat sheet
        await _api.CreateEmptyOutline("Test Story", "Test Author", "0");
        var overviewGuid = _api.CurrentModel.ExplorerView.First().Uuid;
        var addProblemResult = _api.AddElement(StoryItemType.Problem, overviewGuid.ToString(), "Test Problem");
        var problemGuid = addProblemResult.Payload;

        var beatSheetName = _api.GetBeatSheetNames().Payload.First();
        _api.ApplyBeatSheetToProblem(problemGuid, beatSheetName);

        var beats = _api.GetProblemStructure(problemGuid).Payload.Beats.ToList();
        if (beats.Count < 2)
        {
            Assert.Inconclusive("Need at least 2 beats to test move");
            return;
        }
        var firstBeatTitle = beats[0].BeatTitle;
        var secondBeatTitle = beats[1].BeatTitle;

        // Act - move first beat to second position
        var result = _api.MoveBeat(problemGuid, 0, 1);

        // Assert
        Assert.IsTrue(result.IsSuccess, "MoveBeat should succeed");

        var newBeats = _api.GetProblemStructure(problemGuid).Payload.Beats.ToList();
        Assert.AreEqual(secondBeatTitle, newBeats[0].BeatTitle, "Second beat should now be first");
        Assert.AreEqual(firstBeatTitle, newBeats[1].BeatTitle, "First beat should now be second");
    }

    // ===== Get Problem Structure =====

    /// <summary>
    /// Tests that GetProblemStructure returns the current structure of a Problem
    /// </summary>
    [TestMethod]
    public async Task GetProblemStructure_ValidProblem_ReturnsStructure()
    {
        // Arrange - create model and apply beat sheet
        await _api.CreateEmptyOutline("Test Story", "Test Author", "0");
        var overviewGuid = _api.CurrentModel.ExplorerView.First().Uuid;
        var addProblemResult = _api.AddElement(StoryItemType.Problem, overviewGuid.ToString(), "Test Problem");
        var problemGuid = addProblemResult.Payload;

        var beatSheetName = _api.GetBeatSheetNames().Payload.First();
        _api.ApplyBeatSheetToProblem(problemGuid, beatSheetName);

        // Act
        var result = _api.GetProblemStructure(problemGuid);

        // Assert
        Assert.IsTrue(result.IsSuccess, "GetProblemStructure should succeed");
        Assert.IsNotNull(result.Payload.Title, "Title should not be null");
        Assert.IsNotNull(result.Payload.Description, "Description should not be null");
        Assert.IsNotNull(result.Payload.Beats, "Beats should not be null");
    }

    /// <summary>
    /// Tests that GetProblemStructure fails for invalid Problem GUID
    /// </summary>
    [TestMethod]
    public async Task GetProblemStructure_InvalidProblem_ReturnsFailure()
    {
        // Arrange
        await _api.CreateEmptyOutline("Test Story", "Test Author", "0");

        // Act
        var result = _api.GetProblemStructure(Guid.NewGuid());

        // Assert
        Assert.IsFalse(result.IsSuccess, "GetProblemStructure should fail for invalid GUID");
    }

    /// <summary>
    /// Tests that GetProblemStructure returns empty structure for Problem without beat sheet
    /// </summary>
    [TestMethod]
    public async Task GetProblemStructure_NoBeatSheet_ReturnsEmptyStructure()
    {
        // Arrange - create model with Problem but no beat sheet
        await _api.CreateEmptyOutline("Test Story", "Test Author", "0");
        var overviewGuid = _api.CurrentModel.ExplorerView.First().Uuid;
        var addProblemResult = _api.AddElement(StoryItemType.Problem, overviewGuid.ToString(), "Test Problem");
        var problemGuid = addProblemResult.Payload;

        // Act
        var result = _api.GetProblemStructure(problemGuid);

        // Assert
        Assert.IsTrue(result.IsSuccess, "GetProblemStructure should succeed even without beat sheet");
        // Structure may be empty but should not fail
    }

    #endregion

    #region Conflict Apply API Tests (Issue #1223)

    /// <summary>
    /// Tests that ApplyConflictToProtagonist sets the ProtConflict property
    /// </summary>
    [TestMethod]
    public async Task ApplyConflictToProtagonist_ValidInputs_SetsProtConflict()
    {
        // Arrange - create model with Problem
        await _api.CreateEmptyOutline("Test Story", "Test Author", "0");
        var overviewGuid = _api.CurrentModel.ExplorerView.First().Uuid;
        var addProblemResult = _api.AddElement(StoryItemType.Problem, overviewGuid.ToString(), "Test Problem");
        Assert.IsTrue(addProblemResult.IsSuccess, "Problem creation should succeed");
        var problemGuid = addProblemResult.Payload;
        var conflictText = "Hero must overcome fear of heights";

        // Act
        var result = _api.ApplyConflictToProtagonist(problemGuid, conflictText);

        // Assert
        Assert.IsTrue(result.IsSuccess, "ApplyConflictToProtagonist should succeed");
        var problem = _api.CurrentModel.StoryElements.First(e => e.Uuid == problemGuid) as ProblemModel;
        Assert.AreEqual(conflictText, problem.ProtConflict, "ProtConflict should be set");
    }

    /// <summary>
    /// Tests that ApplyConflictToProtagonist returns failure for invalid problem
    /// </summary>
    [TestMethod]
    public async Task ApplyConflictToProtagonist_InvalidProblem_ReturnsFailure()
    {
        // Arrange
        await _api.CreateEmptyOutline("Test Story", "Test Author", "0");
        var invalidGuid = Guid.NewGuid();

        // Act
        var result = _api.ApplyConflictToProtagonist(invalidGuid, "Some conflict");

        // Assert
        Assert.IsFalse(result.IsSuccess, "Should fail for invalid problem GUID");
    }

    /// <summary>
    /// Tests that ApplyConflictToAntagonist sets the AntagConflict property
    /// </summary>
    [TestMethod]
    public async Task ApplyConflictToAntagonist_ValidInputs_SetsAntagConflict()
    {
        // Arrange - create model with Problem
        await _api.CreateEmptyOutline("Test Story", "Test Author", "0");
        var overviewGuid = _api.CurrentModel.ExplorerView.First().Uuid;
        var addProblemResult = _api.AddElement(StoryItemType.Problem, overviewGuid.ToString(), "Test Problem");
        Assert.IsTrue(addProblemResult.IsSuccess, "Problem creation should succeed");
        var problemGuid = addProblemResult.Payload;
        var conflictText = "Villain seeks world domination";

        // Act
        var result = _api.ApplyConflictToAntagonist(problemGuid, conflictText);

        // Assert
        Assert.IsTrue(result.IsSuccess, "ApplyConflictToAntagonist should succeed");
        var problem = _api.CurrentModel.StoryElements.First(e => e.Uuid == problemGuid) as ProblemModel;
        Assert.AreEqual(conflictText, problem.AntagConflict, "AntagConflict should be set");
    }

    /// <summary>
    /// Tests that ApplyConflictToAntagonist returns failure for invalid problem
    /// </summary>
    [TestMethod]
    public async Task ApplyConflictToAntagonist_InvalidProblem_ReturnsFailure()
    {
        // Arrange
        await _api.CreateEmptyOutline("Test Story", "Test Author", "0");
        var invalidGuid = Guid.NewGuid();

        // Act
        var result = _api.ApplyConflictToAntagonist(invalidGuid, "Some conflict");

        // Assert
        Assert.IsFalse(result.IsSuccess, "Should fail for invalid problem GUID");
    }

    #endregion

    #region Master Plot Scenes API Tests (Issue #1223)

    /// <summary>
    /// Tests that GetMasterPlotScenes returns scenes for a valid plot
    /// </summary>
    [TestMethod]
    public void GetMasterPlotScenes_ValidName_ReturnsScenes()
    {
        // Arrange - get a valid plot name first
        var namesResult = _api.GetMasterPlotNames();
        Assert.IsTrue(namesResult.IsSuccess, "Need valid plot name for test");
        var plotName = namesResult.Payload.First();

        // Act
        var result = _api.GetMasterPlotScenes(plotName);

        // Assert
        Assert.IsTrue(result.IsSuccess, "GetMasterPlotScenes should succeed for valid name");
        Assert.IsNotNull(result.Payload, "Payload should not be null");
        // Note: Some plots may have zero scenes, so we just check it doesn't fail
    }

    /// <summary>
    /// Tests that GetMasterPlotScenes returns failure for invalid plot name
    /// </summary>
    [TestMethod]
    public void GetMasterPlotScenes_InvalidName_ReturnsFailure()
    {
        // Arrange
        var plotName = "NonExistentPlot";

        // Act
        var result = _api.GetMasterPlotScenes(plotName);

        // Assert
        Assert.IsFalse(result.IsSuccess, "GetMasterPlotScenes should fail for invalid name");
        Assert.IsNull(result.Payload, "Payload should be null on failure");
        Assert.AreEqual($"No master plot '{plotName}' found", result.ErrorMessage);
    }

    #endregion
}
