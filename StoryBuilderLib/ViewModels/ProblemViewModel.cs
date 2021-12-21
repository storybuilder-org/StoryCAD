﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using ABI.Windows.ApplicationModel.Contacts.DataProvider;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using StoryBuilder.Controllers;
using StoryBuilder.Controls;
using StoryBuilder.DAL;
using StoryBuilder.Models;
using StoryBuilder.Services.Logging;
using StoryBuilder.Services.Messages;
using StoryBuilder.Services.Navigation;
using StoryBuilder.Services.Dialogs.Tools;
using WinRT;

namespace StoryBuilder.ViewModels
{
    public class ProblemViewModel : ObservableRecipient, INavigable
    {
        #region Fields

        private StoryModel _storyModel;
        private readonly StoryController _story;
        private readonly LogService _logger;
        internal readonly StoryReader _rdr;
        private readonly StoryWriter _wtr;
        private bool _changeable;
        private bool _changed;

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

        private int _id;
        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        // Problem problem data
        private string _problemType;
        public string ProblemType
        {
            get => _problemType;
            set => SetProperty(ref _problemType, value);
        }

        private string _conflictType;

        private string _subject;
        public string Subject
        {
            get => _subject;
            set => SetProperty(ref _subject, value);
        }

        private string _problemSource;
        public string ProblemSource
        {
            get => _problemSource;
            set => SetProperty(ref _problemSource, value);
        }

        public string ConflictType
        {
            get => _conflictType;
            set => SetProperty(ref _conflictType, value);
        }
        private string _storyQuestion;
        public string StoryQuestion
        {
            get => _storyQuestion;
            set => SetProperty(ref _storyQuestion, value);
        }

        // Problem protagonist data

        private string _protagonist;  // The Guid of a Character StoryElement
        public string Protagonist
        {
            get => _protagonist;
            set => SetProperty(ref _protagonist, value);
        }

        private string _protGoal;
        public string ProtGoal
        {
            get => _protGoal;
            set => SetProperty(ref _protGoal, value);
        }

        private string _protMotive;
        public string ProtMotive
        {
            get => _protMotive;
            set => SetProperty(ref _protMotive, value);
        }

        private string _protConflict;
        public string ProtConflict
        {
            get => _protConflict;
            set => SetProperty(ref _protConflict, value);
        }

        // Problem antagonist data

        private string _antagonist;  // The Guid of a Character StoryElement
        public string Antagonist
        {
            get => _antagonist;
            set => SetProperty(ref _antagonist, value);
        }

        private string _antagGoal;
        public string AntagGoal
        {
            get => _antagGoal;
            set => SetProperty(ref _antagGoal, value);
        }

        private string _antagMotive;
        public string AntagMotive
        {
            get => _antagMotive;
            set => SetProperty(ref _antagMotive, value);
        }

        private string _antagConflict;
        public string AntagConflict
        {
            get => _antagConflict;
            set => SetProperty(ref _antagConflict, value);
        }

        // Problem resolution data

        private string _outcome;
        public string Outcome
        {
            get => _outcome;
            set => SetProperty(ref _outcome, value);
        }

        private string _method;
        public string Method
        {
            get => _method;
            set => SetProperty(ref _method, value);
        }

        private string _theme;
        public string Theme
        {
            get => _theme;
            set => SetProperty(ref _theme, value);
        }

        private string _premise;
        public string Premise
        {
            get => _premise;
            set => SetProperty(ref _premise, value);
        }

        // Problem notes data

        private string _notes;
        public string Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
        }

        // The ProblemModel is passed when ProblemPage is navigated to
        private ProblemModel _model;
        public ProblemModel Model
        {
            get => _model;
            set => SetProperty(ref _model, value);
        }

        public RelayCommand ConflictCommand { get; }

        #endregion

        #region Methods

        public async Task Activate(object parameter)
        {
            Model = (ProblemModel)parameter;
            await LoadModel();
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
            ProblemType = Model.ProblemType;
            ConflictType = Model.ConflictType;
            Subject = Model.Subject;
            ProblemSource = Model.ProblemSource;
            //StoryQuestion = Model.StoryQuestion;
            // Character instances like Protagonist and Antagonist are 
            // read and written as the CharacterModel's StoryElement Guid 
            // string. A binding converter, StringToStoryElementConverter,
            // provides the UI the corresponding StoryElement itself.f
            Protagonist = Model.Protagonist ?? string.Empty;
            ProtGoal = Model.ProtGoal;
            ProtMotive = Model.ProtMotive;
            ProtConflict = Model.ProtConflict;
            Antagonist = Model.Antagonist ?? string.Empty;
            AntagGoal = Model.AntagGoal;
            AntagMotive = Model.AntagMotive;
            AntagConflict = Model.AntagConflict;
            Outcome = Model.Outcome;
            Method = Model.Method;
            Theme = Model.Theme;
            //Premise = Model.Premise;
            // Read RTF files
            StoryQuestion = await _rdr.GetRtfText(Model.StoryQuestion, Uuid);
            Premise = await _rdr.GetRtfText(Model.Premise, Uuid);
            Notes = await _rdr.GetRtfText(Model.Notes, Uuid);

            _changeable = true;
            }

