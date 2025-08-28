using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCAD.Models;
using StoryCAD.Services.Reports;

namespace StoryCADTests;

[TestClass]
public class ReportFormatterTests
{
    [TestMethod]
    public void GetText_NullInput_ReturnsEmpty()
    {
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var formatter = new ReportFormatter(appState);
        string result = formatter.GetText(null);
        Assert.AreEqual(string.Empty, result);
    }
}
