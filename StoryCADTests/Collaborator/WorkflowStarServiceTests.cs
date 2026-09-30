using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCADLib.Models.Tools;
using StoryCADLib.Services;
using StoryCADLib.Services.Collaborator;
using StoryCollaborator.Workflows;

#nullable disable

namespace StoryCADTests.Collaborator;

/// <summary>
/// Star persistence rules: the defaults seed exactly once, and after that the user's set wins —
/// including when it is empty.
/// </summary>
[TestClass]
public class WorkflowStarServiceTests
{
    private static readonly string[] Defaults = { "Premise", "ProblemBuilder", "SceneBuilder" };

    private PreferenceService _preferences;
    private int _writeCount;
    private WorkflowStarService _service;

    [TestInitialize]
    public void Setup()
    {
        _preferences = new PreferenceService { Model = new PreferencesModel() };
        _writeCount = 0;
        _service = new WorkflowStarService(_preferences, null, _ =>
        {
            _writeCount++;
            return Task.CompletedTask;
        });
    }

    [TestMethod]
    public async Task GetStarredAsync_OnFirstRun_SeedsDefaultsAndPersists()
    {
        var starred = await _service.GetStarredAsync(Defaults);

        CollectionAssert.AreEqual(Defaults, starred.ToArray());
        Assert.IsTrue(_preferences.Model.CollaboratorStarDefaultsApplied);
        Assert.AreEqual(1, _writeCount);
    }

    [TestMethod]
    public async Task GetStarredAsync_OnSecondRun_DoesNotReseedOrWrite()
    {
        await _service.GetStarredAsync(Defaults);
        _writeCount = 0;

        var starred = await _service.GetStarredAsync(Defaults);

        CollectionAssert.AreEqual(Defaults, starred.ToArray());
        Assert.AreEqual(0, _writeCount, "A second read must not touch the disk.");
    }

    [TestMethod]
    public async Task GetStarredAsync_AfterUserEdit_ReturnsTheEdit()
    {
        await _service.GetStarredAsync(Defaults);
        await _service.SetStarredAsync(new[] { "FlawBackstory" });

        var starred = await _service.GetStarredAsync(Defaults);

        CollectionAssert.AreEqual(new[] { "FlawBackstory" }, starred.ToArray());
    }

    [TestMethod]
    public async Task GetStarredAsync_AfterUserUnstarsEverything_StaysEmpty()
    {
        // Unstarring everything is a choice. Re-seeding the defaults over it would override the
        // user every time they reopen Collaborator.
        await _service.GetStarredAsync(Defaults);
        await _service.SetStarredAsync(new List<string>());

        var starred = await _service.GetStarredAsync(Defaults);

        Assert.AreEqual(0, starred.Count);
    }

    [TestMethod]
    public async Task SetStarredAsync_BeforeAnyRead_MarksDefaultsApplied()
    {
        // Otherwise the next read would seed the defaults straight over the user's pick.
        await _service.SetStarredAsync(new[] { "FlawBackstory" });

        var starred = await _service.GetStarredAsync(Defaults);

        CollectionAssert.AreEqual(new[] { "FlawBackstory" }, starred.ToArray());
    }

    [TestMethod]
    public async Task SetStarredAsync_WithDuplicatesAndBlanks_StoresCleanOrderedLabels()
    {
        await _service.SetStarredAsync(new[] { "ProblemBuilder", "", "Premise", "ProblemBuilder", null, "   " });

        var starred = await _service.GetStarredAsync(Defaults);

        CollectionAssert.AreEqual(new[] { "ProblemBuilder", "Premise" }, starred.ToArray());
    }

    [TestMethod]
    public async Task SetStarredAsync_WithNull_StoresEmptySet()
    {
        await _service.SetStarredAsync(null);

        var starred = await _service.GetStarredAsync(Defaults);

        Assert.AreEqual(0, starred.Count);
    }

