using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCAD.Models;
using StoryCAD.Services.API;

namespace StoryCADTests;

[TestClass]
public class SemanticKernelApiTests
{
    private SemanticKernelApi _api = new();

    [TestMethod]
    public async Task CreateOutlineWithInvalidTemplate()
    {
        // Arrange
        string name = "Test Outline";
        string author = "Test Author";
        string invalidTemplateIndex = "abc";

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
        string name = "Test Outline";
        string author = "Test Author";
        string validTemplateIndex = "0";

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
        string filePath = Path.Combine(App.InputDir, "NoModel.stbx");

        // Act
        var result = await _api.WriteOutline(filePath);

        // Assert
        Assert.IsFalse(result.IsSuccess, "WriteOutline should fail if no model is available.");
        Assert.IsTrue(result.ErrorMessage.Contains("Deserialized StoryModel is null"), "Error message should indicate a missing model.");
    }

    [TestMethod]
    public async Task WriteOutline_WithModel_WritesFileSuccessfully()
    {
        // Arrange
        string outlineName = "Test Outline";
        string author = "Test Author";
        var createResult = await _api.CreateEmptyOutline(outlineName, author, "0");
        Assert.IsTrue(createResult.IsSuccess, "Model creation should succeed.");
        string filePath = Path.Combine(App.InputDir, "Outline.stbx");

        // Act
        var writeResult = await _api.WriteOutline(filePath);

        // Assert
        Assert.IsTrue(writeResult.IsSuccess, "WriteOutline should succeed when a model is available.");
        Assert.IsTrue(File.Exists(filePath), "The outline file should exist after writing.");
        string fileContent = File.ReadAllText(filePath);
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
        string outlineName = "Test Outline";
        string author = "Test Author";
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
        string originalName = "Original Outline";
        string updatedName = "Updated Outline";
        string author = "Test Author";
        var createResult = await _api.CreateEmptyOutline(originalName, author, "0");
        Assert.IsTrue(createResult.IsSuccess, "Model creation should succeed.");
        Guid elementGuid = createResult.Payload.First();

        // Retrieve current element JSON.
        var getResult = _api.GetElement(elementGuid);
        Assert.IsTrue(getResult.IsSuccess, "GetElement should succeed");
        string elementJson = (string)getResult.Payload;
        // Deserialize to get element type information.
        StoryElement element = JsonSerializer.Deserialize<StoryElement>(elementJson);
        // Create updated JSON with the same GUID and type but a new Name.
        string updatedJson = JsonSerializer.Serialize(new
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
        string updatedElementJson = (string)getUpdatedResult.Payload;
        StoryElement updatedElement = JsonSerializer.Deserialize<StoryElement>(updatedElementJson);
        Assert.AreEqual(updatedName, updatedElement.Name, "The element's name should have been updated.");
    }

    [TestMethod]
    public async Task UpdateElementProperties_UpdatesElementNameSuccessfully()
    {
        // Arrange
        string originalName = "Original Outline";
        string updatedName = "Updated via Properties";
        string author = "Test Author";
        var createResult = await _api.CreateEmptyOutline(originalName, author, "0");
        Assert.IsTrue(createResult.IsSuccess, "Model creation should succeed.");
        Guid elementGuid = createResult.Payload.First();

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
        string updatedElementJson = (string)getResult.Payload;
        StoryElement updatedElement = JsonSerializer.Deserialize<StoryElement>(updatedElementJson);
        Assert.AreEqual(updatedName, updatedElement.Name, "The element's name should be updated via UpdateElementProperties.");
    }

    [TestMethod]
    public async Task GetElement_ReturnsSerializedElement()
    {
        // Arrange
        string outlineName = "Test Outline";
        string author = "Test Author";
        var createResult = await _api.CreateEmptyOutline(outlineName, author, "0");
        Assert.IsTrue(createResult.IsSuccess, "Model creation should succeed.");
        Guid elementGuid = createResult.Payload.First();

        // Act
        var getResult = _api.GetElement(elementGuid);

        // Assert
        Assert.IsTrue(getResult.IsSuccess, "GetElement should succeed");
        Assert.IsNotNull(getResult.Payload, "GetElement should return a non-null payload");
        string elementJson = getResult.Payload as string;
        Assert.IsNotNull(elementJson, "GetElement should return a JSON string.");
        Assert.IsTrue(elementJson.Contains(outlineName), "The returned JSON should include the element's name.");
        Assert.IsTrue(elementJson.Contains(elementGuid.ToString()), "The returned JSON should include the element's GUID.");
    }

    [TestMethod]
    public async Task AddInvalidParent()
    {
        // Arrange
        // Provide an invalid GUID string as parent.
        string invalidParentGuid = "not-a-guid";

        //open file first
        string file = Path.Combine(App.InputDir, "AddElement.stbx");
        Assert.IsTrue(File.Exists(file));
        await _api.OpenOutline(file);

        // Act
        var addResult = _api.AddElement(StoryItemType.Section, invalidParentGuid, "Added Section");

        // Assert
        Assert.IsFalse(addResult.IsSuccess, "AddElement should fail with an invalid parent GUID.");
        Assert.IsTrue(addResult.ErrorMessage.Contains("Invalid parent GUID"), "Error message should indicate invalid parent GUID.");
    }

    [TestMethod]
    public async Task AddElementWithValidParent()
    {
        // Arrange
        string outlineName = "Test Outline";
        string author = "Test Author";
        var createResult = await _api.CreateEmptyOutline(outlineName, author, "0");
        Assert.IsTrue(createResult.IsSuccess, "Model creation should succeed.");
        // Use one of the existing element GUIDs as the parent.
        Guid parentGuid = createResult.Payload.First();

        // Act
        var addResult = _api.AddElement(StoryItemType.Folder, parentGuid.ToString(), "Added Section");

        // Assert
        Assert.IsTrue(addResult.IsSuccess, "AddElement should succeed with a valid parent.");
        Assert.IsNotNull(addResult.Payload, "The payload should not be null for a successfully added element.");

        // Optionally verify that the added element's type is Section.
        StoryElement newElement = _api.CurrentModel.StoryElements.StoryElementGuids[addResult.Payload];
        Assert.AreEqual(StoryItemType.Folder, newElement.ElementType, "The new element should be of type Section.");

        Guid elementGuid = createResult.Payload.First();

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
        string updatedElementJson = (string)getResult.Payload;
        StoryElement updatedElement = JsonSerializer.Deserialize<StoryElement>(updatedElementJson);
        Assert.AreEqual("Renamed Section", updatedElement.Name, "The element's name should be updated via UpdateElementProperties.");
    }

    [TestMethod]
    public async Task UpdateElementProperty()
    {
        // Arrange
        string originalName = "Original Outline";
        string updatedName = "Updated via Property Method";
        string author = "Test Author";
        var createResult = await _api.CreateEmptyOutline(originalName, author, "0");
        Assert.IsTrue(createResult.IsSuccess, "Model creation should succeed.");
        Guid elementGuid = createResult.Payload.First();

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
        string invalidPath = ""; // empty path

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
        string outlineName = "Test Outline";
        string author = "Test Author";
        var createResult = await _api.CreateEmptyOutline(outlineName, author, "0");
        Assert.IsTrue(createResult.IsSuccess, "Model creation should succeed.");
        string filePath = Path.Combine(App.InputDir, "OpenedOutline.stbx");
        var writeResult = await _api.WriteOutline(filePath);
        Assert.IsTrue(writeResult.IsSuccess, "WriteOutline should succeed.");

        // Act: Create a new API instance and open the written file.
        var newApi = new SemanticKernelApi();
        var openResult = await newApi.OpenOutline(filePath);

        // Assert
        Assert.IsTrue(openResult.IsSuccess, "OpenOutline should succeed with a valid file.");
        Assert.IsTrue(openResult.Payload, "The payload should indicate a successful open operation.");
    }

    [TestMethod]
    public async Task DeleteElementTest()
    {
        // Arrange
        string outlineName = "Test Outline";
        string author = "Test Author";
        var createResult = await _api.CreateEmptyOutline(outlineName, author, "0");
        Assert.IsTrue(createResult.IsSuccess, "Model creation should succeed.");
        
        // Add a deletable element (character)
        var overviewGuid = _api.CurrentModel.ExplorerView.First().Uuid;
        var addResult = _api.AddElement(StoryItemType.Character, overviewGuid.ToString(), "Test Character");
        Assert.IsTrue(addResult.IsSuccess, "Adding character should succeed.");
        Guid elementGuid = addResult.Payload;

        // Act
        // Assume deletion from ExplorerView.
        var deleteResult = await _api.DeleteElement(elementGuid, StoryViewType.ExplorerView);

        // Assert
        Assert.IsTrue(deleteResult.IsSuccess, "DeleteElement should succeed.");
        Assert.IsTrue(deleteResult.Payload, "The payload should indicate that the deletion was successful.");
    }

    [TestMethod]
    public async Task AddCastMember()
    {
        // Arrange
        string outlineName = "Test Outline";
        string author = "Test Author";
        var createResult = await _api.CreateEmptyOutline(outlineName, author, "0");
        Assert.IsTrue(createResult.IsSuccess, "Model creation should succeed.");
        Guid parentGuid = createResult.Payload.First();

        // Add a Scene element.
        var sceneResult = _api.AddElement(StoryItemType.Scene, parentGuid.ToString(), "Added Scene");
        Assert.IsTrue(sceneResult.IsSuccess, "Scene element should be added successfully.");
        StoryElement sceneElement = _api.CurrentModel.StoryElements.StoryElementGuids[sceneResult.Payload];

        // Add a Character element.
        Guid characterGuid = _api.AddElement(StoryItemType.Character, parentGuid.ToString(), "Added Character").Payload;
        var characterResult = _api.CurrentModel.StoryElements.StoryElementGuids[characterGuid];
        Assert.IsTrue(characterResult.ElementType == StoryItemType.Character, "Character element should be added successfully.");

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
        string outlineName = "Test Outline";
        string author = "Test Author";
        var createResult = await _api.CreateEmptyOutline(outlineName, author, "0");
        Assert.IsTrue(createResult.IsSuccess, "Model creation should succeed.");
        Guid parentGuid = createResult.Payload.First();

        // Add two Character elements.
        var charResult1 = _api.AddElement(StoryItemType.Character, parentGuid.ToString(), "Character 1");
        var charResult2 = _api.AddElement(StoryItemType.Character, parentGuid.ToString(), "Character 2");
        Assert.IsTrue(charResult1.IsSuccess && charResult2.IsSuccess, 
            "Both character elements should be added successfully.");

        StoryElement charElement1 = _api.CurrentModel.StoryElements.StoryElementGuids[charResult1.Payload];
        StoryElement charElement2 = _api.CurrentModel.StoryElements.StoryElementGuids[charResult2.Payload];

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
    /// Tests that SetCurrentModel correctly sets the CurrentModel property
    /// </summary>
    [TestMethod]
    public async Task SetCurrentModel_WithValidModel_SetsCurrentModel()
    {
        // Arrange
        var api = new SemanticKernelApi();
        
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
    /// Tests that SetCurrentModel handles null model gracefully
    /// </summary>
    [TestMethod]
    public void SetCurrentModel_WithNullModel_SetsCurrentModelToNull()
    {
        // Arrange
        var api = new SemanticKernelApi();
        
        // Act
        api.SetCurrentModel(null);
        
        // Assert
        Assert.IsNull(api.CurrentModel, "CurrentModel should be null");
    }

    /// <summary>
    /// Tests that API operations work correctly after SetCurrentModel
    /// </summary>
    [TestMethod]
    public async Task SetCurrentModel_AllowsOperationsOnNewModel()
    {
        // Arrange
        var api = new SemanticKernelApi();
        
        // Create first model
        var firstResult = await api.CreateEmptyOutline("First Story", "Author 1", "0");
        Assert.IsTrue(firstResult.IsSuccess);
        var firstModel = api.CurrentModel;
        var firstOverviewGuid = firstResult.Payload.First();
        
        // Add an element to the first model
        var firstElementResult = api.AddElement(StoryItemType.Character, firstOverviewGuid.ToString(), "First Character");
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
        Assert.AreEqual(5, firstModel.StoryElements.Count, "First model should have 5 elements (overview, trash, narrative folder, character, problem)");
        Assert.AreEqual(4, secondModel.StoryElements.Count, "Second model should still have 4 elements (overview, trash, narrative folder, scene)");
        
        // Verify the new element is in the first model
        var problemElement = firstModel.StoryElements.StoryElementGuids[thirdElementResult.Payload];
        Assert.IsNotNull(problemElement);
        Assert.AreEqual("First Problem", problemElement.Name);
        Assert.AreEqual(StoryItemType.Problem, problemElement.ElementType);
    }

    /// <summary>
    /// Tests that DeleteStoryElement moves an element to trash
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
    /// Tests that DeleteStoryElement throws exception for invalid UUID
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
    /// Tests that DeleteStoryElement throws exception when no model is loaded
    /// </summary>
    [TestMethod]
    public void DeleteStoryElement_WithNoModel_ReturnsFailure()
    {
        // Arrange
        var api = new SemanticKernelApi();
        
        // Act
        var result = api.DeleteStoryElement(Guid.NewGuid().ToString());
        
        // Assert
        Assert.IsFalse(result.IsSuccess, "DeleteStoryElement should fail with no model");
        Assert.AreEqual("No StoryModel available. Create a model first.", result.ErrorMessage);
    }

    /// <summary>
    /// Tests that DeleteElement moves an element to trash using OperationResult
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
        var deleteResult = await _api.DeleteElement(sceneGuid, StoryViewType.ExplorerView);
        
        // Assert
        Assert.IsTrue(deleteResult.IsSuccess, "DeleteElement should succeed");
        var trashNode = _api.CurrentModel.TrashView.First();
        Assert.IsTrue(trashNode.Children.Any(n => n.Uuid == sceneGuid), "Scene should be in trash");
    }

    /// <summary>
    /// Tests that DeleteElement returns failure when no model is loaded
    /// </summary>
    [TestMethod]
    public async Task DeleteElement_WithNoModel_ReturnsFailure()
    {
        // Arrange
        var api = new SemanticKernelApi();
        
        // Act
        var result = await api.DeleteElement(Guid.NewGuid(), StoryViewType.ExplorerView);
        
        // Assert
        Assert.IsFalse(result.IsSuccess, "DeleteElement should fail");
        Assert.AreEqual("No outline is opened", result.ErrorMessage);
    }

    /// <summary>
    /// Tests that RestoreFromTrash restores an element from trash
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
        await _api.DeleteElement(problemGuid, StoryViewType.ExplorerView);
        
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
    /// Tests that RestoreFromTrash returns failure for element not in trash
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
    /// Tests that RestoreFromTrash returns failure when no model is loaded
    /// </summary>
    [TestMethod]
    public async Task RestoreFromTrash_WithNoModel_ReturnsFailure()
    {
        // Arrange
        var api = new SemanticKernelApi();
        
        // Act
        var result = await api.RestoreFromTrash(Guid.NewGuid());
        
        // Assert
        Assert.IsFalse(result.IsSuccess, "RestoreFromTrash should fail");
        Assert.AreEqual("No outline is opened", result.ErrorMessage);
    }

    /// <summary>
    /// Tests that EmptyTrash removes all items from trash
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
        await _api.DeleteElement(addResult1.Payload, StoryViewType.ExplorerView);
        await _api.DeleteElement(addResult2.Payload, StoryViewType.ExplorerView);
        
        var trashNode = _api.CurrentModel.TrashView.First();
        Assert.AreEqual(2, trashNode.Children.Count, "Should have 2 items in trash");
        
        // Act
        var emptyResult = await _api.EmptyTrash();
        
        // Assert
        Assert.IsTrue(emptyResult.IsSuccess, "EmptyTrash should succeed");
        Assert.AreEqual(0, trashNode.Children.Count, "Trash should be empty");
    }

    /// <summary>
    /// Tests that EmptyTrash succeeds even when trash is already empty
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
    /// Tests that EmptyTrash returns failure when no model is loaded
    /// </summary>
    [TestMethod]
    public async Task EmptyTrash_WithNoModel_ReturnsFailure()
    {
        // Arrange
        var api = new SemanticKernelApi();
        
        // Act
        var result = await api.EmptyTrash();
        
        // Assert
        Assert.IsFalse(result.IsSuccess, "EmptyTrash should fail");
        Assert.AreEqual("No outline is opened", result.ErrorMessage);
    }

    #region GetStoryElement Tests

    /// <summary>
    /// Tests that GetStoryElement returns success with the correct element for a valid GUID
    /// </summary>
    [TestMethod]
    public async Task GetStoryElement_WithValidGuid_ReturnsSuccess()
    {
        // Arrange
        var createResult = await _api.CreateEmptyOutline("Test Story", "Test Author", "1");  // Use template 1 to get a character
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
    /// Tests that GetStoryElement returns failure when no model is loaded
    /// </summary>
    [TestMethod]
    public void GetStoryElement_WithNoCurrentModel_ReturnsFailure()
    {
        // Arrange
        var api = new SemanticKernelApi();
        var someGuid = Guid.NewGuid();
        
        // Act
        var result = api.GetStoryElement(someGuid);
        
        // Assert
        Assert.IsFalse(result.IsSuccess, "Should return failure");
        Assert.IsNull(result.Payload, "Payload should be null");
        Assert.AreEqual("No StoryModel available. Create a model first.", result.ErrorMessage);
    }

    /// <summary>
    /// Tests that GetStoryElement returns failure for empty GUID
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
    /// Tests that GetStoryElement returns failure for non-existent GUID
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
}