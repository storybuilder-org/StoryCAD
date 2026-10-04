#if WINDOWS10_0_22621_0_OR_GREATER
using StoryCADAutomation.Driver;
using StoryCADAutomation.Interpreting;

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
}
#endif
