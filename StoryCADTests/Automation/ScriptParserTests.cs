#if WINDOWS10_0_22621_0_OR_GREATER
using StoryCADAutomation.Scripting;

namespace StoryCADTests.Automation;

/// <summary>
///     The video statements and their placement rules (#1421 Milestone 5,
///     devdocs/issue_1421_video_design.md section 6).
/// </summary>
[TestClass]
public class ScriptParserTests
{
    private static ScriptParseResult Parse(params string[] lines)
        => ScriptParser.Parse("script \"Video test\"\n" + string.Join("\n", lines) + "\n");

    [TestMethod]
    public void Parse_WithAllVideoStatementsInPlace_Succeeds()
    {
        var result = Parse(
            "title \"A Five-Minute Introduction\"",
            "step \"Main window\" hold 2",
            "narrate \"This is the main window.\"",
            "launch",
            "step \"Recipe\"",
            "narrate \"An outline is a recipe.\"",
            "show image \"apple-pie.png\"",
            "step \"Free\"",
            "show text \"Free and open source\"",
            "closing \"Like and subscribe\"");

        Assert.IsTrue(result.Success, string.Join("; ", result.Errors));
        var verbs = result.Statements.Select(s => s.Verb).ToList();
        CollectionAssert.Contains(verbs, ScriptVerb.Title);
        CollectionAssert.Contains(verbs, ScriptVerb.ShowImage);
        CollectionAssert.Contains(verbs, ScriptVerb.ShowText);
        CollectionAssert.Contains(verbs, ScriptVerb.Closing);
    }

    [TestMethod]
    public void Parse_StepWithHold_StoresHoldSeconds()
    {
        var result = Parse("step \"Main window\" hold 2.5");

        Assert.IsTrue(result.Success, string.Join("; ", result.Errors));
        Assert.AreEqual(2.5, result.Statements[1].Seconds);
    }

    [TestMethod]
    public void Parse_StepWithoutHold_HasNoSeconds()
    {
        var result = Parse("step \"Main window\"");

        Assert.IsTrue(result.Success, string.Join("; ", result.Errors));
        Assert.IsNull(result.Statements[1].Seconds);
    }

    [TestMethod]
    [DataRow("step \"A\" hold 31")]
    [DataRow("step \"A\" hold -1")]
    [DataRow("step \"A\" hold soon")]
    [DataRow("step \"A\" wait 2")]
    public void Parse_StepWithBadHold_ReportsError(string line)
    {
        Assert.IsFalse(Parse(line).Success);
    }

    [TestMethod]
    public void Parse_ShowWithUnknownKind_ReportsError()
    {
        Assert.IsFalse(Parse("step \"A\"", "show video \"clip.mp4\"").Success);
    }

    [TestMethod]
    public void Parse_TitleAfterAStep_ReportsError()
    {
        Assert.IsFalse(Parse("step \"A\"", "title \"Late title\"").Success);
    }

    [TestMethod]
    public void Parse_ClosingBeforeTheEnd_ReportsError()
    {
        Assert.IsFalse(Parse("closing \"Bye\"", "step \"A\"", "launch").Success);
    }

    [TestMethod]
    public void Parse_NarrateAfterAnAction_ReportsError()
    {
        var result = Parse("step \"A\"", "launch", "narrate \"Too late.\"");

        Assert.AreEqual(1, result.Errors.Count);
        Assert.AreEqual(4, result.Errors[0].Line);
    }

    [TestMethod]
    public void Parse_NarrateBeforeAnyStep_Succeeds()
    {
        // Rule 3 applies inside a step; a script with no steps keeps its narrate lines anywhere.
        Assert.IsTrue(Parse("launch", "narrate \"No steps here.\"").Success);
    }

    [TestMethod]
    public void Parse_ShowStepWithAnAction_ReportsError()
    {
        var result = Parse("step \"A\"", "narrate \"Look.\"", "show text \"Hello\"", "pause 1");

        Assert.AreEqual(1, result.Errors.Count);
        Assert.AreEqual(5, result.Errors[0].Line);
    }

    [TestMethod]
    public void Parse_StepWithTwoShows_ReportsError()
    {
        Assert.IsFalse(Parse("step \"A\"", "show text \"One\"", "show text \"Two\"").Success);
    }

    [TestMethod]
    public void Parse_ShowOutsideAStep_ReportsError()
    {
        Assert.IsFalse(Parse("show text \"Hello\"").Success);
    }

    [TestMethod]
    public void Parse_ShowStepBeforeClosing_Succeeds()
    {
        // closing ends the last step, so it does not count as an action in that step.
        Assert.IsTrue(Parse("step \"A\"", "show text \"Hello\"", "closing \"Bye\"").Success);
    }
}
#endif
