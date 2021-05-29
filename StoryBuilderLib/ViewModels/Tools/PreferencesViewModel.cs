using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using StoryBuilder.Controllers;
using StoryBuilder.Models.Tools;

namespace StoryBuilder.ViewModels.Tools
{
    public class PreferencesViewModel : ObservableRecipient
    {
        #region Fields
        private bool _changed;

        private string _licenseowner;
        private string _licensenumber;
        private bool   _quoteonstartup;
        private string _screenfont;
        private string _printerfont;
        private string _backupOnOpen;
        private bool   _timedbackup;
        private int    _timedbackupinterval;
        private string _defaultdirectory;
        private string _defaultprojectdirectory;

        private PreferencesModel _model;

        #endregion

        #region Properties
        public bool Changed
        {
            get { return _changed; }
            set { _changed = value; }
        }

        public string LicenseOwner
        {
            get => _licenseowner;
            set => SetProperty(ref _licenseowner, value);
        }

        public string LicenseNumber
        {
            get => _licensenumber;
            set => SetProperty(ref _licensenumber, value);
        }

        public bool QuoteOnStartup
        {
            get =>_quoteonstartup; 
            set => SetProperty(ref _quoteonstartup, value);
        }

        public string ScreenFont
        {
            get => _screenfont;
            set => SetProperty(ref _screenfont, value);
        }

        public string PrinterFont
        {
            get => _printerfont;
            set => SetProperty(ref _printerfont, value);
        }

        public string BackupOnOpen
        {
            get => _backupOnOpen;
            set => SetProperty(ref _backupOnOpen, value);
        }

        public bool TimedBackup
        {
            get => _timedbackup; 
            set => SetProperty(ref _timedbackup, value);
        }

        public int TimedBackupInterval
        {
            get => _timedbackupinterval; 
            set => SetProperty(ref _timedbackupinterval, value);
        }
        public string BackupDirectory
        {
            get => _defaultprojectdirectory;
            set => SetProperty(ref _defaultprojectdirectory, value);
        }

        public string DefaultDirectory
        {
            get => _defaultdirectory;
            set => SetProperty(ref _defaultdirectory, value);
        }

        public string ProjectDirectory
        {
            get => _defaultprojectdirectory;
            set => SetProperty(ref _defaultprojectdirectory, value);
        }

        public string LogDirectory
        {
            get => _defaultprojectdirectory;
            set => SetProperty(ref _defaultprojectdirectory, value);
        }
   
        #endregion

        #region Constructor

        public PreferencesViewModel()
        {
            var story = Ioc.Default.GetService<StoryController>();
            _model = story.Preferences;
        }

        #endregion

        public void LoadModel()
        {
            LicenseOwner = _model.LicenseOwner;
            LicenseNumber = _model.LicenseNumber;
            QuoteOnStartup = _model.QuoteOnStartup;
            ScreenFont = _model.ScreenFont;
            //model.ScreenFontBold = ScreenFontBold;
            PrinterFont = _model.PrinterFont;
            _backupOnOpen = _model.BackupOnOpen;
            //model.PrinterFontBold = PrinterFontBold;
            TimedBackup = _model.TimedBackup;
            TimedBackupInterval = _model.TimedBackupInterval;
            BackupDirectory = _model.BackupDirectory;
            //model.PreviousBackup = PreviousBackup;
            ProjectDirectory = _model.ProjectDirectory;
            LogDirectory = _model.LogDirectory;
            Changed = false;
        }

        public void SaveModel()
        {
            _model.LicenseOwner = LicenseOwner;
            _model.LicenseNumber = LicenseNumber;
            _model.QuoteOnStartup = QuoteOnStartup;
            _model.ScreenFont = ScreenFont;
            //_model.ScreenFontBold = ScreenFontBold;
            _model.PrinterFont = PrinterFont;
            //_model.PrinterFontBold = PrinterFontBold;
            _model.TimedBackup = TimedBackup;
            _model.TimedBackupInterval = TimedBackupInterval;
            //_model.PreviousBackup = PreviousBackup;
            _model.ProjectDirectory = DefaultDirectory;
            _model.BackupDirectory = BackupDirectory;
            _model.LogDirectory = LogDirectory;
            _model.Changed = true;
            Changed = false;
        }

        public void ChangeScreenFontCommand()
        {
            //FontDialog dialog = new FontDialog();
            //dialog.ShowColor = false;
            //FontFamily family = new FontFamily(ScreenFont);
            //FontStyle style = ScreenFontBold ? FontStyle.Bold : FontStyle.Regular;
            //dialog.Font = new Font(family, 9.75f, style);
            //if(dialog.ShowDialog() != DialogResult.Cancel )
            //{
            //    ScreenFont = dialog.Font.Name;
            //    ScreenFontBold = dialog.Font.Bold;
            //}
        }

        public void ChangePrinterFontCommand()
        {
            //FontWeight weight = 
            //FontDialog dialog = new FontDialog();
            //dialog.FixedPitchOnly = true;
            //dialog.ShowColor = false;
            //FontFamily family = new FontFamily(PrinterFont);
            //FontStyle style = PrinterFontBold ? FontStyles.Oblique : FontStyles.Normal;
            //dialog.Font = new Font(family, 9.75f, style);
            //if (dialog.ShowDialog() != DialogResult.Cancel) {
            //    PrinterFont = dialog.Font.Name;
            //    PrinterFontBold = dialog.Font.Bold;
            //}
        }
    }
}
