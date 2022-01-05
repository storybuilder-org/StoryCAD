using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml.Controls;
using StoryBuilder.Controls;
using StoryBuilder.DAL;
using StoryBuilder.Models;
using StoryBuilder.Services.Dialogs;
using StoryBuilder.Services.Logging;
using StoryBuilder.Services.Messages;
using StoryBuilder.Services.Navigation;
using StoryBuilder.ViewModels.Tools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;

namespace StoryBuilder.ViewModels;

public class CharacterViewModel : ObservableRecipient, INavigable
{
    #region Fields

    private readonly StoryReader _rdr;
    private readonly StoryWriter _wtr;
    private readonly LogService _logger;
    public RelationshipModel CurrentRelationship;
    private bool _changeable; // process property changes for this story element
    private bool _changed;    // this story element has changed

    #endregion

    #region Properties

    #region Relay Commands
    public RelayCommand AddTraitCommand { get; }
    public RelayCommand RemoveTraitCommand { get; }
    public RelayCommand AddRelationshipCommand { get; }
    public RelayCommand RemoveRelationshipCommand { get; }
    public RelayCommand FlawCommand { get; }

    public RelayCommand TraitCommand { get; }

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
            if (_changeable && _name != value) // Name changed?
            {
                _logger.Log(LogLevel.Info, $"Requesting Name change from {_name} to {value}");
                NameChangeMessage msg = new(_name, value);
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
                StatusMessage _smsg = new("Character is already in Relationships", 200);
                Messenger.Send(new StatusChangedMessage(_smsg));
            }
            SetProperty(ref _newRelationshipMember, value);
            StoryElement element = StringToStoryElement(value);
            Messenger.Send(new StatusChangedMessage(new StatusMessage($"New cast member selected {element.Name}", 200)));
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

    public string Adventurousness
    {
        get => _adventurousness;
        set => SetProperty(ref _adventurousness, value);
    }

    private string _aggression;

