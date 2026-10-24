#if WINDOWS10_0_22621_0_OR_GREATER
using StoryCADAutomation.Interpreting;
using StoryCADAutomation.Scripting;
using StoryCADAutomation.Video;
using StoryCADAutomation.Voice;

namespace StoryCADTests.Automation;

/// <summary>
///     `wait &lt;target&gt; condense` (#1626, Terry, 2026-10-24): the parse, the recorded wait, the
///     cut arithmetic, and how the cut moves clips, cards, captions and the body length.
/// </summary>
[TestClass]
public class CutsTests
{
    private static TimeSpan S(double seconds) => TimeSpan.FromSeconds(seconds);

    private static (TimeSpan, TimeSpan) Span(double start, double end) => (S(start), S(end));

    private static readonly (TimeSpan, TimeSpan)[] NoClips = Array.Empty<(TimeSpan, TimeSpan)>();

    [TestMethod]
    public void Parse_WaitWithCondense_SetsCondense()
    {
        var result = ScriptParser.Parse("script \"Wait\"\nwait WorkflowAcceptAllButton condense\nwait WorkflowAcceptAllButton\n");

        Assert.IsTrue(result.Success, string.Join("; ", result.Errors));
        Assert.IsTrue(result.Statements[1].Condense);
        Assert.AreEqual("WorkflowAcceptAllButton", result.Statements[1].Target!.Value);
        Assert.IsFalse(result.Statements[2].Condense);
    }

    [TestMethod]
    [DataRow("wait WorkflowAcceptAllButton slowly")]
    [DataRow("wait WorkflowAcceptAllButton condense 60")]
    public void Parse_WaitWithBadCondenseClause_ReportsError(string line)
    {
        var result = ScriptParser.Parse($"script \"Wait\"\n{line}\n");

        Assert.AreEqual(1, result.Errors.Count, string.Join("; ", result.Errors));
    }

    [TestMethod]
    public void Lint_WaitWithCondense_ChecksTheTargetAsForWait()
    {
        var linter = new ScriptLinter(new XamlUiFacts
        {
            AutomationIds = new HashSet<string> { "WorkflowAcceptAllButton" },
            MenuPaths = new HashSet<string>(),
            TabLabels = new HashSet<string>(),
        });
        var good = ScriptParser.Parse("script \"Wait\"\nwait WorkflowAcceptAllButton condense\n");
        var bad = ScriptParser.Parse("script \"Wait\"\nwait NoSuchButton condense\n");

        Assert.AreEqual(0, linter.Lint(good.Statements).Count);
        Assert.AreEqual(1, linter.Lint(bad.Statements).Count(d => d.Severity == DiagnosticSeverity.Error));
    }

    [TestMethod]
    public void Condense_InPresentationProfile_RecordsTheWait()
    {
        var now = S(10);
        var profile = new PresentationProfile(1.0, () => now, t => now += t, null);

        profile.Condense(() => now += S(90));

        Assert.AreEqual(new StepSpan(S(10), S(100)), profile.CondensedWaits.Single());
    }

    [TestMethod]
    public void Condense_InTestProfile_JustWaits()
    {
        var waited = false;

        ((IExecutionProfile)new TestProfile()).Condense(() => waited = true);

        Assert.IsTrue(waited);
    }

    [TestMethod]
    public void InterpreterOptions_CondenseTimeout_Is5Minutes()
    {
        Assert.AreEqual(TimeSpan.FromMinutes(5), new InterpreterOptions().CondenseTimeout);
    }

    [TestMethod]
    public void FromWaits_WithoutClips_KeepsOneAndAHalfSecondsAtEachEnd()
    {
        var cuts = Cuts.FromWaits(new[] { Span(10, 100) }, NoClips, S(200));

        Assert.AreEqual(new Cut(S(11.5), S(98.5)), cuts.Single());
    }

    [TestMethod]
    public void FromWaits_WithClipPlayingAtTheStart_StartsTheCutWhenTheClipStops()
    {
        var cuts = Cuts.FromWaits(new[] { Span(10, 100) }, new[] { Span(5, 20) }, S(200));

        Assert.AreEqual(new Cut(S(20), S(98.5)), cuts.Single());
    }

    [TestMethod]
    public void FromWaits_WithClipStartingInTheWait_EndsTheCutWhenTheClipStarts()
    {
        var cuts = Cuts.FromWaits(new[] { Span(10, 100) }, new[] { Span(60, 65) }, S(200));

        Assert.AreEqual(new Cut(S(11.5), S(60)), cuts.Single());
    }

    [TestMethod]
    public void FromWaits_WithClipLongerThanTheWait_DoesNotCut()
    {
        Assert.AreEqual(0, Cuts.FromWaits(new[] { Span(10, 20) }, new[] { Span(8, 30) }, S(200)).Count);
    }

    [TestMethod]
    public void FromWaits_WithWaitShorterThanBothEnds_DoesNotCut()
    {
        Assert.AreEqual(0, Cuts.FromWaits(new[] { Span(10, 12.5) }, NoClips, S(200)).Count);
    }

    [TestMethod]
    public void FromWaits_WithWaitPastTheBodyEnd_StopsTheCutAtTheEnd()
    {
        Assert.AreEqual(new Cut(S(11.5), S(50)), Cuts.FromWaits(new[] { Span(10, 100) }, NoClips, S(50)).Single());
    }

