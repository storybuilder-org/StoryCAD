using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using StoryBuilder.DAL;
using StoryBuilder.Models;
using StoryBuilder.Services.Logging;
using StoryBuilder.Services.Messages;
using StoryBuilder.Services.Navigation;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace StoryBuilder.ViewModels
{
    public class SectionViewModel : ObservableRecipient, INavigable
    {
        #region Fields

        private readonly StoryReader _rdr;
        private readonly StoryWriter _wtr;
        private readonly LogService _logger;
        private bool _changeable; // process property changes for this story element
        private bool _changed;    // this story element has changed

        #endregion

        #region Properties

        // StoryElement data

        private Guid _uuid;
        public Guid Uuid
        {
            get => _uuid;
            set => SetProperty(ref _uuid, value);
        }

        private string _name;
        public string Name
        {
            get => _name;
            set
            {
                if (_changeable && _name != value) // Name changed?
                {
                    _logger.Log(LogLevel.Info, $"Requesting Name change from {_name} to {value}");
                    NameChangeMessage msg = new(_name, value);
                    Messenger.Send(new NameChangedMessage(msg));
                }
                SetProperty(ref _name, value);
            }
        }

        // Section data

        private string _notes;
        public string Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
        }

        // The StoryModel is passed when SectionPage is navigated to
        private SectionModel _model;
        public SectionModel Model
        {
            get => _model;
            set => SetProperty(ref _model, value);
        }

        #endregion

        #region Methods

        public async Task Activate(object parameter)
        {
            Model = (SectionModel)parameter;
            await LoadModel();  // Load the ViewModel from the Story
        }

        public async Task Deactivate(object parameter)
        {
            await SaveModel();    // Save the ViewModel back to the Story
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            if (_changeable)
            {
                _changed = true;
                ShellViewModel.ShowChange();
            }
        }

        private async Task LoadModel()
        {
            _changeable = false;
            _changed = false;

            Uuid = Model.Uuid;
            Name = Model.Name;

            // Read RTF file
            Notes = await _rdr.GetRtfText(Model.Notes, Uuid);

            _changeable = true;
        }

        internal async Task SaveModel()
        {
            if (_changed)
            {
                // Story.Uuid is read-only; no need to save
                Model.Name = Name;

                // Write RYG file
                Model.Notes = await _wtr.PutRtfText(Notes, Model.Uuid, "notes.rtf");

                //_logger.Log(LogLevel.Info, string.Format("Requesting IsDirty change to true"));
                //Messenger.Send(new IsChangedMessage(Changed));
            }
        }

        #endregion

        #region Constructor

        public SectionViewModel()
        {
            _logger = Ioc.Default.GetService<LogService>();
            _wtr = Ioc.Default.GetService<StoryWriter>();
            _rdr = Ioc.Default.GetService<StoryReader>();

            Notes = string.Empty;

            PropertyChanged += OnPropertyChanged;
        }

        #endregion
    }
}
