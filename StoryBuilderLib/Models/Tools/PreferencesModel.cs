namespace StoryBuilder.Models.Tools
{
    /// <summary>
    /// PreferencesModel contains product and licensing information,
    /// and (especially) user preferences. 
    /// 
    /// The model is maintained from a Shell Preferences() method
    /// which is launched from a Command tied to a View button,
    /// and by PreferencesViewModel using a ContentDialog as the view.
    /// The backing store is a file, StoryBuilder.prf, which is 
    /// contained with other installation files. If no .prf file is
    /// present, StoryBuilder.Services.Install.InstallationService's
    /// InstallFiles() method will create one.
    /// 
    /// </summary>
    public class PreferencesModel
    {
        #region Properties
        public bool Changed { get; set; }
        // Product Information (set on installation)
        public string ProductName { get; set; }
        public string ProductVersion { get; set; }

        // Liscense Information (set on installation)
        public string LicenseOwner { get; set; }
        public string LicenseNumber { get; set; }
        // Miscellaneous
        public bool QuoteOnStartup { get; set; }
        // Current Screen Font
        public string ScreenFont { get; set; }
        public string PrinterFont { get; set; }
        // Backup Information
        public string BackupOnOpen { get; set; }
        public bool TimedBackup { get; set; }
        public int TimedBackupInterval { get; set; }

        // Installation folder is set to roaming ApplicationData's StoryBuilder subfolder
        // This is also the default Preferences file location
        public string InstallationDirectory { get; set; }
        // Default location to read / save user story outlines
        public string ProjectDirectory { get; set; }
        public string BackupDirectory { get; set; }
        public string LogDirectory { get; set; }

        // Recent files (set automatically)
        public string LastFile1 { get; set; }
        public string LastFile2 { get; set; }
        public string LastFile3 { get; set; }
        public string LastFile4 { get; set; }
        public string LastFile5 { get; set; }

        #endregion

        #region Constructor
        public PreferencesModel()
        {
            InstallationDirectory = string.Empty;
            LastFile1 = string.Empty;
            LastFile2 = string.Empty;
            LastFile3 = string.Empty;
            LastFile4 = string.Empty;
            LastFile5 = string.Empty;
        }
        #endregion

    }


}
