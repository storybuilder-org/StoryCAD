using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using StoryBuilder.Controllers;
using StoryBuilder.DAL;
using StoryBuilder.Models;
using StoryBuilder.Services.Logging;
using StoryBuilder.Services.Messages;
using StoryBuilder.Services.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;

namespace StoryBuilder.ViewModels
{
    public class SettingViewModel : ObservableRecipient, INavigable
    {
        #region Fields

        private readonly StoryController _story;
        private readonly LogService _logger;
        private readonly StoryReader _rdr;
        private readonly StoryWriter _wtr;
        private bool _changeable;

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
                if (_changeable && (_name != value)) // Name changed?
                {
                    _logger.Log(LogLevel.Info, string.Format("Requesting Name change from {0} to {1}", _name, value));
                    var msg = new NameChangeMessage(_name, value);
                    Messenger.Send(new NameChangedMessage(msg));
                }
                SetProperty(ref _name, value);
            }
        }

        // Setting (General) data

        private string _locale;
        public string Locale
        {
            get => _locale;
            set => SetProperty(ref _locale, value);
        }

        private string _season;
        public string Season
        {
            get => _season;
            set => SetProperty(ref _season, value);
        }

        private string _period;
        public string Period
        {
            get => _period;
            set => SetProperty(ref _period, value);
        }

        private string _lighting;
        public string Lighting
        {
            get => _lighting;
            set => SetProperty(ref _lighting, value);
        }

        private string _weather;
        public string Weather
        {
            get => _weather;
            set => SetProperty(ref _weather, value);
        }

        private string _temperature;
        public string Temperature
        {
            get => _temperature;
            set => SetProperty(ref _temperature, value);
        }

        private string _prop1;
        public string Prop1
        {
            get => _prop1;
            set => SetProperty(ref _prop1, value);
        }

        private string _prop2;
        public string Prop2
        {
            get => _prop2;
            set => SetProperty(ref _prop2, value);
        }

        private string _prop3;
        public string Prop3
        {
            get => _prop3;
            set => SetProperty(ref _prop3, value);
        }

        private string _prop4;
        public string Prop4
        {
            get => _prop4;
            set => SetProperty(ref _prop4, value);
        }

        private string _summary;
        public string Summary
        {
            get => _summary;
            set => SetProperty(ref _summary, value);
        }

        // Setting Sense data

        private string _sights;
        public string Sights
        {
            get => _sights;
            set => SetProperty(ref _sights, value);
        }

        private string _sounds;
        public string Sounds
        {
            get => _sounds;
            set => SetProperty(ref _sounds, value);
        }

        private string _touch;
        public string Touch
        {
            get => _touch;
            set => SetProperty(ref _touch, value);
        }

        private string _smelltaste;
        public string SmellTaste
        {
            get => _smelltaste;
            set => SetProperty(ref _smelltaste, value);
        }

        // Setting notes data

        private string _notes;
        public string Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
        }

        // The StoryModel is passed when CharacterPage is navigated to
        private SettingModel _model;
        public SettingModel Model
        {
            get => _model;
            set => SetProperty(ref _model, value);
        }

        // The Changed bit tracks any change to this ViewModel.
        private bool _changed;
        public bool Changed
        {
            get => _changed;
            set => SetProperty(ref _changed, value);
        }

        #endregion

        #region Methods

        public async Task Activate(object parameter)
        {
            Model = (SettingModel)parameter;
            await LoadModel();
        }

        public async Task Deactivate(object parameter)
        {
            await SaveModel();    // Save the ViewModel back to the Story
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            if (_changeable)
                Changed = true;
        }

        private async Task LoadModel()
        {
            Uuid = Model.Uuid;
            Name = Model.Name;
            Locale = Model.Locale;
            Season = Model.Season;
            Period = Model.Period;
            Lighting = Model.Lighting;
            Weather = Model.Weather;
            Temperature = Model.Temperature;
            Prop1 = Model.Prop1;
            Prop2 = Model.Prop2;
            Prop3 = Model.Prop3;
            Prop4 = Model.Prop4;
            Summary = string.Empty;

            // Read RTF files
            Summary = await _rdr.GetRtfText(Model.Summary, Uuid);
            Sights = await _rdr.GetRtfText(Model.Sights, Uuid);
            Sounds = await _rdr.GetRtfText(Model.Sounds, Uuid);
            Touch = await _rdr.GetRtfText(Model.Touch, Uuid);
            SmellTaste = await _rdr.GetRtfText(Model.SmellTaste, Uuid);
            Notes = await _rdr.GetRtfText(Model.Notes, Uuid);

            Changed = false;
            PropertyChanged += OnPropertyChanged;
            _changeable = true;
        }

        internal async Task SaveModel()
        {
            PropertyChanged -= OnPropertyChanged;
            _changeable = false;
            if (Changed)
            {
                // Story.Uuid is read-only and cannot be assigned
                Model.Name = Name;
                Model.Locale = Locale;
                Model.Season = Season;
                Model.Period = Period;
                Model.Lighting = Lighting;
                Model.Weather = Weather;
                Model.Temperature = Temperature;
                Model.Prop1 = Prop1;
                Model.Prop2 = Prop2;
                Model.Prop3 = Prop3;
                Model.Prop4 = Prop4;

                //Write RTF files
                Model.Summary = await _wtr.PutRtfText(Summary, Model.Uuid, "notes.rtf");
                Model.Sights = await _wtr.PutRtfText(Sights, Model.Uuid, "sights.rtf");
                Model.Sounds = await _wtr.PutRtfText(Sounds, Model.Uuid, "sounds.rtf");
                Model.Touch = await _wtr.PutRtfText(Touch, Model.Uuid, "touch.rtf");
                Model.SmellTaste = await _wtr.PutRtfText(SmellTaste, Model.Uuid, "smelltaste.rtf");
                Model.Notes = await _wtr.PutRtfText(Notes, Model.Uuid, "notes.rtf");

                _logger.Log(LogLevel.Info, string.Format("Requesting IsDirty change to true"));
                Messenger.Send(new IsChangedMessage(Changed));
            }
        }

        #endregion

        #region ComboBox ItemsSource collections

        public ObservableCollection<string> LocaleList;
        public ObservableCollection<string> SeasonList;

        #endregion

        #region Constructor
        public SettingViewModel()
        {
            _story = Ioc.Default.GetService<StoryController>();
            _logger = Ioc.Default.GetService<LogService>();
            _wtr = Ioc.Default.GetService<StoryWriter>();
            _rdr = Ioc.Default.GetService<StoryReader>();

            Locale = string.Empty;
            Season = string.Empty;
            Period = string.Empty;
            Lighting = string.Empty;
            Weather = string.Empty;
            Temperature = string.Empty;
            Prop1 = string.Empty;
            Prop2 = string.Empty;
            Prop3 = string.Empty;
            Prop4 = string.Empty;
            Summary = string.Empty;
            Sights = string.Empty;
            Sounds = string.Empty;
            Touch = string.Empty;
            SmellTaste = string.Empty;
            Notes = string.Empty;

            Dictionary<string, ObservableCollection<string>> lists = GlobalData.ListControlSource;
            LocaleList = lists["Locale"];
            SeasonList = lists["Season"];
        }

        #endregion
    }
}
