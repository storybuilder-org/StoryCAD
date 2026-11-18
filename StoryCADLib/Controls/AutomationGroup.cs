using Microsoft.UI.Xaml.Automation.Peers;

namespace StoryCADLib.Controls;

/// <summary>
///     A StackPanel that UI Automation can see. A plain panel has no automation peer, so its
///     AutomationId never reaches UI Automation and a script cannot frame the panel in a
///     screenshot. This one reports itself as a Group (#1422, the Collaborator Review Each card).
/// </summary>
public partial class AutomationGroup : StackPanel
{
    protected override AutomationPeer OnCreateAutomationPeer() => new AutomationGroupPeer(this);

    private sealed partial class AutomationGroupPeer(AutomationGroup owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;

        protected override string GetClassNameCore() => nameof(AutomationGroup);
    }
}
