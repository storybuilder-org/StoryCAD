using System;
using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestPlatform.TestExecutor;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace StoryBuilderTest
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();
        }

     protected override void OnLaunched(LaunchActivatedEventArgs args)
{
    UnitTestClient.CreateDefaultUI();

    m_window = new MainWindow();

    // Ensure the current window is active
    m_window.Activate();

    UITestMethodAttribute.DispatcherQueue = m_window.DispatcherQueue;

    // Replace back with e.Arguments when https://github.com/microsoft/microsoft-ui-xaml/issues/3368 is fixed
    UnitTestClient.Run(Environment.CommandLine);
}

        private Window m_window;
    }
}
