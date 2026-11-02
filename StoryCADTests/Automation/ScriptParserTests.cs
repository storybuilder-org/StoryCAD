#if WINDOWS10_0_22621_0_OR_GREATER
using StoryCADAutomation.Driver;
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

    // #1422: a tree row's expand state is asserted on the row itself.
    [TestMethod]
    [DataRow("expanded", ScriptVerb.ExpectExpanded)]
    [DataRow("collapsed", ScriptVerb.ExpectCollapsed)]
    public void Parse_ExpectExpandState_ProducesExpandVerb(string condition, ScriptVerb expected)
    {
        var result = Parse($"expect tree \"Danger Calls/Settings\" {condition}");

        Assert.IsTrue(result.Success, string.Join("; ", result.Errors));
        Assert.AreEqual(expected, result.Statements[1].Verb);
        Assert.AreEqual("Danger Calls/Settings", result.Statements[1].Target!.Value);
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

    // #1626 (Terry, 2026-10-20): a list on a card puts one item on each line.
    [TestMethod]
    public void Parse_ShowTextWithLineBreaks_SplitsTheCardIntoLines()
    {
        var result = Parse("step \"A\"", @"show text ""Story elements:\nProblem\nScene""");

        Assert.IsTrue(result.Success, string.Join("; ", result.Errors));
        Assert.AreEqual("Story elements:\nProblem\nScene", result.Statements[2].Text);
    }

    [TestMethod]
    public void Parse_PathWithBackslashN_KeepsBothCharacters()
    {
        // Only show text reads \n; a Windows path keeps its folder "\new".
        var result = Parse(@"set FileOpenProjectPathTextBox ""C:\new\Projects""");

        Assert.IsTrue(result.Success, string.Join("; ", result.Errors));
        Assert.AreEqual(@"C:\new\Projects", result.Statements[1].Text);
    }

    [TestMethod]
    public void Parse_ShowStepBeforeClosing_Succeeds()
    {
        // closing ends the last step, so it does not count as an action in that step.
        Assert.IsTrue(Parse("step \"A\"", "show text \"Hello\"", "closing \"Bye\"").Success);
    }

    // #1626: the caption clause gives the written form of a spoken narrate line.
    [TestMethod]
    public void Parse_NarrateWithCaption_StoresCaption()
    {
        var result = Parse("narrate \"The extension dot S-T-B-X.\" caption \"The extension .stbx.\"");

        Assert.IsTrue(result.Success, string.Join("; ", result.Errors));
        Assert.AreEqual("The extension dot S-T-B-X.", result.Statements[1].Text);
        Assert.AreEqual("The extension .stbx.", result.Statements[1].Caption);
    }

    [TestMethod]
    public void Parse_NarrateWithoutCaption_HasNoCaption()
    {
        var result = Parse("narrate \"Plain words.\"");

        Assert.IsTrue(result.Success, string.Join("; ", result.Errors));
        Assert.IsNull(result.Statements[1].Caption);
    }

    [TestMethod]
    [DataRow("narrate \"Spoken.\" subtitle \"Written.\"")]
    [DataRow("narrate \"Spoken.\" caption \"\"")]
    [DataRow("narrate \"Spoken.\" caption \"   \"")]
    [DataRow("narrate \"Spoken.\" caption")]
    [DataRow("narrate \"Spoken.\" caption \"Written.\" \"Extra.\"")]
    public void Parse_NarrateWithBadCaptionClause_ReportsError(string line)
    {
        var result = Parse(line);

        Assert.AreEqual(1, result.Errors.Count, string.Join("; ", result.Errors));
        Assert.AreEqual(2, result.Errors[0].Line);
    }

    // #1422 Window_Management: a stacked pane pushes the page out of view (offscreen), and a
    // hidden pane leaves UI Automation a moment after the click (gone waits for that).
    [TestMethod]
    [DataRow("offscreen", ScriptVerb.ExpectOffscreen)]
    [DataRow("gone", ScriptVerb.ExpectGone)]
    public void Parse_ExpectVisibilityCondition_ProducesVerb(string condition, ScriptVerb expected)
    {
        var result = Parse($"expect tree \"Danger Calls\" {condition}");

        Assert.IsTrue(result.Success, string.Join("; ", result.Errors));
        Assert.AreEqual(expected, result.Statements[1].Verb);
        Assert.AreEqual("Danger Calls", result.Statements[1].Target!.Value);
    }

    [TestMethod]
    [DataRow("window minimize", WindowAction.Minimize)]
    [DataRow("window maximize", WindowAction.Maximize)]
    [DataRow("window restore", WindowAction.Restore)]
    public void Parse_WindowState_ProducesWindowAction(string line, WindowAction expected)
    {
        var result = Parse(line);

        Assert.IsTrue(result.Success, string.Join("; ", result.Errors));
        Assert.AreEqual(ScriptVerb.Window, result.Statements[1].Verb);
        Assert.AreEqual(expected, result.Statements[1].WindowAction);
        Assert.IsNull(result.Statements[1].WindowSize);
    }

    [TestMethod]
    public void Parse_WindowSize_ProducesSize()
    {
        var result = Parse("window size 700x744");

        Assert.IsTrue(result.Success, string.Join("; ", result.Errors));
        Assert.AreEqual(WindowAction.Size, result.Statements[1].WindowAction);
        Assert.AreEqual(new System.Drawing.Size(700, 744), result.Statements[1].WindowSize);
    }

    [TestMethod]
    [DataRow("window")]
    [DataRow("window shrink")]
    [DataRow("window size")]
    [DataRow("window size 700")]
    [DataRow("window size 700x0")]
    [DataRow("window size -700x744")]
    [DataRow("window size 700x744x2")]
    [DataRow("window minimize now")]
    public void Parse_BadWindow_ReportsError(string line)
    {
        Assert.IsFalse(Parse(line).Success);
    }

    // wait <target> gone [condense]: a Collaborator run ends when its progress text goes (#1422).
    [TestMethod]
    public void Parse_WaitGone_SetsGoneAndOptionalCondense()
    {
        var result = Parse("wait ResponseProgressText gone", "wait ResponseProgressText gone condense", "wait ResponseProgressText");

        Assert.IsTrue(result.Success, string.Join("; ", result.Errors));
        Assert.IsTrue(result.Statements[1].Gone);
        Assert.IsFalse(result.Statements[1].Condense);
        Assert.IsTrue(result.Statements[2].Gone);
        Assert.IsTrue(result.Statements[2].Condense);
        Assert.IsFalse(result.Statements[3].Gone);
    }

    // click <target> pointer forces a real pointer click (#1422).
    [TestMethod]
    public void Parse_ClickWithPointer_SetsPointer()
    {
        var result = Parse("click TraitBuilderButton pointer", "click TraitBuilderButton");

        Assert.IsTrue(result.Success, string.Join("; ", result.Errors));
        Assert.IsTrue(result.Statements[1].Pointer);
        Assert.AreEqual("TraitBuilderButton", result.Statements[1].Target!.Value);
        Assert.IsFalse(result.Statements[2].Pointer);
    }

    [TestMethod]
    public void Parse_ClickWithUnknownWord_ReportsError()
    {
        Assert.IsFalse(Parse("click TraitBuilderButton gently").Success);
    }

    // window "title" ...: the app's top-level window of that title, the Collaborator window (#1422).
    [TestMethod]
    public void Parse_WindowWithTitleSize_ProducesTitleAndSize()
    {
        var result = Parse("window \"Story Collaborator\" size 1036x744");

        Assert.IsTrue(result.Success, string.Join("; ", result.Errors));
        Assert.AreEqual(ScriptVerb.Window, result.Statements[1].Verb);
        Assert.AreEqual("Story Collaborator", result.Statements[1].WindowTitle);
        Assert.AreEqual(WindowAction.Size, result.Statements[1].WindowAction);
        Assert.AreEqual(new System.Drawing.Size(1036, 744), result.Statements[1].WindowSize);
    }

    [TestMethod]
    [DataRow("window \"Story Collaborator\" minimize", WindowAction.Minimize)]
    [DataRow("window \"Story Collaborator\" maximize", WindowAction.Maximize)]
    [DataRow("window \"Story Collaborator\" restore", WindowAction.Restore)]
    public void Parse_WindowWithTitleState_ProducesTitleAndAction(string line, WindowAction expected)
    {
        var result = Parse(line);

        Assert.IsTrue(result.Success, string.Join("; ", result.Errors));
        Assert.AreEqual("Story Collaborator", result.Statements[1].WindowTitle);
        Assert.AreEqual(expected, result.Statements[1].WindowAction);
    }

    [TestMethod]
    public void Parse_WindowWithoutTitle_LeavesTitleNull()
    {
        var result = Parse("window size 700x744");

        Assert.IsNull(result.Statements[1].WindowTitle);
    }

    [TestMethod]
    [DataRow("window \"Story Collaborator\"")]
    [DataRow("window \"\" size 1036x744")]
    [DataRow("window \"Story Collaborator\" size")]
    [DataRow("window size \"Story Collaborator\" 1036x744")]
    [DataRow("window \"Story Collaborator\" \"Other\" minimize")]
    public void Parse_BadWindowWithTitle_ReportsError(string line)
    {
        Assert.IsFalse(Parse(line).Success);
    }

    [TestMethod]
    public void Parse_ScreenshotWindow_ProducesNameAndTitle()
    {
        var result = Parse("screenshot \"Collaborator-Window.png\" window \"Story Collaborator\"");

        Assert.IsTrue(result.Success, string.Join("; ", result.Errors));
        var s = result.Statements[1];
        Assert.AreEqual(ScriptVerb.Screenshot, s.Verb);
        Assert.AreEqual("Collaborator-Window.png", s.Text);
        Assert.AreEqual("Story Collaborator", s.WindowTitle);
        Assert.IsNull(s.CaptureTargets);
        Assert.IsNull(s.Crop);
    }

    [TestMethod]
    [DataRow("window")]
    [DataRow("window \"\"")]
    [DataRow("window StoryCollaborator")]
    [DataRow("window \"Story Collaborator\" crop 0,0,10,10")]
    [DataRow("window \"Story Collaborator\" of WorkflowAcceptAllButton")]
    [DataRow("of WorkflowAcceptAllButton window \"Story Collaborator\"")]
    public void Parse_BadScreenshotWindow_ReportsError(string tail)
    {
        Assert.IsFalse(Parse($"screenshot \"Collaborator-Window.png\" {tail}").Success);
    }
    // #1422: pick maps Collaborator picker labels to element names.
    [TestMethod]
    public void Parse_Pick_ProducesLabelMapIgnoringLabelCase()
    {
        var result = Parse("pick OuterProblem=\"Catching the drug dealer Lacas\" Protagonist=\"Leonard Kraskin\"");

        Assert.IsTrue(result.Success, string.Join("; ", result.Errors));
        var s = result.Statements[1];
        Assert.AreEqual(ScriptVerb.Pick, s.Verb);
        Assert.AreEqual(2, s.Picks!.Count);
        Assert.AreEqual("Catching the drug dealer Lacas", s.Picks["outerproblem"]);
        Assert.AreEqual("Leonard Kraskin", s.Picks["Protagonist"]);
    }

    [TestMethod]
    [DataRow("pick")]
    [DataRow("pick Protagonist")]
    [DataRow("pick Protagonist=")]
    [DataRow("pick =\"Leonard Kraskin\"")]
    [DataRow("pick Protagonist=\"\"")]
    [DataRow("pick \"Leonard Kraskin\"")]
    [DataRow("pick Main-Character=\"Leonard Kraskin\"")]
    [DataRow("pick Protagonist=\"Leonard Kraskin\" protagonist=\"Tony Irwin\"")]
    public void Parse_BadPick_ReportsError(string line)
    {
        Assert.IsFalse(Parse(line).Success);
    }

    // #1422: expand and collapse take any target, so a Collaborator group row works too.
    [TestMethod]
    [DataRow("expand", ScriptVerb.Expand)]
    [DataRow("collapse", ScriptVerb.Collapse)]
    public void Parse_ExpandAutomationId_ProducesIdTarget(string verb, ScriptVerb expected)
    {
        var result = Parse($"{verb} WorkflowGroupProblemNavItem");

        Assert.IsTrue(result.Success, string.Join("; ", result.Errors));
        Assert.AreEqual(expected, result.Statements[1].Verb);
        Assert.AreEqual(ElementAddressKind.AutomationId, result.Statements[1].Target!.Kind);
        Assert.AreEqual("WorkflowGroupProblemNavItem", result.Statements[1].Target!.Value);
    }
}
#endif
