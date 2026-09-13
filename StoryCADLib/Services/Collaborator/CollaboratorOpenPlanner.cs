using StoryCADLib.Services.Store;

namespace StoryCADLib.Services.Collaborator;

internal enum CollaboratorOpenAction
{
    ShowJoin,
    RefreshAllowlist,
    ShowClosed,
    ShowSubscribe,
    ShowRevoked
}

internal static class CollaboratorOpenPlanner
{
    public static CollaboratorOpenAction Decide(BetaEnrollmentStatus enrollment, bool hasPlans)
    {
        if (enrollment.Status == "revoked")
        {
            return CollaboratorOpenAction.ShowRevoked;
        }

        if (enrollment.Status == "approved")
        {
            return CollaboratorOpenAction.RefreshAllowlist;
        }

        var pendingJoin = enrollment.Status == "pending" && enrollment.Open;
        var newJoin = string.IsNullOrEmpty(enrollment.Status) && enrollment.Open && enrollment.UnderCap;
        if (pendingJoin || newJoin)
        {
            return CollaboratorOpenAction.ShowJoin;
        }

        if (hasPlans)
        {
            return CollaboratorOpenAction.ShowSubscribe;
        }

        return CollaboratorOpenAction.ShowClosed;
    }
}