    [TestMethod]
    public async Task GetStarredAsync_WhenPreferencesListIsNull_SeedsWithoutThrowing()
    {
        // Preferences.json written by an older build has no starred array at all.
        _preferences.Model.StarredCollaboratorWorkflows = null;

        var starred = await _service.GetStarredAsync(Defaults);

        CollectionAssert.AreEqual(Defaults, starred.ToArray());
    }

    [TestMethod]
    public async Task GetStarredAsync_ReturnsACopy_SoCallersCannotMutateStoredState()
    {
        var first = await _service.GetStarredAsync(Defaults);
        var second = await _service.GetStarredAsync(Defaults);

        Assert.AreNotSame(first, second);
        Assert.AreNotSame(_preferences.Model.StarredCollaboratorWorkflows, first);
    }

    // ── #211 retired-star migration ───────────────────────────────────────────────────────────

    private static readonly Dictionary<string, string> Retired = new()
    {
        ["GMC"] = "ProblemBuilder",
        ["Structure"] = "ProblemBuilder",
        ["SceneSummary"] = "SceneBuilder",
        ["SceneConflict"] = "SceneBuilder"
    };

    /// <summary>
    ///     The case on a real machine: seeded before #77 and #208, so four of seven stars name
    ///     workflows #211 deletes. They must land on the workflows that absorbed them, collapsed
    ///     to one star each, rather than vanishing from the band.
    /// </summary>
    [TestMethod]
    public async Task GetStarredAsync_WithPreConsolidationStars_MapsThemToReplacements()
    {
        _preferences.Model.CollaboratorStarDefaultsApplied = true;
        _preferences.Model.CollaboratorStarMigrationVersion = 0;
        _preferences.Model.StarredCollaboratorWorkflows = new List<string>
        {
            "Premise", "StoryProblem", "GMC", "Structure", "StoryFunction", "SceneSummary", "SceneConflict"
        };

        var starred = await _service.GetStarredAsync(Defaults, Retired, 1);

        CollectionAssert.AreEqual(
            new[] { "Premise", "StoryProblem", "ProblemBuilder", "StoryFunction", "SceneBuilder" },
            starred.ToArray());
        Assert.AreEqual(1, _preferences.Model.CollaboratorStarMigrationVersion);
    }

    [TestMethod]
    public async Task GetStarredAsync_WithCharacterFillStars_MapsBothToCharacterBuilder()
    {
        _preferences.Model.CollaboratorStarDefaultsApplied = true;
        _preferences.Model.CollaboratorStarMigrationVersion = 2;
        _preferences.Model.StarredCollaboratorWorkflows = new List<string>
        {
            "Premise", "DefineCharacter", "StoryFunction", "SceneBuilder"
        };

        var starred = await _service.GetStarredAsync(
            WorkflowRegistry.DefaultStarredLabels.ToArray(),
            WorkflowRegistry.RetiredWorkflowReplacements,
            WorkflowRegistry.StarMigrationVersion,
            WorkflowRegistry.StarsAddedByVersion);

        // #269 appends SettingBuilder in the same pass, because this user is behind version 4 too.
        CollectionAssert.AreEqual(
            new[] { "Premise", "CharacterBuilder", "SceneBuilder", "SettingBuilder" },
            starred.ToArray());
        Assert.AreEqual(WorkflowRegistry.StarMigrationVersion, _preferences.Model.CollaboratorStarMigrationVersion);
    }

    /// <summary>
    ///     Collaborator #269. A user seeded with the five defaults before Setting Builder was
    ///     starred gets it once, at the end of the band, and keeps their order.
    /// </summary>
    [TestMethod]
    public async Task GetStarredAsync_SeededBeforeVersion4_AddsSettingBuilderOnce()
    {
        _preferences.Model.CollaboratorStarDefaultsApplied = true;
        _preferences.Model.CollaboratorStarMigrationVersion = 3;
        _preferences.Model.StarredCollaboratorWorkflows = new List<string>
        {
            "Premise", "StoryProblem", "ProblemBuilder", "CharacterBuilder", "SceneBuilder"
        };

        var starred = await GetStarredFromRegistryAsync();

        CollectionAssert.AreEqual(
            new[] { "Premise", "StoryProblem", "ProblemBuilder", "CharacterBuilder", "SceneBuilder", "SettingBuilder" },
            starred.ToArray());
        Assert.AreEqual(4, _preferences.Model.CollaboratorStarMigrationVersion);
    }

