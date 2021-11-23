﻿using CommunityToolkit.Mvvm.ComponentModel;
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
    /// <summary>
    /// OverviewModel contains overview information for the entire story, such as title, author, and so on.
    /// It's a good place to capture the original idea which prompted the story.
    ///
    /// There is only one OverviewModel instance for each story. It's also the root of the Shell Page's
    /// StoryExplorer TreeView.
    /// </summary>
    public class OverviewViewModel : ObservableRecipient, INavigable
    {
        /* Handing date fields and author:
         * System.DateTime wrkDate = DateTime.FromOADate(0);
           wrkDate = DateTime.Parse(DateTime.Parse(frmStory.DefInstance.mskDateCreated.Text).ToString("MM/dd/yy"));
         * StoryRec.DateCreated.Value = wrkDate.ToString("MM-dd-yy");
         */

        #region Fields

        private readonly StoryController _story;
        private readonly LogService _logger;
        private readonly StoryReader _rdr;
        private readonly StoryWriter _wtr;
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
                if (_changeable && (_name != value)) // Name changed?
                {
                    _logger.Log(LogLevel.Info, string.Format("Requesting Name change from {0} to {1}", _name, value));
                    var msg = new NameChangeMessage(_name, value);
                    Messenger.Send(new NameChangedMessage(msg));
                }
                SetProperty(ref _name, value);
            }
        }

        // Overview data

        private string _dateCreated;
        public string DateCreated
        {
            get => _dateCreated;
            set => SetProperty(ref _dateCreated, value);
        }

        private string _author;
        public string Author
        {
            get => _author;
            set => SetProperty(ref _author, value);

        }

        private string _dateModified;
        public string DateModified
        {
            get => _dateModified;
            set => SetProperty(ref _dateModified, value);
        }


        private string _storyIdea;
        public string StoryIdea
        {
            get => _storyIdea;
            set => SetProperty(ref _storyIdea, value);
        }

        // Concept data

        private string _concept;
        public string Concept
        {
            get => _concept;
            set => SetProperty(ref _concept, value);
        }

        // Premise data

        private string _storyProblem;  // The Guid of a Problem StoryElement
        public string StoryProblem
        {
            get => _storyProblem;
            set 
            {   LoadStoryPremise(value);  // Copy the problem's Premise to this Premise
                SetProperty(ref _storyProblem, value);
            }
        }

        private string _premise;
        public string Premise
        {
            get => _premise;
            set => SetProperty(ref _premise, value);
        }

        private bool _premiseLock;

        public bool PremiseLock
        {
            get => _premiseLock;
            set => SetProperty(ref _premiseLock, value);
        }

        // Structure data

        private string _storyType;
        public string StoryType
        {
            get => _storyType;
            set => SetProperty(ref _storyType, value);
        }

        private string _storyGenre;
        public string StoryGenre
        {
            get => _storyGenre;
            set => SetProperty(ref _storyGenre, value);
        }

        private string _viewpoint;
        public string Viewpoint
        {
            get => _viewpoint;
            set => SetProperty(ref _viewpoint, value);
        }

        private string _viewpointCharacter;
        public string ViewpointCharacter
        {
            get => _viewpointCharacter;
            set => SetProperty(ref _viewpointCharacter, value);
        }

        private string _voice;
        public string Voice
        {
            get => _voice;
            set => SetProperty(ref _voice, value);
        }

        private string _literaryDevice;
        public string LiteraryDevice
        {
            get => _literaryDevice;
            set => SetProperty(ref _literaryDevice, value);
        }

        private string _tense;
        public string Tense
        {
            get => _tense;
            set => SetProperty(ref _tense, value);
        }

        private string _style;
        public string Style
        {
            get => _style;
            set => SetProperty(ref _style, value);
        }

        private string _styleNotes;
        public string StyleNotes
        {
            get => _styleNotes;
            set => SetProperty(ref _styleNotes, value);
        }

        private string _tone;
        public string Tone
        {
            get => _tone;
            set => SetProperty(ref _tone, value);
        }

        private string _toneNotes;
        public string ToneNotes
        {
            get => _toneNotes;
            set => SetProperty(ref _toneNotes, value);
        }

        // Notes data

        private string _notes;
        public string Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
        }

        // The StoryModel is passed when CharacterPage is navigated to
        private OverviewModel _model;
        public OverviewModel Model
        {
            get => _model;
            set => _model = value;
        }

        #endregion



        #region Methods

        public async Task Activate(object parameter)
        {
            Model = (OverviewModel)parameter;
            await LoadModel();
        }

        public async Task Deactivate(object parameter)
        {
            await SaveModel();
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
            DateCreated = Model.DateCreated;
            Author = Model.Author;
            DateModified = Model.DateModified;
            StoryType = Model.StoryType;
            StoryGenre = Model.StoryGenre;
            Viewpoint = Model.Viewpoint;
            ViewpointCharacter = Model.ViewpointCharacter;
            Voice = Model.Voice;
            LiteraryDevice = Model.LiteraryDevice;
            Tense = Model.Tense;
            Style = Model.Style;
            Tone = Model.Tone;
            Style = Model.Style;
            // Load RTF files
            StoryIdea = await _rdr.GetRtfText(Model.StoryIdea, Uuid);
            Concept = await _rdr.GetRtfText(Model.Concept, Uuid);
            StyleNotes = await _rdr.GetRtfText(Model.StyleNotes, Uuid);
            ToneNotes = await _rdr.GetRtfText(Model.ToneNotes, Uuid);
            Notes = await _rdr.GetRtfText(Model.Notes, Uuid);

            _changeable = true;
        }

        internal async Task SaveModel()
        {
            if (_changed)
            {
                // Story.Uuid is read-only and cannot be assigned
                Model.Name = Name;
                Model.DateCreated = DateCreated;
                Model.Author = Author;
                Model.DateModified = DateModified;
                Model.StoryType = StoryType;
                Model.StoryGenre = StoryGenre;
                Model.Viewpoint = Viewpoint;
                Model.ViewpointCharacter = ViewpointCharacter;
                Model.Voice = Voice;
                Model.LiteraryDevice = LiteraryDevice;
                Model.Style = Style;
                Model.Tense = Tense;
                Model.Style = Style;
                Model.Tone = Tone;

                // Write RTF files
                Model.StoryIdea = await _wtr.PutRtfText(StoryIdea, Model.Uuid, "storyidea.rtf");
                Model.Concept = await _wtr.PutRtfText(Concept, Model.Uuid, "concept.rtf");
                Model.StyleNotes = await _wtr.PutRtfText(StyleNotes, Model.Uuid, "stylenotes.rtf");
                Model.ToneNotes = await _wtr.PutRtfText(ToneNotes, Model.Uuid, "tonenotes.rtf");
                Model.Notes = await _wtr.PutRtfText(Notes, Model.Uuid, "notes.rtf");

                //_logger.Log(LogLevel.Info, string.Format("Requesting IsDirty change to true"));
                //Messenger.Send(new IsChangedMessage(Changed));
            }
        }

        private async void LoadStoryPremise(string value)
        {
            ProblemModel problem = (ProblemModel) StringToStoryElement(value);
            PremiseLock = false;    // Set Premise to read-only
            Premise = await _rdr.GetRtfText(problem.Premise, problem.Uuid);
            PremiseLock = true;     // Set Premise to read/write
        }

        private StoryElement StringToStoryElement(string value)
        {
            if (value == null)
                return null;
            if (value.Equals(string.Empty))
                return null;
            // Get the current StoryModel's StoryElementsCollection
            ShellViewModel shell = Ioc.Default.GetService<ShellViewModel>();
            StoryElementCollection elements = shell.StoryModel.StoryElements;
            // legacy: locate the StoryElement from its Name
            foreach (StoryElement element in elements)  // Character or Setting??? Search both?
            {
                if (element.Type == StoryItemType.Character | element.Type == StoryItemType.Setting)
                {
                    if (value.Equals(element.Name))
                        return element;
                }
            }
            // Look for the StoryElement corresponding to the passed guid
            // (This is the normal approach)
            Guid guid = new Guid(value.ToString());
            if (elements.StoryElementGuids.ContainsKey(guid))
                return elements.StoryElementGuids[guid];
            return null;   // Not found
        }


        #endregion

        #region ComboBox ItemsSource collections

        public ObservableCollection<string> StoryTypeList;
        public ObservableCollection<string> GenreList;
        public ObservableCollection<string> ViewpointList;
        public ObservableCollection<string> LiteraryDeviceList;
        public ObservableCollection<string> LiteraryStyleList;
        public ObservableCollection<string> VoiceList;
        public ObservableCollection<string> TenseList;
        public ObservableCollection<string> StyleList;
        public ObservableCollection<string> ToneList;

        // StoryElement collections
        public List<string> CharacterList;

        #endregion

        #region Constructor

        public OverviewViewModel()
        {
            _story = Ioc.Default.GetService<StoryController>();
            _logger = Ioc.Default.GetService<LogService>();
            _wtr = Ioc.Default.GetService<StoryWriter>();
            _rdr = Ioc.Default.GetService<StoryReader>();

            DateCreated = string.Empty;
            Author = string.Empty;
            DateModified = string.Empty;
            StoryType = string.Empty;
            StoryGenre = string.Empty;
            LiteraryDevice = string.Empty;
            Viewpoint = string.Empty;
            Style = string.Empty;
            Tone = string.Empty;
            StoryIdea = string.Empty;
            Concept = string.Empty;
            Premise = string.Empty;
            PremiseLock = true;     // Premise is read-only and is only set when a story problem is selected
            StyleNotes = string.Empty;
            ToneNotes = string.Empty;
            Notes = string.Empty;

            Dictionary<string, ObservableCollection<string>> lists = GlobalData.ListControlSource;
            StoryTypeList = lists["StoryType"];
            GenreList = lists["Genre"];
            ViewpointList = lists["Viewpoint"];
            LiteraryDeviceList = lists["LiteraryDevice"];
            VoiceList = lists["Voice"];
            TenseList = lists["Tense"];
            StyleList = lists["LiteraryStyle"];
            ToneList = lists["Tone"];

            PropertyChanged += OnPropertyChanged;

            //CharacterList = CharacterModel.CharacterNames;

            // TODO: Set good defaults for these
            //System.DateTime wrkDate = DateTime.FromOADate(0);
            //wrkDate = DateTime.Parse(Convert.ToDateTime(StoryRec.DateCreated.Value).ToString("MM/dd/yy"));
            //frmStory.DefInstance.mskDateCreated.Text = StringsHelper.Format(wrkDate, "Medium Date");

        }

        #endregion
    }
}