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
    public void GetAllElements_WithoutModel_ThrowsException()
    {
        // Act & Assert: Calling GetAllElements without creating a model should throw an InvalidOperationException.
        Assert.ThrowsException<InvalidOperationException>(() => _api.GetAllElements());
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
        ObservableCollection<StoryElement> elements = _api.GetAllElements();

        // Assert
        Assert.IsNotNull(elements, "The returned collection should not be null.");
        Assert.IsTrue(elements.Count > 0, "There should be at least one StoryElement in the collection.");
    }

    [TestMethod]
    public void DeleteStoryElement_NotImplemented_ThrowsNotImplementedException()
    {
        // Arrange
        string randomGuid = Guid.NewGuid().ToString();

        // Act & Assert: DeleteStoryElement should throw NotImplementedException.
        Assert.ThrowsException<NotImplementedException>(() => _api.DeleteStoryElement(randomGuid));
    }

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
        string elementJson = (string)_api.GetElement(elementGuid);
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
        _api.UpdateStoryElement(updatedJson, elementGuid);

        // Assert: Retrieve element again and verify its name.
        string updatedElementJson = (string)_api.GetElement(elementGuid);
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
        _api.UpdateElementProperties(elementGuid, propertiesToUpdate);

        // Assert: Verify that the element's name is updated.
        string updatedElementJson = (string)_api.GetElement(elementGuid);
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
        object elementObj = _api.GetElement(elementGuid);
        string elementJson = elementObj as string;

        // Assert
        Assert.IsNotNull(elementJson, "GetElement should return a non-null JSON string.");
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
        var addResult = _api.AddElement(StoryItemType.Section, invalidParentGuid);

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
        var addResult = _api.AddElement(StoryItemType.Folder, parentGuid.ToString());

        // Assert
        Assert.IsTrue(addResult.IsSuccess, "AddElement should succeed with a valid parent.");
        Assert.IsNotNull(addResult.Payload, "The payload should not be null for a successfully added element.");

        // Optionally verify that the added element's type is Section.
        string newElementJson = JsonSerializer.Serialize(addResult.Payload);
        StoryElement newElement = JsonSerializer.Deserialize<StoryElement>(newElementJson);
        Assert.AreEqual(StoryItemType.Folder, newElement.ElementType, "The new element should be of type Section.");
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
        // Use one of the element GUIDs.
        Guid elementGuid = createResult.Payload.First();

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
        var sceneResult = _api.AddElement(StoryItemType.Scene, parentGuid.ToString());
        Assert.IsTrue(sceneResult.IsSuccess, "Scene element should be added successfully.");
        string sceneJson = JsonSerializer.Serialize(sceneResult.Payload);
        StoryElement sceneElement = JsonSerializer.Deserialize<StoryElement>(sceneJson);

        // Add a Character element.
        var characterResult = _api.AddElement(StoryItemType.Character, parentGuid.ToString());
        Assert.IsTrue(characterResult.IsSuccess, "Character element should be added successfully.");
        string characterJson = JsonSerializer.Serialize(characterResult.Payload);
        StoryElement characterElement = JsonSerializer.Deserialize<StoryElement>(characterJson);

        // Act: Add the character as a cast member to the scene.
        var castResult = await _api.AddCastMember(sceneElement.Uuid, characterElement.Uuid);

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
        var charResult1 = _api.AddElement(StoryItemType.Character, parentGuid.ToString());
        var charResult2 = _api.AddElement(StoryItemType.Character, parentGuid.ToString());
        Assert.IsTrue(charResult1.IsSuccess && charResult2.IsSuccess, 
            "Both character elements should be added successfully.");

        string charJson1 = JsonSerializer.Serialize(charResult1.Payload);
        string charJson2 = JsonSerializer.Serialize(charResult2.Payload);
        StoryElement charElement1 = JsonSerializer.Deserialize<StoryElement>(charJson1);
        StoryElement charElement2 = JsonSerializer.Deserialize<StoryElement>(charJson2);

        // Act: Add a relationship between the two characters.
        bool relationshipResult = _api.AddRelationship(charElement1.Uuid, 
            charElement2.Uuid, "Friendship");

        // Assert
        Assert.IsTrue(relationshipResult, "AddRelationship should return true on success.");

        // Also verify that calling with Guid.Empty throws an exception.
        Assert.ThrowsException<ArgumentNullException>(() =>
            _api.AddRelationship(Guid.Empty, charElement2.Uuid, "Invalid"));
    }
}