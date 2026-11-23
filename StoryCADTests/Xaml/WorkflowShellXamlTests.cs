using System.Xml.Linq;
using static StoryCADXamlScan.AutomationXamlScan;

namespace StoryCADTests.Xaml;

/// <summary>
///     Collaborator #261: the hamburger hides the workflow list with NavigationView.IsPaneVisible.
///     Closing the pane with IsPaneOpen put the NavigationView in its closed compact state after a
///     window resize: on reopen every group was closed and a group opened as a flyout.
///     A unit test cannot make the control, so this test reads the XAML as XML.
/// </summary>
[TestClass]
public class WorkflowShellXamlTests
{
    [TestMethod]
    public void WorkflowNav_HidesListWithIsPaneVisible_NotIsPaneOpen()
    {
        var nav = WorkflowNav();

        Assert.AreEqual("{x:Bind ShellViewModel.IsPaneOpen, Mode=OneWay}",
            GetAttributeValue(nav, "IsPaneVisible"));
        Assert.IsNull(GetAttributeValue(nav, "IsPaneOpen"),
            "IsPaneOpen must not be bound: a closed pane in Left mode collapses the groups.");
    }

    private static XElement WorkflowNav()
    {
        var file = ScopeFiles().Single(f => f.EndsWith("WorkflowShell.xaml", StringComparison.OrdinalIgnoreCase));
        return LoadXaml(file).Descendants()
            .Single(e => e.Name.LocalName == "NavigationView" && GetAttributeValue(e, "AutomationProperties.AutomationId") == "WorkflowNav");
    }
}
