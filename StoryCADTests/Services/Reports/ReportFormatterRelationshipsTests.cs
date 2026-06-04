using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCADLib.Models;
using StoryCADLib.Services;
using StoryCADLib.Services.Logging;
using StoryCADLib.Services.Outline;
using StoryCADLib.Services.Reports;
using StoryCADLib.ViewModels.Tools;

namespace StoryCADTests.Services.Reports;

/// <summary>
/// Tests for the Character Relationships Map report (issue #156).
///
/// The report lists, for every character, each of that character's relationships
/// AND the inverse view from the counterpart's perspective. Inverse relationships
/// are created opt-in, so the report must gracefully state when no reciprocal
/// exists rather than implying symmetry.
/// </summary>
[TestClass]
public class ReportFormatterRelationshipsTests
{
    private static async Task<(StoryModel model, AppState appState)> CreateModelAsync()
    {
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var outlineService = Ioc.Default.GetRequiredService<OutlineService>();
        var model = await outlineService.CreateModel("Test Story", "Test Author", 0);
        appState.CurrentDocument = new StoryDocument(model, "test.stbx");
        return (model, appState);
    }

    [TestMethod]
    public async Task RelationshipsMap_WithAsymmetricRelationship_ShowsNoReciprocal()
    {
        // Arrange: A -> B "Father", no B -> A.
        var (model, appState) = await CreateModelAsync();
        var alice = new CharacterModel("Alice", model, null);
        var bob = new CharacterModel("Bob", model, null);
        alice.RelationshipList.Add(new RelationshipModel(bob.Uuid, "Father"));

        // Act
        var formatter = new ReportFormatter(appState);
        var report = formatter.FormatCharacterRelationshipsMapReport();

        // Assert
        Assert.IsTrue(report.Contains("Alice"), "Report should contain source character name");
        Assert.IsTrue(report.Contains("Bob"), "Report should contain partner character name");
        Assert.IsTrue(report.Contains("Father"), "Report should contain the relation type");
        Assert.IsTrue(report.Contains("(no reciprocal relationship defined)"),
            "Asymmetric relationship must surface the missing reciprocal");
    }

    [TestMethod]
    public async Task RelationshipsMap_WithSymmetricRelationship_ShowsBothDirections()
    {
        // Arrange: A -> B "Father" and B -> A "Son".
        var (model, appState) = await CreateModelAsync();
        var alice = new CharacterModel("Alice", model, null);
        var bob = new CharacterModel("Bob", model, null);
        alice.RelationshipList.Add(new RelationshipModel(bob.Uuid, "Father"));
        bob.RelationshipList.Add(new RelationshipModel(alice.Uuid, "Son"));

        // Act
        var formatter = new ReportFormatter(appState);
        var report = formatter.FormatCharacterRelationshipsMapReport();

        // Assert
        Assert.IsTrue(report.Contains("Father"), "Report should show the forward relation type");
        Assert.IsTrue(report.Contains("Son"), "Report should show the reciprocal relation type");
        Assert.IsFalse(report.Contains("(no reciprocal relationship defined)"),
            "Both characters have reciprocal relationships, so none should be reported missing");
    }

    [TestMethod]
    public async Task RelationshipsMap_WithEmptyRelationshipList_RendersNoRelationships()
    {
        // Arrange: a character with no relationships.
        var (model, appState) = await CreateModelAsync();
        var loner = new CharacterModel("Loner", model, null);

        // Act
        var formatter = new ReportFormatter(appState);
        var report = formatter.FormatCharacterRelationshipsMapReport();

        // Assert
        Assert.IsTrue(report.Contains("Loner"), "Report should contain the character name");
        Assert.IsTrue(report.Contains("(no relationships)"),
            "Character with no relationships should be reported as such");
    }

    [TestMethod]
    public async Task RelationshipsMap_WithDeletedPartner_RendersUnknownCharacter()
    {
        // Arrange: A -> (deleted character). Mirrors the issue #1226 missing-element style.
        var (model, appState) = await CreateModelAsync();
        var alice = new CharacterModel("Alice", model, null);
        var ghost = new CharacterModel("Ghost", model, null);
        alice.RelationshipList.Add(new RelationshipModel(ghost.Uuid, "Rival"));
        model.StoryElements.Remove(ghost); // simulate a deleted partner

        // Act
        var formatter = new ReportFormatter(appState);
        var report = formatter.FormatCharacterRelationshipsMapReport();

        // Assert
        Assert.IsTrue(report.Contains("(unknown character)"),
            "A relationship to a deleted partner should render as unknown without throwing");
        Assert.IsTrue(report.Contains("Rival"), "Relation type should still render");
    }

    [TestMethod]
    public async Task Generate_WithCreateRelationshipsOnly_ProducesContentWithPageBreak()
    {
        // Arrange: wire-through via PrintReports with only CreateRelationships set.
        var (model, appState) = await CreateModelAsync();
        var windowing = Ioc.Default.GetRequiredService<Windowing>();
        var editFlush = Ioc.Default.GetRequiredService<EditFlushService>();
        var logger = Ioc.Default.GetRequiredService<ILogService>();

        var alice = new CharacterModel("Alice", model, null);
        var bob = new CharacterModel("Bob", model, null);
        alice.RelationshipList.Add(new RelationshipModel(bob.Uuid, "Mentor"));

        var vm = new PrintReportDialogVM(appState, windowing, editFlush, logger)
        {
            CreateRelationships = true,
            CreateOverview = false,
            CreateStoryWorld = false,
            CreateSummary = false,
            CreateStructure = false,
            ProblemList = false,
            CharacterList = false,
            SettingList = false,
            SceneList = false,
            WebList = false
        };

        // Act
        var printReports = new PrintReports(vm, appState, logger);
        var result = await printReports.Generate();

        // Assert
        Assert.IsTrue(result.Length > 0, "CreateRelationships should produce report content");
        Assert.IsTrue(result.Contains("\\PageBreak"), "Wired report should contain page break markers");
        Assert.IsTrue(result.Contains("Character Relationships Map"),
            "Wired report should contain the relationships map header");
        Assert.IsTrue(result.Contains("Mentor"), "Wired report should include the relation type");
    }
}
