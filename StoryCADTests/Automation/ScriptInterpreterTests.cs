#if WINDOWS10_0_22621_0_OR_GREATER
using System.Reflection;
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
    // #1422: pick answers each picker by its title, whatever the order.
    private static (ScriptRunResult Result, List<string> Calls, List<string> Log) RunPick(
        string pickLine, params OpenDialog?[] dialogs)
    {
        var parsed = ScriptParser.Parse($"script \"Pick\"\nlaunch\n{pickLine}\n");
        Assert.IsTrue(parsed.Success, string.Join("; ", parsed.Errors));
        var driver = PickerDriver.Create(dialogs, out var calls);
        var log = new List<string>();
        var interpreter = new ScriptInterpreter(() => driver, new InterpreterOptions { Log = log.Add });
        return (interpreter.Execute(parsed), calls, log);
    }

    private static OpenDialog Picker(string key, string title)
        => new(key, new[] { title, "Click here to select an element type", "Leonard Kraskin", "Select", "Cancel" });

    [TestMethod]
    public void Execute_Pick_AnswersEachPickerByItsTitle()
    {
        var (result, calls, log) = RunPick(
            "pick outerproblem=\"Catching the drug dealer Lacas\" InnerProblem=\"Leonard's impulsiveness\" Partner=\"Tony Irwin\"",
            Picker("1", "Select OuterProblem"),
            Picker("2", "Change Protagonist"),
            Picker("3", "Select InnerProblem"));

        Assert.IsTrue(result.AllPassed, string.Join("; ", result.Outcomes.Select(o => o.Failure?.Message)));
        CollectionAssert.AreEqual(new[]
        {
            "WaitForDialogWith(CollabPickerList, , )",
            "SelectItem(CollabPickerList, Catching the drug dealer Lacas, )",
            "ResolveClickStrategy(PrimaryButton, )",
            "Invoke(PrimaryButton, )",
            "WaitForDialogWith(CollabPickerList, 1, )",
            "ResolveClickStrategy(PrimaryButton, )",
            "Invoke(PrimaryButton, )",
            "WaitForDialogWith(CollabPickerList, 2, )",
            "SelectItem(CollabPickerList, Leonard's impulsiveness, )",
            "ResolveClickStrategy(PrimaryButton, )",
            "Invoke(PrimaryButton, )",
            "WaitForDialogWith(CollabPickerList, 3, )",
        }, calls);
        Assert.IsTrue(log.Any(l => l.Contains("no picker asked for Partner", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void Execute_PickWithUnmappedSelectTitle_FailsNamingTheLabel()
    {
        var (result, _, _) = RunPick("pick Problem=\"Catching the drug dealer Lacas\"", Picker("1", "Select Antagonist"));

        Assert.IsFalse(result.AllPassed);
        StringAssert.Contains(result.Outcomes.Last().Failure!.Message, "Select Antagonist");
    }

    [TestMethod]
    public void Execute_PickWithNoPicker_Passes()
    {
        var (result, calls, _) = RunPick("pick Problem=\"Catching the drug dealer Lacas\"");

        Assert.IsTrue(result.AllPassed);
        CollectionAssert.AreEqual(new[] { "WaitForDialogWith(CollabPickerList, , )" }, calls);
    }

    [TestMethod]
    public void Execute_PickWithEndlessPickers_FailsAfterTheLimit()
    {
        var dialogs = Enumerable.Range(1, ScriptInterpreter.MaxPickers + 1)
            .Select(i => (OpenDialog?)Picker(i.ToString(), "Change Protagonist")).ToArray();

        var (result, _, _) = RunPick("pick Problem=\"Catching the drug dealer Lacas\"", dialogs);

        Assert.IsFalse(result.AllPassed);
    }

    [TestMethod]
    [DataRow("Select OuterProblem", false, "OuterProblem")]
    [DataRow("Change Protagonist", true, "Protagonist")]
    [DataRow("Select Setting element", false, "Setting element")]
    public void PickerTitle_WithTitleText_ReturnsVerbAndLabel(string title, bool change, string label)
    {
        var parsed = ScriptInterpreter.PickerTitle(new[] { "Select", title, "Select Leonard" });

        Assert.AreEqual((change, label), parsed);
    }

    [TestMethod]
    public void PickerTitle_WithoutTitleText_ReturnsNull()
    {
        Assert.IsNull(ScriptInterpreter.PickerTitle(new[] { "Select", "Cancel", "Click here to select an element type" }));
    }

    // #1422: expand on a Collaborator group row reaches the driver's idempotent Expand.
    [TestMethod]
    public void Execute_ExpandGroupRow_CallsDriverExpand()
    {
        var parsed = ScriptParser.Parse(
            "script \"Expand\"\nlaunch\nexpand WorkflowGroupProblemNavItem\ncollapse WorkflowGroupProblemNavItem\n");
        var driver = DriverCalls.Create(out var calls);
        var interpreter = new ScriptInterpreter(() => driver, new InterpreterOptions { Log = static _ => { } });

        Assert.IsTrue(interpreter.Execute(parsed).AllPassed);
        CollectionAssert.AreEqual(new[]
        {
            "Expand(WorkflowGroupProblemNavItem, )",
            "Collapse(WorkflowGroupProblemNavItem, )",
        }, calls);
    }
}

/// <summary>
///     A <see cref="DriverCalls" /> that also returns the given dialogs, one per
///     WaitForDialogWith call, then null (no more pickers).
/// </summary>
public class PickerDriver : DispatchProxy
{
    private List<string> _calls = null!;
    private Queue<OpenDialog?> _dialogs = null!;

    public static IUiDriver Create(IEnumerable<OpenDialog?> dialogs, out List<string> calls)
    {
        var driver = Create<IUiDriver, PickerDriver>();
        calls = new List<string>();
        var proxy = (PickerDriver)(object)driver;
        proxy._calls = calls;
        proxy._dialogs = new Queue<OpenDialog?>(dialogs);
        return driver;
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        _calls.Add($"{targetMethod!.Name}({string.Join(", ", args ?? Array.Empty<object?>())})");
        if (targetMethod.Name == nameof(IUiDriver.WaitForDialogWith))
        {
            return _dialogs.Count > 0 ? _dialogs.Dequeue() : null;
        }

        var type = targetMethod.ReturnType;
        return type.IsValueType && type != typeof(void) ? Activator.CreateInstance(type) : null;
    }
}
#endif
