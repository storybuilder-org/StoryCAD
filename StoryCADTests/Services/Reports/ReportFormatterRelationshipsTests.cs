using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCADLib.Models;
using StoryCADLib.Services;
using StoryCADLib.Services.Logging;
using StoryCADLib.Services.Outline;
using StoryCADLib.Services.Reports;
using StoryCADLib.ViewModels.Tools;

namespace StoryCADTests.Services.Reports;

/// <summary>
/// Tests for the Character Relationships Map report (issues #156 and #1478).
///
/// The report lists real relationships only. Missing reciprocals and
/// characters with nothing to print are omitted. A pair is printed once:
/// under the earlier character, with the reverse nested as Reciprocal
/// when that reverse entry exists.
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

    private static bool HasHeading(string report, string name)
    {
        return report.Split(["\r\n", "\n"], StringSplitOptions.None)
            .Any(line => line == name);
    }

    private static int CountReciprocalLines(string report)
    {
        return report.Split(["\r\n", "\n"], StringSplitOptions.None)
            .Count(line => line.TrimStart().StartsWith("Reciprocal:"));
    }

    [TestMethod]
    public async Task RelationshipsMap_WithAsymmetricRelationship_OmitsReciprocalLine()
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
        Assert.IsFalse(report.Contains("(no reciprocal relationship defined)"),
            "Missing reciprocal must not print a placeholder");
        Assert.AreEqual(0, CountReciprocalLines(report),
            "Asymmetric relationship must omit the Reciprocal line");
        Assert.IsTrue(HasHeading(report, "Alice"), "Source character should be a heading");
        Assert.IsFalse(HasHeading(report, "Bob"),
            "Partner with no relationships of their own should not be a heading");
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
        Assert.AreEqual(1, CountReciprocalLines(report),
            "A symmetric pair must print Reciprocal once");
        Assert.IsTrue(HasHeading(report, "Alice"), "Earlier character should keep the pair");
        Assert.IsFalse(HasHeading(report, "Bob"),
            "Later character whose only edge was already printed must not be a heading");
    }

    [TestMethod]
    public async Task RelationshipsMap_WithEmptyRelationshipList_OmitsCharacter()
    {
        // Arrange: a character with no relationships.
        var (model, appState) = await CreateModelAsync();
        var loner = new CharacterModel("Loner", model, null);

        // Act
        var formatter = new ReportFormatter(appState);
        var report = formatter.FormatCharacterRelationshipsMapReport();

        // Assert
        Assert.IsTrue(report.Contains("Character Relationships Map"),
            "Report should still contain the header");
        Assert.IsFalse(report.Contains("Loner"),
            "Character with no relationships should not appear");
        Assert.IsFalse(report.Contains("(no relationships)"),
            "Empty relationship lists must not print a placeholder");
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
        Assert.IsFalse(report.Contains("(no reciprocal relationship defined)"),
            "Deleted partner must not print a reciprocal placeholder");
    }

    [TestMethod]
    public async Task RelationshipsMap_WhenPartnerHasOtherRelationships_PrintsThoseOnce()
    {
        // Arrange: Alice -> Bob Father, Bob -> Alice Son, Bob -> Carol Friend.
        var (model, appState) = await CreateModelAsync();
        var alice = new CharacterModel("Alice", model, null);
        var bob = new CharacterModel("Bob", model, null);
        var carol = new CharacterModel("Carol", model, null);
        alice.RelationshipList.Add(new RelationshipModel(bob.Uuid, "Father"));
        bob.RelationshipList.Add(new RelationshipModel(alice.Uuid, "Son"));
        bob.RelationshipList.Add(new RelationshipModel(carol.Uuid, "Friend"));

        // Act
        var formatter = new ReportFormatter(appState);
        var report = formatter.FormatCharacterRelationshipsMapReport();

        // Assert
        Assert.IsTrue(HasHeading(report, "Alice"), "Alice should keep the Alice-Bob pair");
        Assert.IsTrue(HasHeading(report, "Bob"), "Bob should still print remaining relationships");
        Assert.IsFalse(HasHeading(report, "Carol"),
            "Carol has no relationships of her own and must not be a heading");
        Assert.IsTrue(report.Contains("Father"), "Forward relation type should print");
        Assert.IsTrue(report.Contains("Son"), "Reciprocal relation type should print");
        Assert.IsTrue(report.Contains("Friend"), "Bob's remaining relationship should print");
        Assert.AreEqual(1, CountReciprocalLines(report),
            "The Alice-Bob pair must print Reciprocal once");
        Assert.IsFalse(
            report.Split(["\r\n", "\n"], StringSplitOptions.None)
                .Any(line => line.StartsWith("    -> Alice")),
            "Alice must not appear as a primary relationship under Bob");
        Assert.IsFalse(report.Contains("(no reciprocal relationship defined)"));
        Assert.IsFalse(report.Contains("(no relationships)"));
    }

    [TestMethod]
    public async Task RelationshipsMap_WithTwoRelationships_SeparatesThemWithBlankLine()
    {
        // Arrange: one character, two one-way relationships (Leonard/Tony then Leonard/Charlie).
        var (model, appState) = await CreateModelAsync();
        var alice = new CharacterModel("Alice", model, null);
        var bob = new CharacterModel("Bob", model, null);
        var carol = new CharacterModel("Carol", model, null);
        alice.RelationshipList.Add(new RelationshipModel(bob.Uuid, "Father"));
        alice.RelationshipList.Add(new RelationshipModel(carol.Uuid, "Rival"));

        // Act
        var formatter = new ReportFormatter(appState);
        var report = formatter.FormatCharacterRelationshipsMapReport();

        // Assert
        var normalized = report.Replace("\r\n", "\n");
        Assert.IsTrue(
            normalized.Contains("    -> Bob  (Father)\n\n    -> Carol  (Rival)"),
            "Consecutive relationships under one heading must be separated by a blank line");
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
        Assert.IsFalse(result.Contains("(no reciprocal relationship defined)"),
            "Wired report must not print reciprocal placeholders");
        Assert.IsFalse(result.Contains("(no relationships)"),
            "Wired report must not print empty-list placeholders");
    }
}
