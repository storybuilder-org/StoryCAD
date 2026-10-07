#if WINDOWS10_0_22621_0_OR_GREATER
using System.Drawing;
using StoryCADAutomation.Driver;

namespace StoryCADTests.Automation;

/// <summary>
///     The pure parts of two #1421 Milestone 4 driver fixes (ADR-011): the click point must lie
///     on the element, and the foreground check waits briefly for StoryCAD. No app is launched.
/// </summary>
[TestClass]
public class StoryCADDriverTests
{
    private static readonly Rectangle Row = new(100, 200, 300, 40);

    [TestMethod]
    public void ChooseClickPoint_WithPointInsideRectangle_ReturnsThatPoint()
    {
        Assert.AreEqual(new Point(150, 210), StoryCADDriver.ChooseClickPoint(new Point(150, 210), Row));
    }

    [TestMethod]
    public void ChooseClickPoint_WithPointOutsideRectangle_ReturnsRectangleCenter()
    {
        // The outline root row reports a point at the window's top-left corner.
        Assert.AreEqual(new Point(250, 220), StoryCADDriver.ChooseClickPoint(new Point(0, 0), Row));
    }

    [TestMethod]
    public void ChooseClickPoint_WithNoPoint_ReturnsRectangleCenter()
    {
        Assert.AreEqual(new Point(250, 220), StoryCADDriver.ChooseClickPoint(null, Row));
    }

    [TestMethod]
    public void ChooseClickPoint_WithEmptyRectangle_ReturnsReportedPoint()
    {
        Assert.AreEqual(new Point(5, 5), StoryCADDriver.ChooseClickPoint(new Point(5, 5), Rectangle.Empty));
    }

    [TestMethod]
    public void ChooseClickPoint_WithNoPointAndEmptyRectangle_ReturnsNull()
    {
        Assert.IsNull(StoryCADDriver.ChooseClickPoint(null, Rectangle.Empty));
    }

    [TestMethod]
    public void WaitForForegroundProcess_WhenStoryCADReturnsAfterABlank_ReturnsStoryCAD()
    {
        // Process 0 twice (a dialog just closed), then StoryCAD.
        var reads = new Queue<uint>(new uint[] { 0, 0, 42 });
        var pauses = 0;

        var pid = StoryCADDriver.WaitForForegroundProcess(reads.Dequeue, 42, 10, () => pauses++);

        Assert.AreEqual(42u, pid);
        Assert.AreEqual(2, pauses);
    }

    [TestMethod]
    public void WaitForForegroundProcess_WhenAnotherAppKeepsForeground_StopsAfterPollLimit()
    {
        var pauses = 0;

        var pid = StoryCADDriver.WaitForForegroundProcess(() => 7, 42, 10, () => pauses++);

        Assert.AreEqual(7u, pid);
        Assert.AreEqual(10, pauses);
    }

    [TestMethod]
    public void WaitForForegroundProcess_WhenStoryCADIsForeground_DoesNotPause()
    {
        var pauses = 0;

        StoryCADDriver.WaitForForegroundProcess(() => 42, 42, 10, () => pauses++);

        Assert.AreEqual(0, pauses);
    }
}
#endif
