using System.Collections.ObjectModel;
using Windows.Data.Xml.Dom;
using StoryBuilder.Models;

namespace StoryBuilder.Models
{
    public class CharacterModel : StoryElement
    {

        #region Properties

        // Besides its GUID, each Character has a unique (to this story)
        // integer id number (useful in lists of characters.)
        private static int _nextCharacterId;
        private int _id;
        public int Id
        {
            get => _id;
            set => _id = value;
        }

        // Character role data

        private string _role;
        public string Role
        {
            get => _role;
            set => _role = value;
        }

        private string _storyRole;
        public string StoryRole
        {
            get => _storyRole;
            set => _storyRole = value;
        }

        private string _archetype;
        public string Archetype
        {
            get => _archetype;
            set => _archetype = value;
        }

        private string _characterSketch;
        public string CharacterSketch
        {
            get => _characterSketch;
            set => _characterSketch = value;
        }

        // Character physical data

        private string _age;
        public string Age
        {
            get => _age;
            set => _age = value;
        }

        private string _sex;
        public string Sex
        {
            get => _sex;
            set => _sex = value;
        }

        private string _eyes;
        public string Eyes
        {
            get => _eyes;
            set => _eyes = value;
        }

        private string _hair;
        public string Hair
        {
            get => _hair;
            set => _hair = value;
        }

        private string _weight;
        public string Weight
        {
            get => _weight;
            set => _weight = value;
        }

        private string _charHeight;
        public string CharHeight
        {
            get => _charHeight;
            set => _charHeight = value;
        }

        private string _build;
        public string Build
        {
            get => _build;
            set => _build = value;
        }

        private string _complexion;
        public string Complexion
        {
            get => _complexion;
            set => _complexion = value;
        }

        private string _race;
        public string Race
        {
            get => _race;
            set => _race = value;
        }

        private string _nationality;
        public string Nationality
        {
            get => _nationality;
            set => _nationality = value;
        }

        private string _health;
        public string Health
        {
            get => _health;
            set => _health = value;
        }

        private string _physNotes;
        public string PhysNotes
        {
            get => _physNotes;
            set => _physNotes = value;
        }

        // Character appearance data

        private string _appearance;
        public string Appearance
        {
            get => _appearance;
            set => _appearance = value;
        }

        // Character social data

        private string _economic;
        public string Economic
        {
            get => _economic;
            set => _economic = value;
        }

        private string _education;
        public string Education
        {
            get => _education;
            set => _education = value;
        }

        private string _ethnic;
        public string Ethnic
        {
            get => _ethnic;
            set => _ethnic = value;
        }

        private string _religion;
        public string Religion
        {
            get => _religion;
            set => _religion = value;
        }

        // Character psych data

        private string _enneagram;
        public string Enneagram
        {
            get => _enneagram;
            set => _enneagram = value;
        }

        private string _intelligence;
        public string Intelligence
        {
            get => _intelligence;
            set => _intelligence = value;
        }

        private string _values;
        public string Values
        {
            get => _values;
            set => _values = value;
        }

        private string _abnormality;
        public string Abnormality
        {
            get => _abnormality;
            set => _abnormality = value;
        }

        private string _focus;
        public string Focus
        {
            get => _focus;
            set => _focus = value;
        }

        private string _psychNotes;
        public string PsychNotes
        {
            get => _psychNotes;
            set => _psychNotes = value;
        }

        // Character trait data

        private string _adventurousness;
        public string Adventureousness
        {
            get => _adventurousness;
            set => _adventurousness = value;
        }

        private string _agression;
        public string Aggression
        {
            get => _agression;
            set => _agression = value;
        }

        private string _confidence;
        public string Confidence
        {
            get => _confidence;
            set => _confidence = value;
        }

        private string _conscientiousness;
        public string Conscientiousness
        {
            get => _conscientiousness;
            set => _conscientiousness = value;
        }

        private string _creativity;
        public string Creativity
        {
            get => _creativity;
            set => _creativity = value;
        }

        private string _dominance;
        public string Dominance
        {
            get => _dominance;
            set => _dominance = value;
        }

        private string _enthusiasm;
        public string Enthusiasm
        {
            get => _enthusiasm;
            set => _enthusiasm = value;
        }

        private string _assurance;
        public string Assurance
        {
            get => _assurance;
            set => _assurance = value;
        }

        private string _sensitivity;
        public string Sensitivity
        {
            get => _sensitivity;
            set => _sensitivity = value;
        }

        private string _shrewdness;
        public string Shrewdness
        {
            get => _shrewdness;
            set => _shrewdness = value;
        }

        private string _sociability;
        public string Sociability
        {
            get => _sociability;
            set => _sociability = value;
        }

        private string _stability;
        public string Stability
        {
            get => _stability;
            set => _stability = value;
        }


    // Character work data

        private string _work;
        public string Work
        {
            get => _work;
            set => _work = value;
        }

        // Character likes data

        private string _likes;
        public string Likes
        {
            get => _likes;
            set => _likes = value;
        }

        // Character habits data

        private string _habits;
        public string Habits
        {
            get => _habits;
            set => _habits = value;
        }

        // Character abilities data

        private string _abilities;
        public string Abilities
        {
            get => _abilities;
            set => _abilities = value;
        }

        // Character flaw data

        private string _woundCategory;
        public string WoundCategory
        {
            get => _woundCategory;
            set => _woundCategory = value;
        }

        private string _woundSummary;

        public string WoundSummary
        {
            get => _woundSummary;
            set => _woundSummary = value;
        }

        private string _wound;
        public string Wound
        {
            get => _wound;
            set => _wound = value;
        }

        private string _fears;
        public string Fears
        {
            get => _fears;
            set => _fears = value;
        }

        private string _lies;
        public string Lies
        {
            get => _lies;
            set => _lies = value;
        }

        private string _secrets;
        public string Secrets
        {
            get => _secrets;
            set => _secrets = value;
        }

        // Character note data

        private string _backStory;
        public string BackStory
        {
            get => _backStory;
            set => _backStory = value;
        }

        #endregion

        #region Static Properties

        public static ObservableCollection<string> CharacterNames = new ObservableCollection<string>();

        #endregion

        #region Constructors
        public CharacterModel(StoryModel model) : base("New Character", StoryItemType.Character, model)
        {
            Id = ++_nextCharacterId;
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
            Fears = string.Empty;
            Lies = string.Empty;
            Secrets = string.Empty;
            BackStory = string.Empty;
            CharacterNames.Add(this.Name);
        }

        public CharacterModel(string name, StoryModel model) : base(name, StoryItemType.Character, model)
        {
            Id = ++_nextCharacterId;
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
            Fears = string.Empty;
            Lies = string.Empty;
            Secrets = string.Empty;
            BackStory = string.Empty;
            CharacterNames.Add(this.Name);
        }

        public CharacterModel(IXmlNode xn, StoryModel model) : base(xn, model)
        {
            Id = ++_nextCharacterId;
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
            Fears = string.Empty;
            Lies = string.Empty;
            Secrets = string.Empty;
            BackStory = string.Empty;
            CharacterNames.Add(this.Name);
        }

        #endregion

    }
}
