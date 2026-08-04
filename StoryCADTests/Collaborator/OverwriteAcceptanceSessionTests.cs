using StoryCollaborator.Models;
using StoryCollaborator.Services;

namespace StoryCADTests.Collaborator;

[TestClass]
public class OverwriteAcceptanceSessionTests
{
    private static PendingUpdate Make(string key, UpdateKind kind, string value = "proposed")
    {
        var parts = key.Split('.', 2);
        var label = parts.Length == 2 ? parts[0] : "Overview";
        var prop = parts.Length == 2 ? parts[1] : key;
        return new PendingUpdate(label, Guid.NewGuid(), new PropertySpec(prop), value, kind, "current");
    }

    [TestMethod]
    public void Partition_SplitsFreeAndProtect()
    {
        var pending = new List<PendingUpdate>
        {
            Make("Overview.Premise", UpdateKind.Fill),
            Make("Overview.Concept", UpdateKind.Protect),
            Make("Overview.Description", UpdateKind.Refresh),
        };

        var (free, protect) = OverwriteAcceptanceSession.Partition(pending);

        Assert.AreEqual(2, free.Count);
        Assert.AreEqual(1, protect.Count);
        Assert.AreEqual("Overview.Concept", protect[0].Key);
    }

    [TestMethod]
    public void StageProtect_OnlyProtect_AndIdempotent()
    {
        var session = new OverwriteAcceptanceSession();
        var p = Make("Problem.ProtGoal", UpdateKind.Protect);

        session.StageProtect(p);
        session.StageProtect(p);

        Assert.AreEqual(1, session.StagedCount);
        try
        {
            session.StageProtect(Make("Problem.Premise", UpdateKind.Fill));
            Assert.Fail("Expected ArgumentException for non-Protect stage");
        }
        catch (ArgumentException)
        {
            // expected
        }
    }

    [TestMethod]
    public void ShouldConfirmStaged_OnlyWhenQueueEmptyAndStaged()
    {
        var session = new OverwriteAcceptanceSession();
        session.StageProtect(Make("A.B", UpdateKind.Protect));

        Assert.IsFalse(session.ShouldConfirmStaged(remainingPendingCount: 2));
        Assert.IsTrue(session.ShouldConfirmStaged(remainingPendingCount: 0));

        session.ClearStage();
        Assert.IsFalse(session.ShouldConfirmStaged(remainingPendingCount: 0));
    }

    [TestMethod]
    public void BuildConfirmMessage_ListsKeys()
    {
        var msg = OverwriteAcceptanceSession.BuildConfirmMessage(new[]
        {
            Make("Problem.ProtGoal", UpdateKind.Protect),
            Make("Problem.ProtMotive", UpdateKind.Protect),
        });

        StringAssert.Contains(msg, "2 fields");
        StringAssert.Contains(msg, "Problem.ProtGoal");
        StringAssert.Contains(msg, "Problem.ProtMotive");
    }
}
