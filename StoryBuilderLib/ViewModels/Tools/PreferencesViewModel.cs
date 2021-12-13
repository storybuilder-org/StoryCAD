using CommunityToolkit.Mvvm.ComponentModel;
using StoryBuilder.DAL;
using StoryBuilder.Models;
using StoryBuilder.Models.Tools;
using System.Threading.Tasks;
using Windows.Storage;

namespace StoryBuilder.ViewModels.Tools
{
    public class PreferencesViewModel : ObservableRecipient
    {

        private string _Name;
        public string Name
        {
            get { return _Name; }
            set { _Name = value; }
        }
        private string _Email;
        public string Email
        {
            get { return _Email; }
            set { _Email = value; }
        }
        private bool _ErrorConsent;
        public bool ErrorConsent
        {
            get { return _ErrorConsent; }
            set { _ErrorConsent = value; }
        }
        private bool _NewsConsent;
        public bool NewsConsent
        {
            get { return _NewsConsent; }
            set { _NewsConsent = value; }
        }
        private bool _Backup;
        public bool Backup
        {
            get { return _Backup; }
            set { _Backup = value; }
        }
        private int _BackupInterval;
        public int BackupInterval
        {
            get { return _BackupInterval; }
            set { _BackupInterval = value; }
        }
        private string _ProjectDir;
        public string ProjectDir
        {
            get { return _ProjectDir; }
            set { _ProjectDir = value; }
        }

        /// <summary>
        /// Saves the users preferences to disk.
        /// </summary>
        public async Task SaveAsync()
        {
            PreferencesModel prf = new();
            prf.Name = Name;
            prf.Email = Email;
            prf.ErrorCollectionConsent = ErrorConsent;
            prf.ProjectDirectory = ProjectDir;
            prf.TimedBackupInterval = BackupInterval;
            prf.TimedBackup = Backup;
            prf.Newsletter = NewsConsent;

            PreferencesIO prfIO = new(prf, System.IO.Path.Combine(ApplicationData.Current.RoamingFolder.Path, "Storybuilder"));
            await prfIO.UpdateFile();
            PreferencesIO loader = new(GlobalData.Preferences, System.IO.Path.Combine(ApplicationData.Current.RoamingFolder.Path, "Storybuilder"));
            await loader.UpdateModel();
        }

        public PreferencesViewModel()
        {
            PreferencesModel _model = GlobalData.Preferences;
            ErrorConsent = _model.ErrorCollectionConsent;
            Email = _model.Email;
            Name = _model.Name;
            ProjectDir = _model.ProjectDirectory;
            BackupInterval = _model.TimedBackupInterval;
            Backup = _model.TimedBackup;
            NewsConsent = _model.Newsletter;
        }
    }
}
