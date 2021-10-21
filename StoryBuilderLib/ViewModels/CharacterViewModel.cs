using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using StoryBuilder.Controllers;
using StoryBuilder.DAL;
using StoryBuilder.Models;
using StoryBuilder.Services.Dialogs;
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
    public class CharacterViewModel : ObservableRecipient, INavigable
    {
        #region Fields

        private readonly StoryReader _rdr;
        private readonly StoryWriter _wtr;
        private readonly LogService _logger;
        private readonly StoryController _story;
        private StoryModel _storyModel;
        private bool _changeable;
        private RelationshipModel _currentRelationship;

        #endregion

        #region Properties

        #region Relay Commands
        public RelayCommand AddTraitCommand { get; }
        public RelayCommand RemoveTraitCommand { get; }
        public RelayCommand AddRelationshipCommand { get; }
        public RelayCommand RemoveRelationshipCommand { get; }

        #endregion

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

        // Character role data

        private string _role;

        public string Role
        {
            get => _role;
            set => SetProperty(ref _role, value);
        }

        private string _storyRole;

        public string StoryRole
        {
            get => _storyRole;
            set => SetProperty(ref _storyRole, value);
        }

        private string _archetype;

        public string Archetype
        {
            get => _archetype;
            set => SetProperty(ref _archetype, value);
        }

        private string _characterSketch;

        public string CharacterSketch
        {
            get => _characterSketch;
            set => SetProperty(ref _characterSketch, value);
        }

        // Character physical data

        private string _age;

        public string Age
        {
            get => _age;
            set => SetProperty(ref _age, value);
        }

        private string _sex;

        public string Sex
        {
            get => _sex;
            set => SetProperty(ref _sex, value);
        }

        private string _eyes;

        public string Eyes
        {
            get => _eyes;
            set => SetProperty(ref _eyes, value);
        }

        private string _hair;

        public string Hair
        {
            get => _hair;
            set => SetProperty(ref _hair, value);
        }

        private string _weight;

        public string Weight
        {
            get => _weight;
            set => SetProperty(ref _weight, value);
        }

        private string _charHeight;

        public string CharHeight
        {
            get => _charHeight;
            set => SetProperty(ref _charHeight, value);
        }

        private string _build;

        public string Build
        {
            get => _build;
            set => SetProperty(ref _build, value);
        }

        private string _complexion;

        public string Complexion
        {
            get => _complexion;
            set => SetProperty(ref _complexion, value);
        }

        private string _race;

        public string Race
        {
            get => _race;
            set => SetProperty(ref _race, value);
        }

        private string _nationality;

        public string Nationality
        {
            get => _nationality;
            set => SetProperty(ref _nationality, value);
        }

        private string _health;

        public string Health
        {
            get => _health;
            set => SetProperty(ref _health, value);
        }

        private string _physNotes;

        public string PhysNotes
        {
            get => _physNotes;
            set => SetProperty(ref _physNotes, value);
        }

        // Character appearance data

        private string _appearance;

        public string Appearance
        {
            get => _appearance;
            set => SetProperty(ref _appearance, value);
        }

        // Relationship data 
        public ObservableCollection<RelationshipModel> CharacterRelationships { get; set; }

        private RelationshipModel _selectedRelationship;
        public RelationshipModel SelectedRelationship
        {
            get => _selectedRelationship;
            //set => SetProperty(ref _selectedRelationship, value);
            set => _selectedRelationship = value;

        }

        private ObservableCollection<string> _relationships;
        public ObservableCollection<string> Relationships
        {
            get => _relationships;
            set => SetProperty(ref _relationships, value);
        }

        private string _relationType;
        public string RelationType
        {
            get => _relationType;
            set => SetProperty(ref _relationType, value);
        }

        private string _relationshipTrait;
        public string RelationshipTrait
        {
            get => _relationshipTrait;
            set => SetProperty(ref _relationshipTrait, value);
        }

        private string _relationshipAttitude;
        public string RelationshipAttitude
        {
            get => _relationshipAttitude;
            set => SetProperty(ref _relationshipAttitude, value);
        }

        private string _relationshipNotes;

        public string RelationshipNotes
        {
            get => _relationshipNotes;
            set => SetProperty(ref _relationshipNotes, value);
        }

        private string _newRelationshipMember;
        public string NewRelationshipMember
        {
            get => _newRelationshipMember;
            set
            {
                if (RelationshipExists(value))
                {
                    var _smsg = new StatusMessage("Character is already in Relationships", 200);
                    Messenger.Send(new StatusChangedMessage(_smsg));
                }
                SetProperty(ref _newRelationshipMember, value);
                StoryElement element = StringToStoryElement(value);
                string msg = String.Format("New cast member selected", element.Name);
                var smsg = new StatusMessage(msg, 200);
                Messenger.Send(new StatusChangedMessage(smsg));
            }
        }

        // Character social data

        private string _economic;

        public string Economic
        {
            get => _economic;
            set => SetProperty(ref _economic, value);
        }

        private string _education;

        public string Education
        {
            get => _education;
            set => SetProperty(ref _education, value);
        }

        private string _ethnic;

        public string Ethnic
        {
            get => _ethnic;
            set => SetProperty(ref _ethnic, value);
        }

        private string _religion;

        public string Religion
        {
            get => _religion;
            set => SetProperty(ref _religion, value);
        }

        // Character psych data

        private string _enneagram;

        public string Enneagram
        {
            get => _enneagram;
            set => SetProperty(ref _enneagram, value);
        }

        private string _intelligence;

        public string Intelligence
        {
            get => _intelligence;
            set => SetProperty(ref _intelligence, value);
        }

        private string _values;

        public string Values
        {
            get => _values;
            set => SetProperty(ref _values, value);
        }

        private string _abnormality;

        public string Abnormality
        {
            get => _abnormality;
            set => SetProperty(ref _abnormality, value);
        }

        private string _focus;

        public string Focus
        {
            get => _focus;
            set => SetProperty(ref _focus, value);
        }

        private string _psychNotes;

        public string PsychNotes
        {
            get => _psychNotes;
            set => SetProperty(ref _psychNotes, value);
        }

        // Character trait data

        private string _adventurousness;

        public string Adventureousness
        {
            get => _adventurousness;
            set => SetProperty(ref _adventurousness, value);
        }

        private string _agression;

        public string Aggression
        {
            get => _agression;
            set => SetProperty(ref _agression, value);
        }

        private string _confidence;

        public string Confidence
        {
            get => _confidence;
            set => SetProperty(ref _confidence, value);
        }

        private string _conscientiousness;

        public string Conscientiousness
        {
            get => _conscientiousness;
            set => SetProperty(ref _conscientiousness, value);
        }

        private string _creativity;

        public string Creativity
        {
            get => _creativity;
            set => SetProperty(ref _creativity, value);
        }

        private string _dominance;

        public string Dominance
        {
            get => _dominance;
            set => SetProperty(ref _dominance, value);
        }

        private string _enthusiasm;

        public string Enthusiasm
        {
            get => _enthusiasm;
            set => SetProperty(ref _enthusiasm, value);
        }

        private string _assurance;

        public string Assurance
        {
            get => _assurance;
            set => SetProperty(ref _assurance, value);
        }

        private string _sensitivity;

        public string Sensitivity
        {
            get => _sensitivity;
            set => SetProperty(ref _sensitivity, value);
        }

        private string _shrewdness;

        public string Shrewdness
        {
            get => _shrewdness;
            set => SetProperty(ref _shrewdness, value);
        }

        private string _sociability;

        public string Sociability
        {
            get => _sociability;
            set => SetProperty(ref _sociability, value);
        }

        private string _stability;

        public string Stability
        {
            get => _stability;
            set => SetProperty(ref _stability, value);
        }


        // Character work data

        private string _work;

        public string Work
        {
            get => _work;
            set => SetProperty(ref _work, value);
        }

        // Character likes data

        private string _likes;

        public string Likes
        {
            get => _likes;
            set => SetProperty(ref _likes, value);
        }

        // Character habits data

        private string _habits;

        public string Habits
        {
            get => _habits;
            set => SetProperty(ref _habits, value);
        }

        // Character abilities data

        private string _abilities;

        public string Abilities
        {
            get => _abilities;
            set => SetProperty(ref _abilities, value);
        }

        // Character flaw data

        private string _woundCategory;
        public string WoundCategory
        {
            get => _woundCategory;
            set => SetProperty(ref _woundCategory, value);
        }

        private string _woundSummary;
        public string WoundSummary
        {
            get => _woundSummary;
            set => SetProperty(ref _woundSummary, value);
        }

        private string _wound;
        public string Wound
        {
            get => _wound;
            set => SetProperty(ref _wound, value);
        }

        private string _fears;
        public string Fears
        {
            get => _fears;
            set => SetProperty(ref _fears, value);
        }

        private string _lies;
        public string Lies
        {
            get => _lies;
            set => SetProperty(ref _lies, value);
        }

        private string _secrets;
        public string Secrets
        {
            get => _secrets;
            set => SetProperty(ref _secrets, value);
        }

        // Character notes data

        private string _backStory;
        public string BackStory
        {
            get => _backStory;
            set => SetProperty(ref _backStory, value);
        }

        // Besides its GUID, each Character has a unique (to this story) 
        // integer id number (useful in lists of characters.)
        private int _id;

        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        // The StoryModel is passed when CharacterPage is navigated to
        private CharacterModel _model;
        public CharacterModel Model
        {
            get => _model;
            set => _model = value;
        }

        // The Changed bit tracks any change to this ViewModel.
        private bool _changed;
        public bool Changed
        {
            get => _changed;
            set => _changed = value;
        }

        // Traits info

        private ObservableCollection<string> _characterTraits;
        public ObservableCollection<string> CharacterTraits
        {
            get => _characterTraits;
            set => SetProperty(ref _characterTraits, value);
        }

        private int _existingTraitIndex;
        public int ExistingTraitIndex
        {
            get => _existingTraitIndex;
            set => SetProperty(ref _existingTraitIndex, value);
        }

        private string _newTrait;
        public string NewTrait 
        {
            get => _newTrait;
            set => SetProperty(ref _newTrait, value);
        }

         #endregion

        #region Public Methods

        public async Task Activate(object parameter)
        {
            Model = (CharacterModel)parameter;
            await LoadModel(); // Load the ViewModel from the Story
        }

        public async Task Deactivate(object parameter)
        {
            await SaveModel(); // Save the ViewModel back to the Story
        }

        /// Instead of loading a Character's RelationshipModels directly into
        /// the ViewModel and binding them, the models themselves are loaded 
        /// into the VM's CharacterRelationships ObservableCollection, but
        /// its properties are bound only when one of of the ComboBox items
        /// CharacterRelationships is bound to is selected.
        /// However, one property need modified during LoadModel: the Partner  
        /// StoryElement in the RelationshipModel needs loaded from its Uuid.
        public async Task RelationshipChanged(object sender, SelectionChangedEventArgs args)
        {
            await SaveRelationship(_currentRelationship);
            await LoadRelationship(SelectedRelationship);
            _currentRelationship = SelectedRelationship;
        }

        public async Task SaveRelationships()
        {
            await SaveRelationship(_currentRelationship);  // Save any current changes
            _currentRelationship = null;
            // Move relationships back to the character model
            Model.RelationshipList.Clear();
            foreach (RelationshipModel relation in CharacterRelationships)
                Model.RelationshipList.Add(relation);
        }

        #endregion

        #region Private Methods
        private void OnPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
             if (_changeable)
                Changed = true;
        }

        private async Task LoadModel()
        {
            PropertyChanged += OnPropertyChanged;
            Uuid = Model.Uuid;
            Name = Model.Name;
            Role = Model.Role;
            StoryRole = Model.StoryRole;
            Archetype = Model.Archetype;
            CharacterSketch = Model.CharacterSketch;
            Age = Model.Age;
            Sex = Model.Sex;
            Eyes = Model.Eyes;
            Hair = Model.Hair;
            Weight = Model.Weight;
            CharHeight = Model.CharHeight;
            Build = Model.Build;
            Complexion = Model.Complexion;
            Race = Model.Race;
            Nationality = Model.Nationality;
            Health = Model.Health;
            PhysNotes = Model.PhysNotes;
            Appearance = Model.Appearance;
            Economic = Model.Economic;
            Education = Model.Education;
            Ethnic = Model.Ethnic;
            Religion = Model.Religion;
            Enneagram = Model.Enneagram;
            Intelligence = Model.Intelligence;
            Values = Model.Values;
            Abnormality = Model.Abnormality;
            Focus = Model.Focus;
            PsychNotes = Model.PsychNotes;
            Adventureousness = Model.Adventureousness;
            Aggression = Model.Aggression;
            Confidence = Model.Confidence;
            Conscientiousness = Model.Conscientiousness;
            Creativity = Model.Creativity;
            Dominance = Model.Dominance;
            Enthusiasm = Model.Enthusiasm;
            Assurance = Model.Assurance;
            Sensitivity = Model.Sensitivity;
            Shrewdness = Model.Shrewdness;
            Sociability = Model.Sociability;
            Stability = Model.Stability;
            Work = Model.Work;
            Likes = Model.Likes;
            Habits = Model.Habits;
            Abilities = Model.Abilities;
            WoundCategory = Model.WoundCategory;
            WoundSummary = Model.WoundSummary;
            Wound = Model.Wound;
            Fears = Model.Fears;
            Lies = Model.Lies;
            Secrets = Model.Secrets;
            BackStory = Model.BackStory;
            Id = Model.Id;

             //Read RTF files
            CharacterSketch = await _rdr.GetRtfText(Model.CharacterSketch, Uuid);
            PhysNotes = await _rdr.GetRtfText(Model.PhysNotes, Uuid);
            Appearance = await _rdr.GetRtfText(Model.Appearance, Uuid);
            Economic = await _rdr.GetRtfText(Model.Economic, Uuid);
            Education = await _rdr.GetRtfText(Model.Education, Uuid);
            Ethnic = await _rdr.GetRtfText(Model.Ethnic, Uuid);
            Religion = await _rdr.GetRtfText(Model.Religion, Uuid);
            PsychNotes = await _rdr.GetRtfText(Model.PsychNotes, Uuid);
            Work = await _rdr.GetRtfText(Model.Work, Uuid);
            Likes = await _rdr.GetRtfText(Model.Likes, Uuid);
            Habits = await _rdr.GetRtfText(Model.Habits, Uuid);
            Abilities = await _rdr.GetRtfText(Model.Abilities, Uuid);
            Wound = await _rdr.GetRtfText(Model.Wound, Uuid);
            Fears = await _rdr.GetRtfText(Model.Fears, Uuid);
            Lies = await _rdr.GetRtfText(Model.Lies, Uuid);
            Secrets = await _rdr.GetRtfText(Model.Secrets, Uuid);
            BackStory = await _rdr.GetRtfText(Model.BackStory, Uuid);

            CharacterTraits.Clear();
            foreach (string member in Model.TraitList)
                CharacterTraits.Add(member);

            RelationType = string.Empty;
            RelationshipTrait = string.Empty;
            RelationshipAttitude = string.Empty;
            RelationshipNotes = string.Empty;
            SelectedRelationship = null;
            _currentRelationship = null;
            CharacterRelationships.Clear();
            foreach (RelationshipModel relation in Model.RelationshipList)
            {
                relation.Partner = StringToStoryElement(relation.PartnerUuid); // Populate partner Character StoryElement for Name
                CharacterRelationships.Add(relation);
            }

            Changed = false;
            _changeable = true;
        }

        internal async Task SaveModel()
        {
            PropertyChanged -= OnPropertyChanged;
            _changeable = false;

            if (Changed)
            {
                // Story.Uuid is read-only and cannot be set
                Model.Name = Name;
                Model.Role = Role;
                Model.StoryRole = StoryRole;
                Model.Archetype = Archetype;
                Model.Age = Age;
                Model.Sex = Sex;
                Model.Eyes = Eyes;
                Model.Hair = Hair;
                Model.Weight = Weight;
                Model.CharHeight = CharHeight;
                Model.Build = Build;
                Model.Complexion = Complexion;
                Model.Race = Race;
                Model.Nationality = Nationality;
                Model.Health = Health;
                Model.Enneagram = Enneagram;
                Model.Intelligence = Intelligence;
                Model.Values = Values;
                Model.Abnormality = Abnormality;
                Model.Focus = Focus;
                Model.Adventureousness = Adventureousness;
                Model.Aggression = Aggression;
                Model.Confidence = Confidence;
                Model.Conscientiousness = Conscientiousness;
                Model.Creativity = Creativity;
                Model.Dominance = Dominance;
                Model.Enthusiasm = Enthusiasm;
                Model.Assurance = Assurance;
                Model.Sensitivity = Sensitivity;
                Model.Shrewdness = Shrewdness;
                Model.Sociability = Sociability;
                Model.Stability = Stability;
                Model.TraitList.Clear();
                
                foreach (string element in CharacterTraits)
                    Model.TraitList.Add(element);
                
                 await SaveRelationship(_currentRelationship);  // Save any current changes
                _currentRelationship = null;
                // Move relationships back to the character model
                Model.RelationshipList.Clear();
                foreach (RelationshipModel relation in CharacterRelationships)
                    Model.RelationshipList.Add(relation);
                Model.Abilities = Abilities;
                Model.WoundCategory = WoundCategory;
                Model.WoundSummary = WoundSummary;
                Model.Wound = Wound;
                Model.Fears = Fears;
                Model.Lies = Lies;
                Model.Secrets = Secrets;
                Model.BackStory = BackStory;
                Model.Id = Id;

                // Write and clear RTF files
                Model.CharacterSketch = await _wtr.PutRtfText(CharacterSketch, Uuid, "charactersketch.rtf");
                Model.PhysNotes = await _wtr.PutRtfText(PhysNotes, Uuid, "physnotes.rtf");
                Model.Appearance = await _wtr.PutRtfText(Appearance, Uuid, "appearance.rtf");
                Model.Economic = await _wtr.PutRtfText(Economic, Uuid, "economic.rtf");
                Model.Education = await _wtr.PutRtfText(Education, Uuid, "education.rtf");
                Model.Ethnic = await _wtr.PutRtfText(Ethnic, Uuid, "ethnic.rtf");
                Model.Religion = await _wtr.PutRtfText(Religion, Uuid, "religion.rtf");
                Model.PsychNotes = await _wtr.PutRtfText(PsychNotes, Uuid, "psychnotes.rtf");
                Model.Work = await _wtr.PutRtfText(Work, Uuid, "work.rtf");
                Model.Likes = await _wtr.PutRtfText(Likes, Uuid, "likes.rtf");
                Model.Habits = await _wtr.PutRtfText(Habits, Uuid, "habits.rtf");
                Model.Abilities = await _wtr.PutRtfText(Abilities, Uuid, "abilities.rtf");
                Model.Wound = await _wtr.PutRtfText(Wound, Uuid, "wound.rtf");
                Model.Fears = await _wtr.PutRtfText(Fears, Uuid, "fears.rtf");
                Model.Lies = await _wtr.PutRtfText(Lies, Uuid, "lies.rtf");
                Model.Secrets = await _wtr.PutRtfText(Secrets, Uuid, "secrets.rtf");
                Model.BackStory = await _wtr.PutRtfText(BackStory, Uuid, "backstory.rtf");

                _logger.Log(LogLevel.Info, string.Format("Requesting IsDirty change to true"));
                Messenger.Send(new IsChangedMessage(Changed));
            }
        }

        private void AddTrait()
        {
            CharacterTraits.Add(NewTrait);
        }
        
        private void RemoveTrait() 
        {
            if (ExistingTraitIndex == -1) 
            {
                var _smsg = new StatusMessage("No trait selected to delete", 200);
                Messenger.Send(new StatusChangedMessage(_smsg));
                return;
            }
            CharacterTraits.RemoveAt(ExistingTraitIndex);
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

        /// <summary>
        /// Load and bind a RelationshipModel instance
        /// </summary>
        /// <param name="selectedRelation">The RelationShipModel just selected or added</param>
        private async Task LoadRelationship(RelationshipModel selectedRelation)
        {
            if (selectedRelation == null)
                return;
            _changeable = false;
            RelationType = selectedRelation.RelationType;
            RelationshipTrait = selectedRelation.Trait;
            RelationshipAttitude = selectedRelation.Attitude;
            RelationshipNotes = await _rdr.GetRtfText(selectedRelation.Notes, Uuid);

            _changeable = true;
        }

        private async Task SaveRelationship(RelationshipModel selectedRelation)
        {
            if (!Changed || selectedRelation == null)
                return;
            selectedRelation.Trait = RelationshipTrait;
            selectedRelation.Attitude = RelationshipAttitude;
            selectedRelation.Notes = await _wtr.PutRtfText(RelationshipNotes, Uuid, selectedRelation.PartnerUuid + "_notes.rtf");
            _logger.Log(LogLevel.Info, string.Format("Requesting IsDirty change to true"));
            Messenger.Send(new IsChangedMessage(Changed));
        }

        /// <summary>
        /// Add a new RelationshipModel instance for this character.
        /// 
        /// The added relationship is made the currently loaded and displayed one.
        /// </summary>
        private async void AddRelationship()
        {
            _logger.Log(LogLevel.Info, "Executing AddRelationship command");
            await SaveRelationship(_currentRelationship);

            NewRelationshipDialog dialog = new();
            dialog.XamlRoot = GlobalData.XamlRoot;
            NewRelationshipViewModel vm = new NewRelationshipViewModel(Model);
            dialog.NewRelVM = vm;
            vm.RelationTypes.Clear();
            foreach (RelationType relationType in GlobalData.RelationTypes)
                vm.RelationTypes.Add(relationType);
            vm.ProspectivePartners.Clear();
            foreach (StoryElement character in _storyModel.StoryElements.Characters)
            {
                if (character == vm.Member) continue;  // Skip me
                foreach (RelationshipModel rel in CharacterRelationships)
                {
                    if (character == rel.Partner) goto NextCharacter; // Skip partner
                }
                vm.ProspectivePartners.Add(character);
            NextCharacter: continue;
            }
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                try
                {
                    // Create the new RelationshipModel
                    string partnerUuid = StoryWriter.UuidString(vm.SelectedPartner.Uuid);
                    RelationshipModel memberRelationship = new RelationshipModel(partnerUuid, vm.RelationType);
                    // Complete pairing 
                    // Add partner relationship to member's list of relationships 
                    CharacterRelationships.Add(memberRelationship);
                    SelectedRelationship = memberRelationship;
                    await LoadRelationship(SelectedRelationship);
                    _currentRelationship = SelectedRelationship;

                    Changed = true;
                    string msg = String.Format("Relationship to {0} added", vm.SelectedPartner.Name);
                    var smsg = new StatusMessage(msg, 200);
                    Messenger.Send(new StatusChangedMessage(smsg));
                    _logger.Log(LogLevel.Info, msg);

                    _logger.Log(LogLevel.Info, string.Format("Requesting IsDirty change to true"));
                    Messenger.Send(new IsChangedMessage(Changed));
                }
                catch (Exception ex)
                {
                    string msg = "Error creating new Relationship";
                    _logger.LogException(LogLevel.Error, ex, msg);
                    var smsg = new StatusMessage(msg, 200);
                    Messenger.Send(new StatusChangedMessage(smsg));
                }
            }
            else
            {
                string msg = "AddRelationship cancelled";
                _logger.Log(LogLevel.Info, msg);
                var smsg = new StatusMessage(msg, 200);
                Messenger.Send(new StatusChangedMessage(smsg));
            }
        }

        private async void RemoveRelationship()
        {
            _logger.Log(LogLevel.Info, "Executing RemoveRelationship command");
            string msg;
            // verify that I have an active relationship
            if (SelectedRelationship == null)
            {
                _logger.Log(LogLevel.Warn, "A relationship must be active to be removed");
                msg = "A relationship must be active to be removed";
                var smsg = new StatusMessage(msg, 200);
                Messenger.Send(new StatusChangedMessage(smsg));
                _logger.Log(LogLevel.Warn, "A relationship must be active to be removed");
                return;
            }
            RelationshipModel rel = SelectedRelationship;
            // display verification message
            StoryElement partner = rel.Partner;
            msg = string.Format("Delete relationship {0} ", partner.Name);
            msg += Environment.NewLine;
            msg += "(and inverse relationship)";
            ContentDialog dialog = new ContentDialog()
            {

                Title = "Remove Relationship",
                Content = msg,
                PrimaryButtonText = "Yes",
                SecondaryButtonText = "No"
            };
            dialog.XamlRoot = GlobalData.XamlRoot;

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                msg = string.Format("Relationship to {0} deleted", partner.Name);
                msg = string.Format("Relationship to {0} deleted", partner.Name);
                SelectedRelationship = null;
                // log and display status
                _logger.Log(LogLevel.Info, msg);
                var smsg = new StatusMessage(msg, 200);
                Messenger.Send(new StatusChangedMessage(smsg));
                _logger.Log(LogLevel.Info, string.Format("Requesting IsDirty change to true"));
                Messenger.Send(new IsChangedMessage(Changed));
            }
            else
            {
                _logger.Log(LogLevel.Info, "Remove relationship cancelled");
                msg = "RemoveRelationship cancelled";
                _logger.Log(LogLevel.Info, msg);
                var smsg = new StatusMessage(msg, 200);
                Messenger.Send(new StatusChangedMessage(smsg));
            }
        }

        private void ClearActiveRelationship()
        {
            _changeable = false;
            RelationType = string.Empty;
            RelationshipTrait = string.Empty;
            RelationshipAttitude = string.Empty;
            RelationshipNotes = string.Empty;
            SelectedRelationship = null;
            _currentRelationship = null;
            _changeable = true;
        }

        /// <summary>
        /// Test if the relationship to be added already exists.
        /// </summary>
        /// <param name="uuid">uuid of Partner to add</param>
        /// <returns>true if found, false othewise</returns>
        private bool RelationshipExists(string uuid)
        {
            StoryElement character = StringToStoryElement(uuid);
            foreach (RelationshipModel relationship in CharacterRelationships)
            {
                if (character == relationship.Partner)
                    return true;
            }
            return false;
        }

        #endregion

        #region ComboBox ItemsSource collections

        public ObservableCollection<string> RoleList;
        public ObservableCollection<string> StoryRoleList;
        public ObservableCollection<string> ArchetypeList;
        public ObservableCollection<string> BuildList;
        public ObservableCollection<string> NationalityList;
        public ObservableCollection<string> RelationshipTraitList;
        public ObservableCollection<string> RelationshipAttitudeList;

        // TODO: How do I bind to Sex option buttons?
        public ObservableCollection<string> EyesList;
        public ObservableCollection<string> HairList;
        public ObservableCollection<string> SkinList;
        public ObservableCollection<string> RaceList;
        public ObservableCollection<string> EnneagramList;
        public ObservableCollection<string> IntelligenceList;
        public ObservableCollection<string> ValuesList;
        public ObservableCollection<string> AbnormalityList;
        public ObservableCollection<string> FocusList;
        public ObservableCollection<string> AdventureousnessList;
        public ObservableCollection<string> AggressionList;
        public ObservableCollection<string> ConfidenceList;
        public ObservableCollection<string> ConscientiousnessList;
        public ObservableCollection<string> CreativityList;
        public ObservableCollection<string> DominanceList;
        public ObservableCollection<string> EnthusiasmList;
        public ObservableCollection<string> AssuranceList;
        public ObservableCollection<string> SensitivityList;
        public ObservableCollection<string> ShrewdnessList;
        public ObservableCollection<string> SociabilityList;
        public ObservableCollection<string> TraitList;
        public ObservableCollection<string> StabilityList;
        public ObservableCollection<string> WoundCategoryList;
        public ObservableCollection<string> WoundSummaryList;

        #endregion

        #region Constructors

        public CharacterViewModel()
        {
            ShellViewModel shell = Ioc.Default.GetService<ShellViewModel>();
            _storyModel = shell.StoryModel;
            _story = Ioc.Default.GetService<StoryController>();
            _logger = Ioc.Default.GetService<LogService>();
            _wtr = Ioc.Default.GetService<StoryWriter>();
            _rdr = Ioc.Default.GetService<StoryReader>();
            Ioc.Default.GetService<NewProjectViewModel>();

            PropertyChanged += OnPropertyChanged;


            Dictionary<string, ObservableCollection<string>> lists = GlobalData.ListControlSource;
            RoleList = lists["Role"];
            StoryRoleList = lists["StoryRole"];
            ArchetypeList = lists["Archetype"];
            BuildList = lists["Build"];
            NationalityList = lists["Country"];
            // TODO: How do I bind to Sex option buttons?
            EyesList = lists["EyeColor"];
            HairList = lists["HairColor"];
            SkinList = lists["Complexion"];
            RaceList = lists["Race"];
            EnneagramList = lists["Enneagram"];
            IntelligenceList = lists["Intelligence"];
            ValuesList = lists["Value"];
            AbnormalityList = lists["MentalIllness"];
            FocusList = lists["Focus"];
            AdventureousnessList = lists["Adventurous"];
            AggressionList = lists["Aggressiveness"];
            ConfidenceList = lists["Confidence"];
            ConscientiousnessList = lists["Conscientiousness"];
            CreativityList = lists["Creativeness"];
            DominanceList = lists["Dominance"];
            EnthusiasmList = lists["Enthusiasm"];
            AssuranceList = lists["Assurance"];
            SensitivityList = lists["Sensitivity"];
            ShrewdnessList = lists["Shrewdness"];
            SociabilityList = lists["Sociability"];
            StabilityList = lists["Stability"];
            TraitList = lists["Trait"];
            WoundCategoryList = lists["WoundCategory"];
            WoundSummaryList = lists["Wound"];
            RelationshipTraitList = lists["Trait"];
            RelationshipAttitudeList = lists["Attitude"];

            CharacterTraits = new ObservableCollection<string>();
            CharacterRelationships = new ObservableCollection<RelationshipModel>();

            AddTraitCommand = new RelayCommand(AddTrait, () => true);
            RemoveTraitCommand = new RelayCommand(RemoveTrait, () => true);
            AddRelationshipCommand = new RelayCommand(AddRelationship, () => true);
            RemoveRelationshipCommand = new RelayCommand(RemoveRelationship, () => true);

            Role = string.Empty;
            StoryRole = string.Empty;
            Archetype = string.Empty;
            CharacterSketch = string.Empty;
            Age = string.Empty;
            Sex = string.Empty;
            Eyes = string.Empty;
            Hair = string.Empty;
            Weight = string.Empty;
            CharHeight = string.Empty;
            Build = string.Empty;
            Complexion = string.Empty;
            Race = string.Empty;
            Nationality = string.Empty;
            Health = string.Empty;
            PhysNotes = string.Empty;
            Appearance = string.Empty;
            Economic = string.Empty;
            Education = string.Empty;
            Ethnic = string.Empty;
            Religion = string.Empty;
            Enneagram = string.Empty;
            Intelligence = string.Empty;
            Values = string.Empty;
            Abnormality = string.Empty;
            Focus = string.Empty;
            PsychNotes = string.Empty;
            Adventureousness = string.Empty;
            Aggression = string.Empty;
            Confidence = string.Empty;
            Conscientiousness = string.Empty;
            Creativity = string.Empty;
            Dominance = string.Empty;
            Enthusiasm = string.Empty;
            Assurance = string.Empty;
            Sensitivity = string.Empty;
            Shrewdness = string.Empty;
            Sociability = string.Empty;
            Stability = string.Empty;
            Work = string.Empty;
            Likes = string.Empty;
            Habits = string.Empty;
            Abilities = string.Empty;
            WoundCategory = string.Empty;
            WoundSummary = string.Empty;
            Wound = string.Empty;
            BackStory = string.Empty;
        }
    }

    #endregion
}

