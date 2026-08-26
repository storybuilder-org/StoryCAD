using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CollaboratorLib.Context;
using StoryCADLib.Models;
using StoryCADLib.Models.Tools;
using StoryCADLib.Services.API;
using StoryCADLib.Services.Outline;
using StoryCADLib.ViewModels;
using StoryCollaborator.Workflows;

namespace StoryCADTests.Collaborator;

/// <summary>
///     Collaborator #211. Workflow labels are bare strings on both sides of the gap map, so
///     deleting a workflow leaves no compile error behind — only a menu entry or a gap hint
///     pointing at something the Worker will reject. These are the guards that fail instead.
/// </summary>
[TestClass]
public class WorkflowLabelIntegrityTests
{
    private static StoryCADApi CreateApi() => new(
        Ioc.Default.GetRequiredService<OutlineService>(),
        Ioc.Default.GetRequiredService<ListData>(),
        Ioc.Default.GetRequiredService<ControlData>(),
        Ioc.Default.GetRequiredService<ToolsData>());

    /// <summary>
    ///     Walks the required-field scanner over a blank element of every type it knows, then
    ///     asks the gap map who fills each missing property. Every name it answers with has to
    ///     be a registered workflow.
    /// </summary>
    [TestMethod]
    public async Task GapWorkflowOwnership_NamesOnlyRegisteredWorkflows()
    {
        var api = CreateApi();
        await api.CreateEmptyOutline("Label integrity", "Author", "0");
        var model = api.CurrentModel!;
        var overview = model.StoryElements.First(e => e.ElementType == StoryItemType.StoryOverview);

        var elements = new List<StoryElement> { overview };
        foreach (var type in new[]
                 {
                     StoryItemType.Problem, StoryItemType.Character,
                     StoryItemType.Setting, StoryItemType.Scene
                 })
        {
            var added = api.AddElement(type, overview.Uuid.ToString(), string.Empty);
            elements.Add(api.GetStoryElement(added.Payload).Payload!);
        }

        var checkedPairs = 0;
        foreach (var element in elements)
        {
            foreach (var property in RequiredFieldGapScanner.GetMissingProperties(api, element))
            {
                foreach (var label in GapWorkflowOwnership.WorkflowsFor(element.ElementType, property))
                {
                    Assert.IsNotNull(WorkflowRegistry.Get(label),
                        $"{element.ElementType}.{property} names '{label}', which is not registered.");
                    checkedPairs++;
                }
            }
        }

        Assert.IsTrue(checkedPairs > 0, "the scan found no gap-to-workflow pairs to check");
    }

    [TestMethod]
    public void RegistryLabels_AreDistinct()
    {
        var labels = WorkflowRegistry.All.Select(w => w.Label).ToList();
        CollectionAssert.AllItemsAreUnique(labels);
    }

    [TestMethod]
    public void DeletedWorkflows_StayDeleted()
    {
        // #211. Re-adding one of these means re-adding it to the Worker table too, or the run
        // fails at the proxy rather than in the menu.
        foreach (var label in new[]
                 {
                     "ConflictBuilder", "GMC", "Structure", "BeatScenes", "SceneSummary",
                     "CastSceneRoles", "SceneDevelopment", "SceneConflict", "Sequel"
                 })
        {
            Assert.IsNull(WorkflowRegistry.Get(label), label);
        }
    }
}
