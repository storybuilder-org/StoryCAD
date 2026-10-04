#if WINDOWS10_0_22621_0_OR_GREATER
using StoryCADAutomation.Scripting;

namespace StoryCADTests.Automation;

/// <summary>
///     The lint rule that keeps file-dialog paths under {scratch} (#1421 review B1).
/// </summary>
[TestClass]
public class ScriptLinterTests
{
    private static readonly ScriptLinter Linter = new(new XamlUiFacts
    {
        AutomationIds = new HashSet<string>(),
        MenuLabels = new HashSet<string>(),
        TabLabels = new HashSet<string>(),
    });

    private static IReadOnlyList<ScriptDiagnostic> LintDialogLine(string line)
    {
        var parsed = ScriptParser.Parse($"script \"Lint test\"\n{line}\n");
        Assert.IsTrue(parsed.Success, string.Join("; ", parsed.Errors));
        return Linter.Lint(parsed.Statements).Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
    }

    [TestMethod]
    public void Lint_WithScratchRootedPath_ReportsNoError()
    {
        Assert.AreEqual(0, LintDialogLine("save-file-dialog \"{scratch}/Smoke.stbx\"").Count);
    }

    [TestMethod]
    public void Lint_WithPathOutsideScratch_ReportsError()
    {
        Assert.AreEqual(1, LintDialogLine("save-file-dialog \"C:/Users/Novel.stbx\"").Count);
    }

    [TestMethod]
    public void Lint_WithDriveAfterScratchRoot_ReportsError()
    {
        Assert.AreEqual(1, LintDialogLine("open-file-dialog \"{scratch}/C:/Users/Novel.stbx\"").Count);
    }

    [TestMethod]
    public void Lint_WithRepeatedScratchToken_ReportsError()
    {
        Assert.AreEqual(1, LintDialogLine("save-file-dialog \"{scratch}/{scratch}/Smoke.stbx\"").Count);
    }
}
#endif
