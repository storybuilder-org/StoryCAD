using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCAD.Services.Reports;

namespace StoryCADTests;

[TestClass]
public class ReportFormatterTests
{
    [TestMethod]
    public void GetText_NullInput_ReturnsEmpty()
    {
        var formatter = new ReportFormatter();
        string result = formatter.GetText(null);
        Assert.AreEqual(string.Empty, result);
    }
}
