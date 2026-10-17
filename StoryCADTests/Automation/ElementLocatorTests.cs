#if WINDOWS10_0_22621_0_OR_GREATER
using StoryCADAutomation.Driver;

namespace StoryCADTests.Automation;

/// <summary>
///     Tree rows match without trailing spaces (#1421 Milestone 4, ADR-011): the shipped samples
///     end most element names in a space ("Santiago "), which a script author cannot see.
///     A menu leaf the probe finds always gets a readiness budget (#1421 review M2).
/// </summary>
[TestClass]
public class ElementLocatorTests
{
    [TestMethod]
    [DataRow("Santiago ", "Santiago")]
    [DataRow("Santiago\t ", "Santiago")]
    [DataRow("Santiago", "Santiago")]
    [DataRow(" Santiago", " Santiago")]
    [DataRow("", "")]
    public void RowName_WithTrailingSpaces_TrimsOnlyTheEnd(string uiaName, string expected)
    {
        Assert.AreEqual(expected, ElementLocator.RowName(uiaName));
    }

    // #1421 review M2: a menu leaf found after the deadline used to get 1 ms to become ready.
    [TestMethod]
    public void LeafReadinessBudget_AfterDeadline_ReturnsFlyoutFloor()
    {
        var now = new DateTime(2026, 10, 17, 12, 0, 0, DateTimeKind.Utc);
        Assert.AreEqual(TimeSpan.FromMilliseconds(500), ElementLocator.LeafReadinessBudget(now.AddSeconds(-2), now));
    }

    [TestMethod]
    public void LeafReadinessBudget_WithTimeLeft_ReturnsTimeLeft()
    {
        var now = new DateTime(2026, 10, 17, 12, 0, 0, DateTimeKind.Utc);
        Assert.AreEqual(TimeSpan.FromSeconds(3), ElementLocator.LeafReadinessBudget(now.AddSeconds(3), now));
    }
}
#endif
