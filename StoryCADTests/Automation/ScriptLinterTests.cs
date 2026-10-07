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
    public void Lint_WithContentDialogButtonId_ReportsNoError()
    {
        Assert.AreEqual(0, LintDialogLine("click PrimaryButton").Count);
    }

    [TestMethod]
    public void Lint_WithContentDialogButtonIdOutsideClickOrDialog_ReportsError()
    {
        Assert.AreEqual(1, LintDialogLine("expect PrimaryButton exists").Count);
    }

    [TestMethod]
    public void Lint_WithMadeUpIdInDialog_ReportsError()
    {
        Assert.AreEqual(1, LintDialogLine("dialog \"Save changes?\" click NoSuchButton").Count);
    }

    [TestMethod]
    public void Lint_WithNativeOrContentDialogIdInDialog_ReportsNoError()
    {
        Assert.AreEqual(0, LintDialogLine("dialog \"Open\" click 1").Count);
        Assert.AreEqual(0, LintDialogLine("dialog \"Save changes?\" click SecondaryButton").Count);
    }

    [TestMethod]
    public void Lint_WithUnknownBareId_ReportsError()
    {
        Assert.AreEqual(1, LintDialogLine("click NoSuchButton").Count);
    }

    [TestMethod]
    public void Lint_WithRepeatedScratchToken_ReportsError()
    {
        Assert.AreEqual(1, LintDialogLine("save-file-dialog \"{scratch}/{scratch}/Smoke.stbx\"").Count);
    }

    [TestMethod]
    public void Lint_WithPlainScreenshotName_ReportsNoError()
    {
        Assert.AreEqual(0, LintDialogLine("screenshot \"Overview-Page.png\"").Count);
    }

    [TestMethod]
    public void Parse_WithScreenshotDialog_ProducesDialogCaptureStatement()
    {
        var parsed = ScriptParser.Parse("script \"Lint test\"\nscreenshot dialog \"File-Open-Dialog.png\"\n");

        Assert.IsTrue(parsed.Success, string.Join("; ", parsed.Errors));
        Assert.AreEqual(ScriptVerb.ScreenshotDialog, parsed.Statements[1].Verb);
        Assert.AreEqual("File-Open-Dialog.png", parsed.Statements[1].Text);
    }

    [TestMethod]
    public void Lint_WithScreenshotDialogPath_ReportsError()
    {
        Assert.AreEqual(1, LintDialogLine("screenshot dialog \"media/File-Open-Dialog.png\"").Count);
    }

    [TestMethod]
    [DataRow("apple-pie.png", 0)]
    [DataRow("Apple Pie.JPG", 0)]
    [DataRow("media/apple-pie.png", 1)]
    [DataRow("../apple-pie.png", 1)]
    [DataRow("apple-pie.gif", 1)]
    public void Lint_WithShowImageName_ReportsErrorOnlyForBadNames(string name, int errors)
    {
        Assert.AreEqual(errors, LintDialogLine($"step \"Recipe\"\nshow image \"{name}\"").Count);
    }

    [TestMethod]
    public void Lint_WithScreenshotPathOrWrongType_ReportsError()
    {
        Assert.AreEqual(1, LintDialogLine("screenshot \"../Overview.png\"").Count);
        Assert.AreEqual(1, LintDialogLine("screenshot \"media/Overview.png\"").Count);
        Assert.AreEqual(1, LintDialogLine("screenshot \"Overview.jpg\"").Count);
    }

    /// <summary>
    ///     Every committed script lints clean against the live XAML, the same rule as the
    ///     required CI `check`, so a XAML change that breaks a script fails here first.
    /// </summary>
    [TestMethod]
    public void Lint_AllCommittedScripts_ReportNoErrors()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "StoryCAD.sln")))
        {
            root = root.Parent;
        }

        Assert.IsNotNull(root, "StoryCAD.sln not found above the test output folder.");
        var scripts = Directory.GetFiles(Path.Combine(root.FullName, "StoryCADTests"), "*.scs", SearchOption.AllDirectories);
        Assert.IsTrue(scripts.Length > 0, "no committed .scs scripts found");

        var linter = new ScriptLinter(XamlUiFacts.LoadFromXamlScan());
        foreach (var script in scripts)
        {
            var parsed = ScriptParser.ParseFile(script);
            var errors = parsed.Errors.Concat(linter.Lint(parsed.Statements))
                .Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
            Assert.AreEqual(0, errors.Count, $"{script}: {string.Join("; ", errors)}");
        }
    }
}
#endif
