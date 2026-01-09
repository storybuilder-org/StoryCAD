using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCADLib.Models;
using StoryCADLib.Services;
using StoryCADLib.ViewModels;

#nullable disable

namespace StoryCADTests.ViewModels;

/// <summary>
/// Tests for issue #1219 - NullReferenceException when "(none)" placeholder
/// is selected in relationship dialog. Tests the helper methods that extract
/// testable logic from AddRelationship().
/// </summary>
[TestClass]
public class AddRelationshipTests
{
    #region Test Infrastructure

    private CharacterViewModel _viewModel;
    private StoryModel _storyModel;
    private CharacterModel _testCharacter;
    private AppState _appState;

    #endregion

    #region TestInitialize and TestCleanup

    [TestInitialize]
    public void TestInitialize()
    {
        _appState = Ioc.Default.GetRequiredService<AppState>();
        _viewModel = Ioc.Default.GetRequiredService<CharacterViewModel>();

        // Create a fresh story model (not via OutlineService to avoid extra infrastructure)
        _storyModel = new StoryModel();

        // Create test character and add to Characters collection
        _testCharacter = new CharacterModel("Test Character", _storyModel, null);
        _storyModel.StoryElements.Characters.Add(_testCharacter);

        // Set up AppState.CurrentDocument so Activate() can load the model
        _appState.CurrentDocument = new StoryDocument(_storyModel);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _viewModel = null;
        _storyModel = null;
        _testCharacter = null;
    }

    #endregion

    #region ProspectivePartners Filtering Tests

    [TestMethod]
    public void GetProspectivePartners_WithNonePlaceholder_ExcludesPlaceholder()
    {
        // Arrange - Activate ViewModel with test character
        _viewModel.Activate(_testCharacter);

        // Verify the "(none)" placeholder exists (added in StoryElementCollection constructor)
        var nonePlaceholder = _storyModel.StoryElements.Characters[0];
        Assert.AreEqual("(none)", nonePlaceholder.Name, "First character should be (none) placeholder");
        Assert.IsFalse(nonePlaceholder is CharacterModel,
            "Placeholder should NOT be CharacterModel, just StoryElement");

        // Act - Get prospective partners (should filter out non-CharacterModel entries)
        var prospectivePartners = _viewModel.GetProspectivePartners(_storyModel);

        // Assert - Verify placeholder is NOT included
        Assert.IsFalse(prospectivePartners.Any(p => p.Name == "(none)"),
            "Prospective partners should NOT include (none) placeholder");
        Assert.IsTrue(prospectivePartners.All(p => p is CharacterModel),
            "All prospective partners should be CharacterModel instances");
    }

    [TestMethod]
    public void GetProspectivePartners_WithValidCharacters_IncludesAllValidCharacters()
    {
        // Arrange - Add multiple valid characters
        var char1 = new CharacterModel("Alice", _storyModel, null);
        var char2 = new CharacterModel("Bob", _storyModel, null);
        _storyModel.StoryElements.Characters.Add(char1);
        _storyModel.StoryElements.Characters.Add(char2);
        _viewModel.Activate(_testCharacter);

        // Act
        var prospectivePartners = _viewModel.GetProspectivePartners(_storyModel);

        // Assert - Verify both valid characters are included
        Assert.IsTrue(prospectivePartners.Any(p => p.Name == "Alice"),
            "Alice should be in prospective partners");
        Assert.IsTrue(prospectivePartners.Any(p => p.Name == "Bob"),
            "Bob should be in prospective partners");
    }

    [TestMethod]
    public void GetProspectivePartners_ExcludesSelf()
    {
        // Arrange - Add another character so we have someone to include
        var otherChar = new CharacterModel("Other", _storyModel, null);
        _storyModel.StoryElements.Characters.Add(otherChar);
        _viewModel.Activate(_testCharacter);

        // Act
        var prospectivePartners = _viewModel.GetProspectivePartners(_storyModel);

        // Assert - Verify current character (self) is NOT included
        Assert.IsFalse(prospectivePartners.Any(p => p.Uuid == _testCharacter.Uuid),
            "Current character should NOT be in prospective partners");
        Assert.IsTrue(prospectivePartners.Any(p => p.Uuid == otherChar.Uuid),
            "Other character should be in prospective partners");
    }

