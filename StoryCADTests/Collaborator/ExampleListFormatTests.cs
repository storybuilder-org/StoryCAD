using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCADLib.Models;
using StoryCADLib.Models.Tools;
using StoryCADLib.Services.API;
using StoryCADLib.Services.Outline;
using StoryCADLib.ViewModels;
using StoryCollaborator;
using StoryCollaborator.Workflows;

#nullable disable

namespace StoryCADTests.Collaborator;

/// <summary>
///     Collaborator #77. EnrichWithExamples joined every list with ", ". Four Method values,
///     two Theme values and one Outcome value contain a comma, so the model could not tell
///     where one option ended. A live run returned the value "Survival (deliveranc", cut
///     mid-word. One value per line removes the ambiguity.
/// </summary>
[TestClass]
public class ExampleListFormatTests
{
    private static StoryCADApi CreateApi() => new(
        Ioc.Default.GetRequiredService<OutlineService>(),
        Ioc.Default.GetRequiredService<ListData>(),
        Ioc.Default.GetRequiredService<ControlData>(),
        Ioc.Default.GetRequiredService<ToolsData>());

    private static string Inject(string listName)
    {
        var model = new StoryModel();
        var api = CreateApi();
        api.CurrentModel = model;

        var workflow = new Workflow("ProblemBuilder", "Problem Builder", "test",
            StoryItemType.Problem, exampleLists: new List<string> { listName });

        var args = new Dictionary<string, string>();
        new WorkflowRunner(model, workflow, api).EnrichWithExamples(args);
        return args[$"{listName}_examples"];
    }

    [TestMethod]
    public void EnrichWithExamples_PutsOneValuePerLine()
    {
        var formatted = Inject("ProblemType");

        var lines = formatted.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.AreEqual(3, lines.Length,
            "ProblemType holds Conflict, Decision, Discover: three lines, not one joined string");
    }

    [TestMethod]
    public void EnrichWithExamples_KeepsCommaBearingValuesWhole()
    {
        var formatted = Inject("Method");

        // "Pleads his worth, loyalty or devotion" must survive as one option.
        var lines = formatted.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.TrimStart('-', ' ').Trim()).ToList();

        CollectionAssert.Contains(lines, "Pleads his worth, loyalty or devotion",
            "a value containing a comma must stay on one line, not split into two options");
    }

    [TestMethod]
    public void EnrichWithExamples_DropsTheComboBoxBlank()
    {
        // ListData inserts " " at index 0 of every list so a non-editable ComboBox can show an
        // empty selection (StoryCAD #1267). That blank must not reach the prompt.
        var formatted = Inject("ProblemType");

        foreach (var line in formatted.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            Assert.AreNotEqual("-", line.Trim(),
                "the ComboBox placeholder must not be offered to the model as an option");
        }
    }

    [TestMethod]
    public void EnrichWithExamples_MarksEachValueAsAnOption()
    {
        var formatted = Inject("ProblemType");

        foreach (var line in formatted.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            StringAssert.StartsWith(line.Trim(), "-",
                "each option is bulleted so the model reads a list, not prose");
        }
    }
}
