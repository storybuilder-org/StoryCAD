using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCADLib.Services.Collaborator;
using StoryCADLib.Services.Store;

namespace StoryCADTests.Services.Collaborator;

[TestClass]
public class CollaboratorOpenPlannerTests
{
    [TestMethod]
    public void CollaboratorOpenPlanner_OpenUnderCap_ShowJoin()
    {
        var action = CollaboratorOpenPlanner.Decide(new BetaEnrollmentStatus(true, true, null), false);
        Assert.AreEqual(CollaboratorOpenAction.ShowJoin, action);
    }

    [TestMethod]
    public void CollaboratorOpenPlanner_PendingOpen_ShowJoin()
    {
        var action = CollaboratorOpenPlanner.Decide(new BetaEnrollmentStatus(true, false, "pending"), false);
        Assert.AreEqual(CollaboratorOpenAction.ShowJoin, action);
    }

    [TestMethod]
    public void CollaboratorOpenPlanner_PendingClosed_ShowClosed()
    {
        var action = CollaboratorOpenPlanner.Decide(new BetaEnrollmentStatus(false, true, "pending"), false);
        Assert.AreEqual(CollaboratorOpenAction.ShowClosed, action);
    }

    [TestMethod]
    public void CollaboratorOpenPlanner_ClosedNoPlans_ShowClosed()
    {
        var action = CollaboratorOpenPlanner.Decide(new BetaEnrollmentStatus(false, false, null), false);
        Assert.AreEqual(CollaboratorOpenAction.ShowClosed, action);
    }

    [TestMethod]
    public void CollaboratorOpenPlanner_ClosedHasPlans_ShowSubscribe()
    {
        var action = CollaboratorOpenPlanner.Decide(new BetaEnrollmentStatus(false, false, null), true);
        Assert.AreEqual(CollaboratorOpenAction.ShowSubscribe, action);
    }

    [TestMethod]
    public void CollaboratorOpenPlanner_ApprovedStatus_RefreshAllowlist()
    {
        var action = CollaboratorOpenPlanner.Decide(new BetaEnrollmentStatus(false, false, "approved"), false);
        Assert.AreEqual(CollaboratorOpenAction.RefreshAllowlist, action);
    }

    [TestMethod]
    public void CollaboratorOpenPlanner_Revoked_ShowRevoked()
    {
        var action = CollaboratorOpenPlanner.Decide(new BetaEnrollmentStatus(true, true, "revoked"), false);
        Assert.AreEqual(CollaboratorOpenAction.ShowRevoked, action);
    }

    [TestMethod]
    public void CollaboratorOpenPlanner_WindowsIsSupported_NotUsed()
    {
        var action = CollaboratorOpenPlanner.Decide(new BetaEnrollmentStatus(true, true, null), false);
        Assert.AreEqual(CollaboratorOpenAction.ShowJoin, action);
    }
}
