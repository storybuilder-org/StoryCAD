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
    public void CropArea_At150Percent_ScalesFromWindowCorner()
    {
        var frame = new Rectangle(100, 50, 1554, 1116);

        Assert.AreEqual(new Rectangle(115, 80, 300, 150),
            StoryCADDriver.CropArea(frame, new Rectangle(10, 20, 200, 100), 144));
    }

    [TestMethod]
    public void CropArea_ReachingOutsideWindow_ThrowsStepException()
    {
        var frame = new Rectangle(0, 0, 1036, 744);

        Assert.ThrowsExactly<AutomationStepException>(() =>
            StoryCADDriver.CropArea(frame, new Rectangle(900, 0, 200, 100), 96));
    }

    private static readonly Rectangle Window = new(0, 0, 1036, 744);

    private static (ElementAddress, Rectangle) Element(string id, Rectangle bounds)
        => (ElementAddress.FromAutomationId(id), bounds);

    [TestMethod]
    public void ElementArea_WithTwoElements_ReturnsUnion()
    {
        var area = StoryCADDriver.ElementArea(Window,
            [Element("A", new Rectangle(10, 700, 200, 30)), Element("B", new Rectangle(900, 695, 40, 40))], 0, 96);

        Assert.AreEqual(Rectangle.FromLTRB(10, 695, 940, 735), area);
    }

    [TestMethod]
    public void ElementArea_WithMarginAt150Percent_GrowsByScaledMargin()
    {
        var frame = new Rectangle(0, 0, 1554, 1116);

        var area = StoryCADDriver.ElementArea(frame, [Element("A", new Rectangle(100, 100, 50, 50))], 8, 144);

        Assert.AreEqual(new Rectangle(88, 88, 74, 74), area);
    }

    [TestMethod]
    public void ElementArea_WithMarginPastWindowEdge_ClipsToWindow()
    {
        var area = StoryCADDriver.ElementArea(Window, [Element("A", new Rectangle(4, 700, 1030, 40))], 12, 96);

        Assert.AreEqual(Rectangle.FromLTRB(0, 688, 1036, 744), area);
    }

    [TestMethod]
    public void ElementArea_WithEmptyRectangle_ThrowsStepExceptionNamingTarget()
    {
        var ex = Assert.ThrowsExactly<AutomationStepException>(() =>
            StoryCADDriver.ElementArea(Window,
                [Element("A", new Rectangle(10, 10, 50, 50)), Element("Hidden", Rectangle.Empty)], 0, 96));

        StringAssert.Contains(ex.Message, "Hidden");
    }

    [TestMethod]
    public void ElementArea_WithElementOutsideWindow_ThrowsStepExceptionNamingTarget()
    {
        var ex = Assert.ThrowsExactly<AutomationStepException>(() =>
            StoryCADDriver.ElementArea(Window, [Element("Away", new Rectangle(2000, 10, 50, 50))], 8, 96));

        StringAssert.Contains(ex.Message, "Away");
    }

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
