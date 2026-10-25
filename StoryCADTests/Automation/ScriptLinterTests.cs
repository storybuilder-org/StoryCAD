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
        MenuPaths = new HashSet<string>(),
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
    [DataRow("click WorkflowPremiseNavItem")]
    [DataRow("click WorkflowPremiseStarButton")]
    [DataRow("expect WorkflowPremiseCheck exists")]
    [DataRow("expand WorkflowGroupOverviewNavItem")]
    [DataRow("expect WorkflowOutlineGapsNavItem exists")]
    [DataRow("expect WorkflowSettingsTersenessCombo exists")]
    public void Lint_WithCollaboratorIdBuiltInCSharp_ReportsNoError(string line)
    {
        Assert.AreEqual(0, LintDialogLine(line).Count);
    }

    [TestMethod]
    [DataRow("click WorkflowPremiseRow")]
    [DataRow("expect WorkflowSettingsTersenessDropDown exists")]
    public void Lint_WithCollaboratorLikeIdOfWrongShape_ReportsError(string line)
    {
        Assert.AreEqual(1, LintDialogLine(line).Count);
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
    [DataRow("Character-Role-Tab.png", 0)]
    [DataRow("Preferences-Backup-Tab-Mac.png", 0)]
    [DataRow("File-Open-Dialog.png", 0)]
    [DataRow("DevTab.png", 1)]
    [DataRow("Danger-Calls-after-Scene-Move.png", 1)]
    [DataRow("file_open.png", 1)]
    public void Lint_WithScreenshotName_WarnsWhenNotManualImageStandard(string name, int warnings)
    {
        var parsed = ScriptParser.Parse($"script \"Lint test\"\nscreenshot \"{name}\"\n");
        var found = Linter.Lint(parsed.Statements).Where(d => d.Severity == DiagnosticSeverity.Warning).ToList();

        Assert.AreEqual(warnings, found.Count);
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
    public void Parse_WithScreenshotCrop_ProducesCropRectangle()
    {
        var parsed = ScriptParser.Parse("script \"Lint test\"\nscreenshot \"File-Menu.png\" crop 0,40,467,311\n");

        Assert.IsTrue(parsed.Success, string.Join("; ", parsed.Errors));
        Assert.AreEqual(new System.Drawing.Rectangle(0, 40, 467, 311), parsed.Statements[1].Crop);
    }

    [TestMethod]
    [DataRow("crop 0,0,467")]
    [DataRow("crop 0,0,0,311")]
    [DataRow("crop -1,0,467,311")]
    [DataRow("crop 0,0,467,311.5")]
    [DataRow("trim 0,0,467,311")]
    public void Parse_WithBadScreenshotCrop_ReportsError(string tail)
    {
        var parsed = ScriptParser.Parse($"script \"Lint test\"\nscreenshot \"File-Menu.png\" {tail}\n");

        Assert.IsFalse(parsed.Success);
    }

    [TestMethod]
    public void Parse_WithScreenshotOfOneTarget_ProducesTargetAndZeroMargin()
    {
        var parsed = ScriptParser.Parse("script \"Lint test\"\nscreenshot \"Status-Bar.png\" of BackupStatusButton\n");

        Assert.IsTrue(parsed.Success, string.Join("; ", parsed.Errors));
        var s = parsed.Statements[1];
        Assert.AreEqual(ScriptVerb.Screenshot, s.Verb);
        Assert.IsNull(s.Crop);
        Assert.AreEqual(1, s.CaptureTargets!.Count);
        Assert.AreEqual("BackupStatusButton", s.CaptureTargets[0].Value);
        Assert.AreEqual(0, s.CaptureMargin);
    }

    [TestMethod]
    public void Parse_WithScreenshotOfSeveralTargetsAndMargin_ProducesAllTargetsAndMargin()
    {
        var parsed = ScriptParser.Parse(
            "script \"Lint test\"\nscreenshot \"File-Menu.png\" of FileMenuButton tree \"Story\" name:\"Exit\" margin 12\n");

        Assert.IsTrue(parsed.Success, string.Join("; ", parsed.Errors));
        var targets = parsed.Statements[1].CaptureTargets!;
        Assert.AreEqual(3, targets.Count);
        Assert.AreEqual(StoryCADAutomation.Driver.ElementAddressKind.AutomationId, targets[0].Kind);
        Assert.AreEqual(StoryCADAutomation.Driver.ElementAddressKind.TreePath, targets[1].Kind);
        Assert.AreEqual(StoryCADAutomation.Driver.ElementAddressKind.Name, targets[2].Kind);
        Assert.AreEqual(12, parsed.Statements[1].CaptureMargin);
    }

    [TestMethod]
    [DataRow("of")]
    [DataRow("of margin 8")]
    [DataRow("of FileMenuButton margin")]
    [DataRow("of FileMenuButton margin -1")]
    [DataRow("of FileMenuButton margin 2.5")]
    [DataRow("of FileMenuButton margin 8 ExitMenuItem")]
    [DataRow("of \"FileMenuButton\"")]
    [DataRow("crop 0,40,467,311 of FileMenuButton")]
    [DataRow("of FileMenuButton crop 0,40,467,311")]
    public void Parse_WithBadScreenshotOf_ReportsError(string tail)
    {
        var parsed = ScriptParser.Parse($"script \"Lint test\"\nscreenshot \"File-Menu.png\" {tail}\n");

        Assert.IsFalse(parsed.Success);
    }

    [TestMethod]
    public void Lint_WithUnknownScreenshotOfTarget_ReportsError()
    {
        Assert.AreEqual(1, LintDialogLine("screenshot \"File-Menu.png\" of NoSuchButton").Count);
    }

    [TestMethod]
    public void Lint_WithKnownScreenshotOfTargets_ReportsNoError()
    {
        var linter = new ScriptLinter(new XamlUiFacts
        {
            AutomationIds = new HashSet<string> { "FileMenuButton", "ExitMenuItem" },
            MenuPaths = new HashSet<string>(),
            TabLabels = new HashSet<string>(),
        });
        var parsed = ScriptParser.Parse(
            "script \"Lint test\"\nscreenshot \"File-Menu.png\" of FileMenuButton ExitMenuItem margin 12\n");

        Assert.AreEqual(0, linter.Lint(parsed.Statements).Count(d => d.Severity == DiagnosticSeverity.Error));
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

    // #1421 review M3: a menu text path is linted as a whole path of Windows UIA names.
    private static readonly ScriptLinter MenuLinter = new(new XamlUiFacts
    {
        AutomationIds = new HashSet<string>(),
        MenuPaths = new HashSet<string>
        {
            XamlUiFacts.MenuPathKey(new[] { "File" }),
            XamlUiFacts.MenuPathKey(new[] { "File", "Save Story" }),
            XamlUiFacts.MenuPathKey(new[] { "File", "Quit" }),
            XamlUiFacts.MenuPathKey(new[] { "Tools" }),
            XamlUiFacts.MenuPathKey(new[] { "Tools", "Plotting Aids" }),
            XamlUiFacts.MenuPathKey(new[] { "Tools", "Plotting Aids", "Master Plots                Ctrl+M" }),
        },
        TabLabels = new HashSet<string>(),
    });

    private static IReadOnlyList<ScriptDiagnostic> LintMenuLine(ScriptLinter linter, string path)
    {
        var parsed = ScriptParser.Parse($"script \"Lint test\"\nmenu \"{path}\"\n");
        Assert.IsTrue(parsed.Success, string.Join("; ", parsed.Errors));
        return linter.Lint(parsed.Statements).Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
    }

    [TestMethod]
    [DataRow("File/Quit")]
    [DataRow("File/Save Story")]
    [DataRow("Tools/Plotting Aids/Master Plots")]
    [DataRow("File")]
    public void LintMenuPath_WithWindowsPath_ReportsNoError(string path)
    {
        Assert.AreEqual(0, LintMenuLine(MenuLinter, path).Count);
    }

    [TestMethod]
    public void LintMenuPath_WithExitInsteadOfQuit_ReportsErrorNamingQuit()
    {
        var errors = LintMenuLine(MenuLinter, "File/Exit");

        Assert.AreEqual(1, errors.Count);
        StringAssert.Contains(errors[0].Message, "Quit");
    }

    [TestMethod]
    [DataRow("Tools/Save Story")]
    [DataRow("Master Plots")]
    [DataRow("Tools/Master Plots")]
    public void LintMenuPath_WithLabelUnderWrongParent_ReportsError(string path)
    {
        Assert.AreEqual(1, LintMenuLine(MenuLinter, path).Count);
    }

    [TestMethod]
    public void LintMenuPath_AgainstLiveXaml_RejectsExitAndAcceptsQuit()
    {
        var linter = new ScriptLinter(XamlUiFacts.LoadFromXamlScan());

        Assert.AreEqual(1, LintMenuLine(linter, "File/Exit").Count);
        Assert.AreEqual(0, LintMenuLine(linter, "File/Quit").Count);
        Assert.AreEqual(0, LintMenuLine(linter, "Tools/Plotting Aids/Master Plots").Count);
    }

    // #1422: the runner opens a menu item through its XAML owners and never searches menus.
    [TestMethod]
    [DataRow("SaveStoryMenuItem", 0)]
    [DataRow("MasterPlotsMenuItem", 0)]
    [DataRow("ContextAddCharacterMenuItem", 1)]
    [DataRow("FileMenuButton", 1)]
    public void LintMenuLeaf_AgainstLiveXaml_AcceptsOnlyToolbarMenuItems(string id, int expectedErrors)
    {
        var linter = new ScriptLinter(XamlUiFacts.LoadFromXamlScan());
        var parsed = ScriptParser.Parse($"script \"Lint test\"\nmenu {id}\n");
        Assert.IsTrue(parsed.Success, string.Join("; ", parsed.Errors));

        var errors = linter.Lint(parsed.Statements).Where(d => d.Severity == DiagnosticSeverity.Error).ToList();

        Assert.AreEqual(expectedErrors, errors.Count, string.Join("; ", errors));
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

    // #1626: one narrate text has one clip, so it has one caption.
    [TestMethod]
    [DataRow("caption \"Written A.\"", "caption \"Written B.\"", 1)]
    [DataRow("caption \"Written A.\"", "", 1)]
    [DataRow("caption \"Written A.\"", "caption \"Written A.\"", 0)]
    [DataRow("", "", 0)]
    public void Lint_WithSameNarrateTextTwice_ReportsDifferentCaptions(string first, string second, int errors)
    {
        var parsed = ScriptParser.Parse($"script \"Lint test\"\nnarrate \"Spoken.\" {first}\nnarrate \"Spoken.\" {second}\n");
        Assert.IsTrue(parsed.Success, string.Join("; ", parsed.Errors));

        var found = Linter.Lint(parsed.Statements).Where(d => d.Severity == DiagnosticSeverity.Error).ToList();

        Assert.AreEqual(errors, found.Count, string.Join("; ", found));
        Assert.IsTrue(found.All(d => d.Line == 3));
    }

    // The CommandBar "..." button: a WinUI template part the XAML scan never sees (#1422).
    [TestMethod]
    [DataRow("click MoreButton")]
    [DataRow("expect MoreButton gone")]
    public void Lint_WithCommandBarMoreButton_ReportsNoError(string line)
    {
        Assert.AreEqual(0, LintDialogLine(line).Count);
    }
}
#endif
