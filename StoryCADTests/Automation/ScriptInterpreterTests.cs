#if WINDOWS10_0_22621_0_OR_GREATER
using StoryCADAutomation.Driver;
using StoryCADAutomation.Interpreting;
using StoryCADAutomation.Scripting;

namespace StoryCADTests.Automation;

/// <summary>
///     Run-time scratch containment for file-dialog paths (#1421 review B1). No app is launched.
/// </summary>
[TestClass]
public class ScriptInterpreterTests
{
    private static readonly string Scratch = Path.Combine(Path.GetTempPath(), "StoryCADAutomation", "run-test");

    [TestMethod]
    public void ResolveScratchPath_WithPathUnderScratch_ReturnsFullPath()
    {
        var resolved = ScriptInterpreter.ResolveScratchPath("{scratch}/outlines/Smoke.stbx", Scratch);

        Assert.AreEqual(Path.Combine(Scratch, "outlines", "Smoke.stbx"), resolved);
    }

    [TestMethod]
    public void ResolveScratchPath_WithDotDotEscape_ThrowsAutomationStepException()
    {
        Assert.ThrowsExactly<AutomationStepException>(
            () => ScriptInterpreter.ResolveScratchPath("{scratch}/../Smoke.stbx", Scratch));
    }

    [TestMethod]
    public void ResolveScratchPath_WithSiblingFolderSharingPrefix_ThrowsAutomationStepException()
    {
        // "run-test2" starts with "run-test"; the check must compare whole folder names.
        Assert.ThrowsExactly<AutomationStepException>(
            () => ScriptInterpreter.ResolveScratchPath("{scratch}/../run-test2/Smoke.stbx", Scratch));
    }

    [TestMethod]
    public void ResolveScratchPath_WithoutScratchToken_ThrowsAutomationStepException()
    {
        var userPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Novel.stbx");

        Assert.ThrowsExactly<AutomationStepException>(
            () => ScriptInterpreter.ResolveScratchPath(userPath, Scratch));
    }

    // #1422 evaluation: --keep-going skips the rest of a failed step and runs the next step.
    // No launch statement, so every expect fails with "no app is running".
    private static IReadOnlyList<StatementStatus> RunWithoutApp(bool keepGoing)
    {
        var parsed = ScriptParser.Parse(
            "script \"Keep going\"\nstep \"A\"\nexpect FileMenuButton exists\nexpect ViewSelectorCombo exists\n" +
            "step \"B\"\nexpect StatusMessageText exists\n");
        Assert.IsTrue(parsed.Success, string.Join("; ", parsed.Errors));
        var interpreter = new ScriptInterpreter(
            () => throw new InvalidOperationException("no launch in this test"),
            new InterpreterOptions { KeepGoing = keepGoing, Log = static _ => { } });

        return interpreter.Execute(parsed).Outcomes.Select(o => o.Status).ToList();
    }

    [TestMethod]
    public void Execute_WithKeepGoing_SkipsRestOfFailedStepAndRunsNextStep()
    {
        var statuses = RunWithoutApp(keepGoing: true);

        CollectionAssert.AreEqual(new[]
        {
            StatementStatus.Passed, StatementStatus.Passed, StatementStatus.Failed, StatementStatus.Skipped,
            StatementStatus.Passed, StatementStatus.Failed,
        }, statuses.ToArray());
    }

    [TestMethod]
    public void Execute_WithoutKeepGoing_SkipsEverythingAfterFirstFailure()
    {
        var statuses = RunWithoutApp(keepGoing: false);

        CollectionAssert.AreEqual(new[]
        {
            StatementStatus.Passed, StatementStatus.Passed, StatementStatus.Failed, StatementStatus.Skipped,
            StatementStatus.Skipped, StatementStatus.Skipped,
        }, statuses.ToArray());
    }

    // #1422: window and screenshot statements with a title reach the titled-window driver calls.
    [TestMethod]
    public void Execute_WithTitledWindowStatements_CallsTitledWindowDriverMethods()
    {
        var parsed = ScriptParser.Parse(
            "script \"Collaborator window\"\nlaunch\n" +
            "window \"Story Collaborator\" size 1036x744\n" +
            "window \"Story Collaborator\" minimize\n" +
            "window size 1036x744\n" +
            "screenshot \"Collaborator-Window.png\" window \"Story Collaborator\"\n");
        Assert.IsTrue(parsed.Success, string.Join("; ", parsed.Errors));
        var driver = DriverCalls.Create(out var calls);
        var output = Path.Combine(Path.GetTempPath(), "StoryCADAutomation", "titled-window-test");
        var interpreter = new ScriptInterpreter(
            () => driver, new InterpreterOptions { OutputDirectory = output, Log = static _ => { } });

        var result = interpreter.Execute(parsed);

        Assert.IsTrue(result.AllPassed);
        CollectionAssert.AreEqual(new[]
        {
            "SizeWindow(Story Collaborator, 1036, 744)",
            "SetWindowState(Story Collaborator, Minimize)",
            "SizeMainWindow(1036, 744)",
            $"CaptureWindow({Path.Combine(Path.GetFullPath(output), "Collaborator-Window.png")}, Story Collaborator)",
        }, calls);
    }
}
#endif