    [TestMethod]
    public void FromWaits_WithTwoWaits_GivesTwoCutsInOrder()
    {
        var cuts = Cuts.FromWaits(new[] { Span(100, 160), Span(10, 40) }, NoClips, S(300));

        CollectionAssert.AreEqual(new[] { new Cut(S(11.5), S(38.5)), new Cut(S(101.5), S(158.5)) }, cuts.ToList());
    }

    [TestMethod]
    [DataRow(5.0, 5.0)]     // before both cuts
    [DataRow(20.0, 11.5)]   // inside the first cut: where the cut is
    [DataRow(50.0, 23.0)]   // after the first cut (27 s)
    [DataRow(120.0, 74.5)]  // inside the second cut
    [DataRow(200.0, 116.0)] // after both cuts (27 s + 57 s)
    public void Map_WithTwoCuts_MovesTimesEarlierByTheCutsBefore(double recording, double body)
    {
        var cuts = new[] { new Cut(S(11.5), S(38.5)), new Cut(S(101.5), S(158.5)) };

        Assert.AreEqual(body, Cuts.Map(S(recording), cuts).TotalSeconds, 0.0001);
    }

    [TestMethod]
    public void SelectFilter_WithCuts_DropsTheirFramesAndClosesTheGaps()
    {
        var filter = Cuts.SelectFilter(new[] { new Cut(S(11.5), S(38.5)), new Cut(S(101.5), S(158.5)) });

        Assert.AreEqual(",select='not(between(t,11.5,38.5)+between(t,101.5,158.5))',setpts=N/FRAME_RATE/TB", filter);
        Assert.AreEqual(string.Empty, Cuts.SelectFilter(Array.Empty<Cut>()));
    }

    [TestMethod]
    public void BodyArguments_WithCuts_SelectsAfterFpsAndKeepsAudioUncut()
    {
        var plan = new VideoPlan
        {
            RecordingPath = "rec.mkv", RecordingLength = S(20), Cuts = new[] { new Cut(S(11.5), S(38.5)) },
            Clips = new[] { new ClipPlacement("a.wav", S(12)) },
        };

        var args = VideoAssembler.BodyArguments(plan, "body.mp4").ToList();
        var filter = args[args.IndexOf("-filter_complex") + 1];

        StringAssert.StartsWith(filter,
            "[0:v]scale=1920:1080:flags=lanczos,fps=30,select='not(between(t,11.5,38.5))',setpts=N/FRAME_RATE/TB,setsar=1[v0]");
        StringAssert.Contains(filter, "adelay=12000:all=1");
        Assert.AreEqual("20", args[args.IndexOf("-t") + 1]);
    }

    [TestMethod]
    public void BodyArguments_WithoutCuts_HasNoSelect()
    {
        var plan = new VideoPlan { RecordingPath = "rec.mkv", RecordingLength = S(20) };

        var args = VideoAssembler.BodyArguments(plan, "body.mp4").ToList();

        Assert.IsFalse(args[args.IndexOf("-filter_complex") + 1].Contains("select="));
    }

    [TestMethod]
    public void Plan_WithCondensedWait_MovesClipsCardsCaptionsAndBodyLength()
    {
        var folder = Path.Combine(Path.GetTempPath(), "StoryCADAutomation", "cuts-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            File.WriteAllText(Path.Combine(folder, VoiceClips.ClipName("voice", "Ideation starts.")), "wav");
            File.WriteAllText(Path.Combine(folder, VoiceClips.ClipName("voice", "The run is done.")), "wav");
            var now = TimeSpan.Zero;
            var profile = new PresentationProfile(1.0, () => now, t => now += t, _ => S(3));

            // Step A at 0 s: its clip plays 0-3 s while a 100 s wait runs from 1 s to 101 s.
            profile.BeginStep(0);
            profile.Narrate("Ideation starts.", _ => { });
            now = S(1);
            profile.Condense(() => now = S(101));
            // Step B at 101 s: a clip at 101-104 s, then a card step at 104-107 s.
            profile.BeginStep(0);
            profile.Narrate("The run is done.", _ => { });
            profile.BeginStep(0);
            profile.Narrate("Card words.", _ => { });
            profile.EndStep();
            var parsed = ScriptParser.Parse(
                "script \"Cuts\"\nstep \"A\"\nnarrate \"Ideation starts.\"\nstep \"B\"\nnarrate \"The run is done.\"\n" +
                "step \"C\"\nnarrate \"Card words.\"\nshow text \"Done\"\n");

            var plan = VideoCommand.Plan(parsed, profile, TimeSpan.Zero, "rec.mkv", S(110), folder, null, "voice", folder);

            // The cut starts when the first clip stops (3 s) and ends 1.5 s before the wait ends (99.5 s).
            Assert.AreEqual(new Cut(S(3), S(99.5)), plan.Cuts.Single());
            Assert.AreEqual(0, plan.Clips[0].Start.TotalSeconds, 0.0001);
            Assert.AreEqual(101 - 96.5, plan.Clips[1].Start.TotalSeconds, 0.0001);
            Assert.AreEqual(104 - 96.5, plan.Cards.Single().Start.TotalSeconds, 0.0001);
            Assert.AreEqual(110 - 96.5, plan.RecordingLength.TotalSeconds, 0.0001);
            var captions = Captions.ForPlan(plan);
            Assert.AreEqual(101 - 96.5, captions.Single(c => c.Text == "The run is done.").Start.TotalSeconds, 0.0001);
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }
}
#endif
