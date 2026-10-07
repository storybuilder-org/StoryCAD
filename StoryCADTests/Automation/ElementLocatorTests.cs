#if WINDOWS10_0_22621_0_OR_GREATER
using StoryCADAutomation.Driver;

namespace StoryCADTests.Automation;

/// <summary>
///     Tree rows match without trailing spaces (#1421 Milestone 4, ADR-011): the shipped samples
///     end most element names in a space ("Santiago "), which a script author cannot see.
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
}
#endif
