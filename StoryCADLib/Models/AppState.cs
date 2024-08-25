using System.Diagnostics;
using System.Reflection;
using Windows.ApplicationModel;
using Windows.Storage;

namespace StoryCAD.Models;

/// <summary>
/// This class holds developer tools and app data.
/// </summary>
public class AppState
{
    public AppState()
    {
        StoryCADTestsMode = Assembly.GetEntryAssembly().Location.Contains("StoryCADTests.dll") ||
                                Assembly.GetEntryAssembly().Location.Contains("CollaboratorTests.dll") ||
                                Assembly.GetEntryAssembly().Location.Contains("testhost.dll");
    }

    /// <summary>
    /// This is the path where all app files are stored
    /// </summary>
    public string RootDirectory
    {
        get
        {
            //Use a different path if we are within StoryCAD.
            if (StoryCADTestsMode)
            {
                return System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "StoryCADTests");
            }
            else
            {
                return System.IO.Path.Combine(ApplicationData.Current.RoamingFolder.Path, "StoryCAD");
            }
        }
    }

    /// <summary>
    /// This variable will return true if any of following are true:
    ///  - The Build revision is NOT 0.
    ///  - A debugger i.e. VS2022 is attached.
    ///  - .ENV is missing.
    ///  
    /// Usually it's all or none of the above.
    /// </summary>
    public bool DeveloperBuild => Debugger.IsAttached || !EnvPresent || Package.Current.Id.Version.Revision != 0;

    /// <summary>
    /// This is a debug timer that counts the amount of time from
    /// the app being opened to Shell being properly initalised.
    /// </summary>
    public Stopwatch StartUpTimer = Stopwatch.StartNew();

    /// <summary>
    /// Is .env present?
    /// </summary>
    public bool EnvPresent = false;

    /// <summary>
    /// Returns true if code is running via StoryCADTests.dll instead of StoryCAD.exe
    /// </summary>
    public bool StoryCADTestsMode;

    /// <summary>
    /// The current (running) version of StoryCAD
    /// Returns a simple 4 number tuple on release versions i.e 2.12.0.0
    /// Returns a 3 number tuple and build time on
    ///
    /// (Returns 2.0.0.0 (StoryCADTests) if spawned via StoryCADTests.dll)
    /// </summary>
    public string Version
    {
        get
        {
            if (StoryCADTestsMode) {  return "2.0.0.0 (StoryCADTests)";  }

            string _packageVersion = $"{ Package.Current.Id.Version.Major }.{ Package.Current.Id.Version.Minor}.{ Package.Current.Id.Version.Build}";
            if (Package.Current.Id.Version.Revision == 65535)
            {
                string StoryCADManifestVersion = Assembly.GetEntryAssembly()
                    .GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion
                    .Split("build")[1];
                return $"Version: {_packageVersion} Built on: {StoryCADManifestVersion}";
            }
            return $"Version: {_packageVersion}";
		}
	}

    /// <summary>
    /// Returns true if the app has loaded with a version change.
    /// If this is true a changelog will show, install service will
    /// run and the server will update the version.
    /// </summary>    
    public bool LoadedWithVersionChange = false;
}