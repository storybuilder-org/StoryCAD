using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCADLib.Models.Tools;
using StoryCADLib.Services;
using StoryCADLib.Services.Collaborator;

#nullable disable

namespace StoryCADTests.Collaborator;

/// <summary>
/// Star persistence rules: the defaults seed exactly once, and after that the user's set wins —
/// including when it is empty.
/// </summary>
[TestClass]
public class WorkflowStarServiceTests
{
    private static readonly string[] Defaults = { "Premise", "GMC", "SceneConflict" };

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
        await _service.SetStarredAsync(new[] { "GMC", "", "Premise", "GMC", null, "   " });

        var starred = await _service.GetStarredAsync(Defaults);

        CollectionAssert.AreEqual(new[] { "GMC", "Premise" }, starred.ToArray());
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
}