    [TestMethod]
    public void GetProspectivePartners_WithExistingRelationship_ExcludesExistingPartner()
    {
        // Arrange - Create a partner and add an existing relationship
        var existingPartner = new CharacterModel("Existing Partner", _storyModel, null);
        var newPartner = new CharacterModel("New Partner", _storyModel, null);
        _storyModel.StoryElements.Characters.Add(existingPartner);
        _storyModel.StoryElements.Characters.Add(newPartner);
        _viewModel.Activate(_testCharacter);

        // Add existing relationship
        var relationship = new RelationshipModel(existingPartner.Uuid, "Friend");
        relationship.Partner = existingPartner;
        _viewModel.CharacterRelationships.Add(relationship);

        // Act
        var prospectivePartners = _viewModel.GetProspectivePartners(_storyModel);

        // Assert - Verify existing partner is excluded, new partner included
        Assert.IsFalse(prospectivePartners.Any(p => p.Uuid == existingPartner.Uuid),
            "Existing relationship partner should NOT be in prospective partners");
        Assert.IsTrue(prospectivePartners.Any(p => p.Uuid == newPartner.Uuid),
            "New partner should be in prospective partners");
    }

    #endregion

    #region Inverse Relationship Validation Tests

    [TestMethod]
    public void TryCreateInverseRelationship_WithNonCharacterModelPartner_ReturnsFalse()
    {
        // Arrange - Get the "(none)" placeholder which is NOT a CharacterModel
        _viewModel.Activate(_testCharacter);
        var nonePlaceholder = _storyModel.StoryElements.Characters[0];
        Assert.AreEqual("(none)", nonePlaceholder.Name);
        Assert.IsFalse(nonePlaceholder is CharacterModel);

        // Act - Attempt to create inverse relationship with non-CharacterModel
        // This should NOT throw NullReferenceException
        var result = _viewModel.TryCreateInverseRelationship(
            nonePlaceholder,
            "Partner",
            out string errorMessage);

        // Assert - Should fail gracefully without throwing
        Assert.IsFalse(result, "Should return false for non-CharacterModel partner");
        Assert.IsFalse(string.IsNullOrEmpty(errorMessage),
            "Should provide error message for invalid partner type");
    }

    [TestMethod]
    public void TryCreateInverseRelationship_WithValidCharacterModel_Succeeds()
    {
        // Arrange
        var partner = new CharacterModel("Valid Partner", _storyModel, null);
        _storyModel.StoryElements.Characters.Add(partner);
        _viewModel.Activate(_testCharacter);

        // Act
        var result = _viewModel.TryCreateInverseRelationship(
            partner,
            "Friend",
            out string errorMessage);

        // Assert
        Assert.IsTrue(result, "Should succeed with valid CharacterModel partner");
        Assert.IsTrue(string.IsNullOrEmpty(errorMessage),
            "Should not have error message on success");
        Assert.IsTrue(partner.RelationshipList.Any(r => r.PartnerUuid == _testCharacter.Uuid),
            "Partner should have inverse relationship to current character");
    }

    [TestMethod]
    public void TryCreateInverseRelationship_WithExistingInverse_DoesNotDuplicate()
    {
        // Arrange - Create partner with existing inverse relationship
        var partner = new CharacterModel("Partner With Existing", _storyModel, null);
        _storyModel.StoryElements.Characters.Add(partner);
        _viewModel.Activate(_testCharacter);

        // Add existing inverse relationship
        partner.RelationshipList.Add(new RelationshipModel(_testCharacter.Uuid, "Existing"));

        // Act
        var result = _viewModel.TryCreateInverseRelationship(
            partner,
            "Friend",
            out string errorMessage);

        // Assert - Should succeed but not add duplicate
        Assert.IsTrue(result, "Should succeed even with existing inverse");
        Assert.AreEqual(1, partner.RelationshipList.Count(r => r.PartnerUuid == _testCharacter.Uuid),
            "Should not duplicate inverse relationship");
    }

    #endregion
}
