using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace StoryBuilder.Models.Tools
{
    public class CharacterRelationshipsModel : ObservableObject
    {
        #region Properties

        private static int _nextRelationship;

        private int _id;
        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }
        public string FirstChar { get; set; }
        public string FirstTrait { get; set; }
        public string SecondChar { get; set; }
        public string SecondTrait { get; set; }
        public string Relationship { get; set; }
        public string Remarks { get; set; }
        #endregion

        #region Constructor

        public CharacterRelationshipsModel()
        {
            Id = ++_nextRelationship;
            FirstChar = string.Empty;
            FirstTrait = string.Empty;
            SecondChar = string.Empty;
            SecondTrait = string.Empty;
            Relationship = string.Empty;
            Remarks = string.Empty;
        }

        #endregion
    }
}
