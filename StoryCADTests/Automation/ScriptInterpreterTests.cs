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
}
#endif
