#pragma warning disable CS8632 // Nullable annotations used without nullable context
using System.Diagnostics;
using System.Reflection;
using StoryCADLib.Services;

namespace StoryCADLib.Models;

/// <summary>
///     This class holds developer tools and app data.
/// </summary>
public class AppState
{
    private StoryDocument? _currentDocument;

    /// <summary>
    ///     Is .env present?
    /// </summary>
    public bool EnvPresent = false;

    /// <summary>
    ///     Suppresses graphical output if True
    /// </summary>
    public bool Headless;

    /// <summary>
    ///     Returns true if the app has loaded with a version change.
    ///     If this is true a changelog will show, install service will
    ///     run and the server will update the version.
    /// </summary>
    public bool LoadedWithVersionChange = false;

    /// <summary>
    ///     This is a debug timer that counts the amount of time from
    ///     the app being opened to Shell being properly initialised.
    /// </summary>
    public Stopwatch StartUpTimer = Stopwatch.StartNew();

    /// <summary>
    ///     This is the path where all app files are stored
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
    ///     This variable will return true if any of following are true:
    ///     - The Build revision is NOT 0.
    ///     - A debugger i.e. VS2022 is attached.
    ///     - .ENV is missing.
    ///     Usually it's all or none of the above.
    /// </summary>
    public bool DeveloperBuild => Debugger.IsAttached || !EnvPresent || Package.Current.Id.Version.Revision != 0;

    /// <summary>
    ///     The current version of StoryCADLib
    /// </summary>
    public string Version
    {
        get
        {
            //Get StoryCADLib Version and return it
            var assembly = Assembly.GetExecutingAssembly().GetName();
            return assembly.Version!.ToString();
        }
    }

    public StoryDocument? CurrentDocument
    {
        get => _currentDocument;
        set
        {
            _currentDocument = value;
            CurrentDocumentChanged?.Invoke(this, EventArgs.Empty); // <- notify Shell
        }
    }

    /// <summary>
    ///     The current ViewModel that can save its edits back to the model.
    ///     Set by pages in OnNavigatedTo when they have saveable content.
    ///     Null for pages without editable content (Home, Reports, etc.).
    /// </summary>
    public ISaveable? CurrentSaveable { get; set; }

    /// <summary>
    ///     The currently open story document, combining the model and its file path.
    ///     Null when no document is open (app startup).
    ///     When set, triggers UI binding updates through the Shell.
    /// </summary>
    /// <summary>
    ///     Event fired when CurrentDocument changes to notify UI to refresh bindings.
    ///     Shell subscribes to this to update x:Bind bindings when the document changes.
    /// </summary>
    public event EventHandler? CurrentDocumentChanged;
}
