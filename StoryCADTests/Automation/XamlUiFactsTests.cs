#if WINDOWS10_0_22621_0_OR_GREATER
using StoryCADAutomation.Scripting;

namespace StoryCADTests.Automation;

/// <summary>
///     Menu owner chains read from the live XAML: `menu` opens these directly instead of
///     trying every menu in turn (#1421 Milestone 4).
/// </summary>
[TestClass]
public class XamlUiFactsTests
{
    private static readonly XamlUiFacts Facts = XamlUiFacts.LoadFromXamlScan();

    [TestMethod]
    public void MenuOpeners_ForPrintReports_IsTheReportsButton()
    {
        CollectionAssert.AreEqual(new[] { "ReportsMenuButton" }, Facts.MenuOpeners["PrintReportsMenuItem"].ToArray());
    }

    [TestMethod]
    public void MenuOpeners_ForMasterPlots_IsToolsThenPlottingAids()
    {
        CollectionAssert.AreEqual(new[] { "ToolsMenuButton", "PlottingAidsMenuItem" },
            Facts.MenuOpeners["MasterPlotsMenuItem"].ToArray());
    }
}
#endif
