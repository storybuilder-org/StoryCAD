using Microsoft.Toolkit.Mvvm.ComponentModel;
using StoryBuilder.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StoryBuilder.ViewModels.Tools
{
    //TODO: Implmement INavigable (for close button?) Note different frame.
    public class CharacterRelationshipsViewModel : ObservableRecipient // , INavigable
    {
        #region Fields
        private bool _changed;

        private int _id;
        private string _firstChar;
        private string _firstTrait;
        private string _secondChar;
        private string _secondTrait;
        private string _relationship;
        private string _remarks;

        private ObservableCollection<CharacterModel> _characters;

        #endregion

        #region Properties
        public bool Changed
        {
            get { return _changed; }
            set { _changed = false; }
        }

        public int Id   // Note that this property doesn't use Change Notification
        {
            get { return _id; }
            set { _id = value; }
        }

        public string FirstChar
        {
            get => _firstChar;
            set => SetProperty(ref _firstChar, value);
        }

        public string FirstTrait
        {
            get => _firstTrait;
            set => SetProperty(ref _firstTrait, value);
        }

        public string SecondChar
        {
            get => _secondChar;
            set => SetProperty(ref _secondChar, value);
        }

        public string SecondTrait
        {
            get => _secondTrait;
            set => SetProperty(ref _secondTrait, value);
        }

        public string Relationship
        {
            get => _relationship;
            set => SetProperty(ref _relationship, value);
        }

        public string Remarks
        {
            get => _remarks;
            set => SetProperty(ref _remarks, value);
        }

        #endregion

        #region Combobox and ListBox sources

        public ObservableCollection<string> RelationshipSource;

        #endregion

        #region Constructor

        public CharacterRelationshipsViewModel(Dictionary<string, ObservableCollection<string>> lists, ObservableCollection<CharacterModel> characters)
        {
            _characters = characters;
            RelationshipSource = lists["Relation"];
        }

        #endregion

    }
}
