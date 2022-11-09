using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using Microsoft.Windows.ApplicationModel.DynamicDependency;
using StoryBuilder.Models;

[assembly: WinUITestTarget(typeof(StoryBuilder.App))]

namespace StoryBuilderTest;

[TestClass]
public class Initialize
{
    [AssemblyInitialize]
    public static void AssemblyInitialize(TestContext context)
    {
        // TODO: Initialize the appropriate version of the Windows App SDK.
        // This is required when testing MSIX apps that are framework-dependent on the Windows App SDK.
        Bootstrap.TryInitialize(0x00010001, out int _);

        // Activate app

        //UITestMethodAttribute.DispatcherQueue = GlobalData.MainWindow.DispatcherQueue;
    }

    [AssemblyCleanup]
    public static void AssemblyCleanup()
    {
        Bootstrap.Shutdown();
    }
}
