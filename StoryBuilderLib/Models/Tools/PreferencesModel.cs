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

        //User infomation
        public string Name { get; set; }
        public string Email { get; set; }
        public bool ErrorCollectionConsent { get; set; }
        public bool Newsletter { get; set; }

        // Visual changes
        public bool ForceDarkmode { get; set; }
        public bool QuoteOnStartup { get; set; }

        // Backup Information
        public string BackupOnOpen { get; set; }
        public bool TimedBackup { get; set; }
        public int TimedBackupInterval { get; set; }

        //Directorys
        public string InstallationDirectory { get; set; }
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