    [TestMethod]
    public async Task GetStarredAsync_WhenSettingBuilderIsAlreadyStarred_DoesNotAddItTwice()
    {
        _preferences.Model.CollaboratorStarDefaultsApplied = true;
        _preferences.Model.CollaboratorStarMigrationVersion = 3;
        _preferences.Model.StarredCollaboratorWorkflows = new List<string> { "SettingBuilder", "Premise" };

        var starred = await GetStarredFromRegistryAsync();

        CollectionAssert.AreEqual(new[] { "SettingBuilder", "Premise" }, starred.ToArray());
    }

    /// <summary>
    ///     The add runs once. A user who unstars Setting Builder after it was added keeps it off.
    /// </summary>
    [TestMethod]
    public async Task GetStarredAsync_AfterUserUnstarsTheAddedStar_DoesNotAddItAgain()
    {
        _preferences.Model.CollaboratorStarDefaultsApplied = true;
        _preferences.Model.CollaboratorStarMigrationVersion = 3;
        _preferences.Model.StarredCollaboratorWorkflows = new List<string> { "Premise" };

        await GetStarredFromRegistryAsync();
        await _service.SetStarredAsync(new[] { "Premise" });
        var starred = await GetStarredFromRegistryAsync();

        CollectionAssert.AreEqual(new[] { "Premise" }, starred.ToArray());
    }

    [TestMethod]
    public async Task GetStarredAsync_OnFirstRun_SeedsSettingBuilderWithTheDefaults()
    {
        var starred = await GetStarredFromRegistryAsync();

        CollectionAssert.AreEqual(WorkflowRegistry.DefaultStarredLabels.ToArray(), starred.ToArray());
        CollectionAssert.Contains(starred.ToList(), "SettingBuilder");
    }

    [TestMethod]
    public void StarsAddedByVersion_NameOnlyDefaultLabelsAtOrBelowTheCurrentVersion()
    {
        foreach (var (version, labels) in WorkflowRegistry.StarsAddedByVersion)
        {
            Assert.IsTrue(version <= WorkflowRegistry.StarMigrationVersion, $"version {version}");
            foreach (var label in labels)
            {
                CollectionAssert.Contains(WorkflowRegistry.DefaultStarredLabels.ToList(), label,
                    $"an added star must also be a default, or new users would not get it: {label}");
            }
        }
    }

    private Task<IReadOnlyList<string>> GetStarredFromRegistryAsync() =>
        _service.GetStarredAsync(
            WorkflowRegistry.DefaultStarredLabels,
            WorkflowRegistry.RetiredWorkflowReplacements,
            WorkflowRegistry.StarMigrationVersion,
            WorkflowRegistry.StarsAddedByVersion);

    [TestMethod]
    public async Task GetStarredAsync_AfterMigrating_DoesNotRunAgain()
    {
        _preferences.Model.CollaboratorStarDefaultsApplied = true;
        _preferences.Model.CollaboratorStarMigrationVersion = 0;
        _preferences.Model.StarredCollaboratorWorkflows = new List<string> { "GMC" };

        await _service.GetStarredAsync(Defaults, Retired, 1);
        var writesAfterFirst = _writeCount;

        // The user then unstars everything. A second call must not re-migrate or re-seed.
        await _service.SetStarredAsync(new string[0]);
        var starred = await _service.GetStarredAsync(Defaults, Retired, 1);

        Assert.AreEqual(0, starred.Count, "an empty set the user chose stays empty");
        Assert.IsTrue(_writeCount > writesAfterFirst, "SetStarredAsync persists");
    }