        internal async Task SaveModel()
        {
            if (_changed)
            {
                // Story.Uuid is read-only and cannot be assigned
                Model.Name = Name;
                Model.ProblemType = ProblemType;
                Model.ConflictType = ConflictType;
                Model.Subject = Subject;
                Model.ProblemSource = ProblemSource;
                Model.Protagonist = Protagonist ?? string.Empty;
                Model.ProtGoal = ProtGoal;
                Model.ProtMotive = ProtMotive;
                Model.ProtConflict = ProtConflict;
                Model.Antagonist = Antagonist ?? string.Empty;
                Model.AntagGoal = AntagGoal;
                Model.AntagMotive = AntagMotive;
                Model.AntagConflict = AntagConflict;
                Model.Outcome = Outcome;
                Model.Method = Method;
                Model.Theme = Theme;

                // Write RTF files
                Model.StoryQuestion = await _wtr.PutRtfText(StoryQuestion, Model.Uuid, "storyquestion.rtf");
                Model.Premise = await _wtr.PutRtfText(Premise, Model.Uuid, "premise.rtf");
                Model.Notes = await _wtr.PutRtfText(Notes, Model.Uuid, "notes.rtf");

                //_logger.Log(LogLevel.Info, string.Format("Requesting IsDirty change to true"));
                //Messenger.Send(new IsChangedMessage(Changed));
            }
        }

        /// <summary>
        /// Opens conflict builder
        /// </summary>
        public async void ConflictTool()
        {
            _logger.Log(LogLevel.Info, "Displaying Conflict Finder tool dialog");

            //Creates and shows content
            ContentDialog ConflictDialog = new();
            ConflictDialog.Title = "Conflict builder";
            ConflictDialog.XamlRoot = GlobalData.XamlRoot;
            ConflictDialog.PrimaryButtonText = "Copy to Protagonist";
            ConflictDialog.SecondaryButtonText = "Copy to Antagonist";
            ConflictDialog.CloseButtonText = "Close";
            Conflict SelectedConflict = new();
            ConflictDialog.Content = SelectedConflict;
            var result = await ConflictDialog.ShowAsync();

            if (result == ContentDialogResult.Primary)   // Copy to Protagonist conflict
            {
                ProtConflict = SelectedConflict.ExampleText;
            }
            else if (result == ContentDialogResult.Secondary) // Copy to Antagonist conflict
            {
                AntagConflict = SelectedConflict.ExampleText;
            }
            _logger.Log(LogLevel.Info, "Conflict Finder finished");
        }

        #endregion

        #region Control initialization sources

        // ListControls sources
        public ObservableCollection<string> ProblemTypeList;
        public ObservableCollection<string> ConflictTypeList;
        public ObservableCollection<string> SubjectList;
        public ObservableCollection<string> ProblemSourceList;
        public ObservableCollection<string> GoalList;
        public ObservableCollection<string> MotiveList;
        public ObservableCollection<string> ConflictList;
        public ObservableCollection<string> OutcomeList;
        public ObservableCollection<string> MethodList;
        public ObservableCollection<string> ThemeList;

        public ICollectionView CharacterList;

        #endregion;

        #region Constructors

        public ProblemViewModel()
        {
            ShellViewModel shell = Ioc.Default.GetService<ShellViewModel>();
            _storyModel = shell.StoryModel;
            _story = Ioc.Default.GetService<StoryController>();
            _logger = Ioc.Default.GetService<LogService>();
            _wtr = Ioc.Default.GetService<StoryWriter>();
            _rdr = Ioc.Default.GetService<StoryReader>();

            ProblemType = string.Empty;
            ConflictType = string.Empty;
            Subject = string.Empty;
            ProblemSource = string.Empty;
            StoryQuestion = string.Empty;
            Protagonist = null;
            ProtGoal = string.Empty;
            ProtMotive = string.Empty;
            ProtConflict = string.Empty;
            Antagonist = string.Empty;
            AntagGoal = string.Empty;
            AntagMotive = string.Empty;
            AntagConflict = string.Empty;
            Outcome = string.Empty;
            Method = string.Empty;
            Theme = string.Empty;
            Premise = string.Empty;
            Notes = string.Empty;

            Dictionary<string, ObservableCollection<string>> lists = GlobalData.ListControlSource;
            ProblemTypeList = lists["ProblemType"];
            ConflictTypeList = lists["ConflictType"];
            SubjectList = lists["ProblemSubject"];
            ProblemSourceList = lists["ProblemSource"];
            GoalList = lists["Goal"];
            MotiveList = lists["Motive"];
            ConflictList = lists["Conflict"];
            OutcomeList = lists["Outcome"];
            MethodList = lists["Method"];
            ThemeList = lists["Theme"];

            ConflictCommand = new RelayCommand(ConflictTool, () => true);

            PropertyChanged += OnPropertyChanged;
        }
        #endregion
    }
}
