using CommunityToolkit.Mvvm.ComponentModel;
using StoryBuilder.Models;
using StoryBuilder.Models.Tools;

namespace StoryBuilder.ViewModels.Tools
{
    public class PreferencesViewModel : ObservableRecipient
    {
        #region Fields
        private bool _changed;

        private bool _quoteonstartup;
        private string _screenfont;
        private string _printerfont;
        private string _backupOnOpen;
        private bool _timedbackup;
        private int _timedbackupinterval;
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

        public bool QuoteOnStartup
        {
            get => _quoteonstartup;
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
            _model = GlobalData.Preferences;
        }

        #endregion

        public void LoadModel()
        {

        }

        public void SaveModel()
        {
        
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