    [TestMethod]
    public async Task GetStarredAsync_WhenNothingRetired_LeavesTheSetAloneButRecordsTheVersion()
    {
        _preferences.Model.CollaboratorStarDefaultsApplied = true;
        _preferences.Model.CollaboratorStarMigrationVersion = 0;
        _preferences.Model.StarredCollaboratorWorkflows = new List<string> { "Premise", "SceneBuilder" };

        var starred = await _service.GetStarredAsync(Defaults, Retired, 1);

        CollectionAssert.AreEqual(new[] { "Premise", "SceneBuilder" }, starred.ToArray());
        Assert.AreEqual(1, _preferences.Model.CollaboratorStarMigrationVersion,
            "the version must advance even when no label changed, or it migrates on every launch");
    }

    [TestMethod]
    public async Task GetStarredAsync_OnFirstRun_SeedsAtTheCurrentMigrationVersion()
    {
        var starred = await _service.GetStarredAsync(Defaults, Retired, 1);

        CollectionAssert.AreEqual(Defaults, starred.ToArray());
        Assert.AreEqual(1, _preferences.Model.CollaboratorStarMigrationVersion,
            "a set seeded from today's defaults names nothing retired");
    }

    /// <summary>
    ///     A retired label with no replacement listed is left where it is. The menu already
    ///     ignores what it cannot resolve, so a no-op beats inventing a destination.
    /// </summary>
    [TestMethod]
    public async Task GetStarredAsync_WithUnmappedLabel_LeavesItInPlace()
    {
        _preferences.Model.CollaboratorStarDefaultsApplied = true;
        _preferences.Model.CollaboratorStarMigrationVersion = 0;
        _preferences.Model.StarredCollaboratorWorkflows = new List<string> { "SomeWithdrawnWorkflow", "GMC" };

        var starred = await _service.GetStarredAsync(Defaults, Retired, 1);

        CollectionAssert.AreEqual(new[] { "SomeWithdrawnWorkflow", "ProblemBuilder" }, starred.ToArray());
    }

    /// <summary>
    ///     Collaborator #224, against the real registry constants rather than a fixture. A user
    ///     who curated stars after #211 sits at migration version 1 and can hold both Setting
    ///     labels. Both must land on SettingBuilder, collapsed to one star, and the stored
    ///     version must reach the registry's, or the rewrite runs again on every launch.
    /// </summary>
    [TestMethod]
    public async Task GetStarredAsync_WithBothSettingStars_CollapsesThemToSettingBuilder()
    {
        _preferences.Model.CollaboratorStarDefaultsApplied = true;
        _preferences.Model.CollaboratorStarMigrationVersion = 1;
        _preferences.Model.StarredCollaboratorWorkflows = new List<string>
        {
            "Premise", "SettingTimeSpace", "SceneBuilder", "Sensations"
        };

        var starred = await _service.GetStarredAsync(
            WorkflowRegistry.DefaultStarredLabels,
            WorkflowRegistry.RetiredWorkflowReplacements,
            WorkflowRegistry.StarMigrationVersion);

        CollectionAssert.AreEqual(
            new[] { "Premise", "SettingBuilder", "SceneBuilder" },
            starred.ToArray(),
            string.Join(", ", starred));
        Assert.AreEqual(WorkflowRegistry.StarMigrationVersion,
            _preferences.Model.CollaboratorStarMigrationVersion);
    }

    /// <summary>
    ///     The same user, one launch later. Nothing may change a second time.
    /// </summary>
    [TestMethod]
    public async Task GetStarredAsync_AfterTheSettingMigration_IsStable()
    {
        _preferences.Model.CollaboratorStarDefaultsApplied = true;
        _preferences.Model.CollaboratorStarMigrationVersion = 1;
        _preferences.Model.StarredCollaboratorWorkflows = new List<string> { "Sensations" };

        await _service.GetStarredAsync(
            WorkflowRegistry.DefaultStarredLabels,
            WorkflowRegistry.RetiredWorkflowReplacements,
            WorkflowRegistry.StarMigrationVersion);
        var starred = await _service.GetStarredAsync(
            WorkflowRegistry.DefaultStarredLabels,
            WorkflowRegistry.RetiredWorkflowReplacements,
            WorkflowRegistry.StarMigrationVersion);

        CollectionAssert.AreEqual(new[] { "SettingBuilder" }, starred.ToArray());
    }
}
