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
    /// <summary>
    /// This is the path where all app files are stored
    /// </summary>
    public string RootDirectory
    {
        get
        {
            try
            {
                if (!Headless)
                {
                    //Try and get roaming folder
                    return Path.Combine(ApplicationData.Current.RoamingFolder.Path, "StoryCAD");
                }
            }
            catch
            {
            }

            //Return base directory
            return AppDomain.CurrentDomain.BaseDirectory;
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
    /// the app being opened to Shell being properly initialised.
    /// </summary>
    public Stopwatch StartUpTimer = Stopwatch.StartNew();

    /// <summary>
    /// Is .env present?
    /// </summary>
    public bool EnvPresent = false;

    /// <summary>
    /// Suppresses graphical output if True
    /// </summary>
    public bool Headless;

    /// <summary>
    /// The current version of StoryCADLib
    /// </summary>
    public string Version
    {
        get
        {
            //Get StoryCADLib Version and return it
            AssemblyName assembly = Assembly.GetExecutingAssembly().GetName();
            return assembly.Version!.ToString();
		}
	}

    /// <summary>
    /// Returns true if the app has loaded with a version change.
    /// If this is true a changelog will show, install service will
    /// run and the server will update the version.
    /// </summary>    
    public bool LoadedWithVersionChange = false;

    /// <summary>
    /// The currently open story document, combining the model and its file path.
    /// Null when no document is open (app startup).
    /// </summary>
    public StoryDocument? CurrentDocument { get; set; }

    /// <summary>
    /// The current ViewModel that can save its edits back to the model.
    /// Set by pages in OnNavigatedTo when they have saveable content.
    /// Null for pages without editable content (Home, Reports, etc.).
    /// </summary>
    public Services.ISaveable? CurrentSaveable { get; set; }
}