    public string Aggression
    {
        get => _aggression;
        set => SetProperty(ref _aggression, value);
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

    // Character likes data

    private string _notes;

    public string Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value);
    }
         
    // Character flaw data

    private string _flaw   ;
    public string Flaw
    {
        get => _flaw;
        set => SetProperty(ref _flaw, value);
    }

 
    // Character Backstory data 

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

    public async Task SaveRelationships()
    {
        await SaveRelationship(CurrentRelationship);  // Save any current changes
        CurrentRelationship = null;
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
        Adventurousness = Model.Adventureousness;
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
        Notes = Model.Notes;
        Flaw = Model.Flaw;
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
        Notes = await _rdr.GetRtfText(Model.Notes, Uuid);
        Flaw = await _rdr.GetRtfText(Model.Flaw, Uuid);
        BackStory = await _rdr.GetRtfText(Model.BackStory, Uuid);

        CharacterTraits.Clear();
        foreach (string member in Model.TraitList)
            CharacterTraits.Add(member);

        RelationType = string.Empty;
        RelationshipTrait = string.Empty;
        RelationshipAttitude = string.Empty;
        RelationshipNotes = string.Empty;
        SelectedRelationship = null;
        CurrentRelationship = null;
        CharacterRelationships.Clear();
        foreach (RelationshipModel relation in Model.RelationshipList)
        {
            relation.Partner = StringToStoryElement(relation.PartnerUuid); // Populate partner Character StoryElement for Name
            CharacterRelationships.Add(relation);
        }

        _changeable = true;
    }

    internal async Task SaveModel()
    {
        if (_changed)
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
            Model.Adventureousness = Adventurousness;
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
                
            await SaveRelationship(CurrentRelationship);  // Save any current changes
            CurrentRelationship = null;
            // Move relationships back to the character model
            Model.RelationshipList.Clear();
            foreach (RelationshipModel relation in CharacterRelationships)
                Model.RelationshipList.Add(relation);
            Model.Flaw = Flaw;
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
            Model.Notes = await _wtr.PutRtfText(Notes, Uuid, "Notes.rtf");
            Model.Flaw = await _wtr.PutRtfText(Flaw, Uuid, "flaw.rtf");
            Model.BackStory = await _wtr.PutRtfText(BackStory, Uuid, "backstory.rtf");

            //_logger.Log(LogLevel.Info, string.Format("Requesting IsDirty change to true"));
            //Messenger.Send(new IsChangedMessage(Changed));
        }
    }

    private void AddTrait()
    {
        string trait = "(Other) " + NewTrait;
        CharacterTraits.Add(trait);
        NewTrait = string.Empty;
    }
        
    private void RemoveTrait() 
    {
        if (ExistingTraitIndex == -1) 
        {
            StatusMessage _smsg = new("No trait selected to delete", 200);
            Messenger.Send(new StatusChangedMessage(_smsg));
            return;
        }
        CharacterTraits.RemoveAt(ExistingTraitIndex);
    }

    private static StoryElement StringToStoryElement(string value)
    {
        if (value == null)
            return null;
        if (value.Equals(string.Empty))
            return null;
        // Get the current StoryModel's StoryElementsCollection
        StoryModel model = ShellViewModel.GetModel();
        StoryElementCollection elements = model.StoryElements;
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
        if (Guid.TryParse(value, out Guid guid))
        {
            if (elements.StoryElementGuids.ContainsKey(guid)) { return elements.StoryElementGuids[guid]; }
        }
        return null;  // Not found
    }

    /// <summary>
    /// Load and bind a RelationshipModel instance
    /// </summary>
    /// <param name="selectedRelation">The RelationShipModel just selected or added</param>
    public async Task LoadRelationship(RelationshipModel selectedRelation)
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

    public async Task SaveRelationship(RelationshipModel selectedRelation)
    {
        if (!_changed || selectedRelation == null)
            return;
        selectedRelation.Trait = RelationshipTrait;
        selectedRelation.Attitude = RelationshipAttitude;
        selectedRelation.Notes = await _wtr.PutRtfText(RelationshipNotes, Uuid, selectedRelation.PartnerUuid + "_notes.rtf");
        //_logger.Log(LogLevel.Info, string.Format("Requesting IsDirty change to true"));
        //Messenger.Send(new IsChangedMessage(Changed));
    }


    /// <summary>
    /// Add a new RelationshipModel instance for this character.
    /// The added relationship is made the currently loaded and displayed one.
    /// </summary>
    private async void AddRelationship()
    {
        _logger.Log(LogLevel.Info, "Executing AddRelationship command");
        await SaveRelationship(CurrentRelationship);

        //Sets up view model
        NewRelationshipViewModel VM = new(Model);
        VM.RelationTypes.Clear();
        foreach (RelationType relationType in GlobalData.RelationTypes) { VM.RelationTypes.Add(relationType); }
        VM.ProspectivePartners.Clear(); //Prospective partners are chars who are not in a relationship with this char
        StoryModel model = ShellViewModel.GetModel();
        foreach (StoryElement character in model.StoryElements.Characters)
        {
            if (character == VM.Member) continue;  // Skip me
            foreach (RelationshipModel rel in CharacterRelationships)
            {
                if (character == rel.Partner) goto NextCharacter; // Skip partner
            }
            VM.ProspectivePartners.Add(character);
            NextCharacter: continue;
        }

        //Creates dialog and shows dialog
        ContentDialog NewRelationship = new();
        NewRelationship.Title = "New relationship";
        NewRelationship.PrimaryButtonText = "Add relationship";
        NewRelationship.SecondaryButtonText = "Cancel";
        NewRelationship.XamlRoot = GlobalData.XamlRoot;
        NewRelationship.Content = new NewRelationshipPage(VM);
        ContentDialogResult result = await NewRelationship.ShowAsync();

        if (result == ContentDialogResult.Primary) //User clicks add relationship
        {
            try
            {
                // Create the new RelationshipModel
                string partnerUuid = StoryWriter.UuidString(VM.SelectedPartner.Uuid);
                RelationshipModel memberRelationship = new(partnerUuid, VM.RelationType);

                memberRelationship.Partner = StringToStoryElement(partnerUuid); // Complete pairing
                // Add partner relationship to member's list of relationships 
                CharacterRelationships.Add(memberRelationship);
                SelectedRelationship = memberRelationship;
                await LoadRelationship(SelectedRelationship);
                CurrentRelationship = SelectedRelationship;

                _changed = true;
                string msg = $"Relationship to {VM.SelectedPartner.Name} added";
                Messenger.Send(new StatusChangedMessage(new StatusMessage(msg, 200)));
                _logger.Log(LogLevel.Info, msg);
            }
            catch (Exception ex)
            {
                _logger.LogException(LogLevel.Error, ex, "Error creating new Relationship");
                Messenger.Send(new StatusChangedMessage(new StatusMessage("Error creating new Relationship", 200)));
            }
        }
        else //User clicks cancel
        {
            _logger.Log(LogLevel.Info, "AddRelationship cancelled");
            Messenger.Send(new StatusChangedMessage(new StatusMessage("AddRelationship cancelled", 200)));
        }
    }

    private async void RemoveRelationship()
    {
        _logger.Log(LogLevel.Info, "Executing RemoveRelationship command");
        string msg;
        // verify that I have an active relationship
        if (SelectedRelationship == null)
        {
            _logger.Log(LogLevel.Warn, "A relationship to be removed");
            msg = "Select the relationship to be removed";
            StatusMessage smsg = new(msg, 200);
            Messenger.Send(new StatusChangedMessage(smsg));
            _logger.Log(LogLevel.Warn, "A relationship must be active to be removed");
            return;
        }

        // Display a confirmation message
        StoryElement partner = SelectedRelationship.Partner;
        msg = $"Remove relationship to {partner.Name}? ";
        msg += Environment.NewLine;
        ContentDialog dialog = new()
        {
            Title = "Remove Relationship",
            Content = msg,
            PrimaryButtonText = "Yes",
            SecondaryButtonText = "No"
        };
        dialog.XamlRoot = GlobalData.XamlRoot;
        ContentDialogResult result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            // Remove the current (selected) relationship
            RelationshipModel rel = SelectedRelationship;
            ClearActiveRelationship();
            rel.Partner = null;
            CharacterRelationships.Remove(rel);
            msg = $"Relationship to {partner.Name} deleted";
            _changed = true;
            // log and display status
            _logger.Log(LogLevel.Info, msg);
            StatusMessage smsg = new(msg, 200);
            Messenger.Send(new StatusChangedMessage(smsg));
        }
        else
        {
            _logger.Log(LogLevel.Info, "Remove relationship cancelled");
            msg = "RemoveRelationship cancelled";
            _logger.Log(LogLevel.Info, msg);
            StatusMessage smsg = new(msg, 200);
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
        CurrentRelationship = null;

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

    /// <summary>
    /// This opens and deals with the flaw tool
    /// </summary>
    private async void FlawTool()
    {
        _logger.Log(LogLevel.Info, "Displaying Flaw Finder tool dialog");

        //Creates and shows dialog
        ContentDialog FlawDialog = new();
        FlawDialog.XamlRoot = GlobalData.XamlRoot;
        FlawDialog.Content = new Flaw();
        FlawDialog.Title = "Flaw Builder";
        FlawDialog.PrimaryButtonText = "Copy flaw example";
        FlawDialog.CloseButtonText = "Cancel";
        ContentDialogResult result = await FlawDialog.ShowAsync();

        if (result == ContentDialogResult.Primary)   // Copy to Character Flaw  
        {
            Flaw = Ioc.Default.GetService<FlawViewModel>().WoundSummary; //Sets the flaw.
            _logger.Log(LogLevel.Info, "Flaw Finder complete");
        }
        else  // Cancel button pressed
        {
            _logger.Log(LogLevel.Info, "Flaw Finder canceled");
        }
    }

    private async void TraitTool()
    {
        _logger.Log(LogLevel.Info, "Displaying Trait Builder tool dialog");

        //Creates and shows dialog
        ContentDialog TraitDialog = new(); 
        TraitDialog.Title = "Trait builder";
        TraitDialog.PrimaryButtonText = "Copy trait";
        TraitDialog.CloseButtonText = "Cancel";
        TraitDialog.XamlRoot = GlobalData.XamlRoot;
        TraitDialog.Content = new Traits();
        ContentDialogResult result = await TraitDialog.ShowAsync();

        if (result == ContentDialogResult.Primary)   // Copy to Character Trait 
        {
            CharacterTraits.Add(Ioc.Default.GetService<TraitsViewModel>().Example);
            _changed = true;
            ShellViewModel.ShowChange();
            _logger.Log(LogLevel.Info, "Trait Builder complete");
        }
        else  // Cancel button pressed
        {
            _logger.Log(LogLevel.Info, "Trait Builder cancelled");
        }
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
    public ObservableCollection<string> AdventurousnessList;
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

    #endregion

    #region Constructors

    public CharacterViewModel()
    {
        _logger = Ioc.Default.GetService<LogService>();
        _wtr = Ioc.Default.GetService<StoryWriter>();
        _rdr = Ioc.Default.GetService<StoryReader>();
        Ioc.Default.GetService<NewProjectViewModel>();

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
        AdventurousnessList = lists["Adventurous"];
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
        RelationshipTraitList = lists["Trait"];
        RelationshipAttitudeList = lists["Attitude"];

        CharacterTraits = new ObservableCollection<string>();
        CharacterRelationships = new ObservableCollection<RelationshipModel>();

        AddTraitCommand = new RelayCommand(AddTrait, () => true);
        RemoveTraitCommand = new RelayCommand(RemoveTrait, () => true);
        AddRelationshipCommand = new RelayCommand(AddRelationship, () => true);
        RemoveRelationshipCommand = new RelayCommand(RemoveRelationship, () => true);
        FlawCommand = new RelayCommand(FlawTool, () => true);
        TraitCommand = new RelayCommand(TraitTool, () => true);

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
        Adventurousness = string.Empty;
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
        Notes = string.Empty;
        Flaw = string.Empty;
        BackStory = string.Empty;

        PropertyChanged += OnPropertyChanged;
    }
}

#endregion