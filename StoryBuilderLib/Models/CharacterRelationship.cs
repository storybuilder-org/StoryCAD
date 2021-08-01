using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace StoryBuilder.Models 
{
    public class CharacterRelationship : ObservableObject
    {
        #region Properties
        public StoryElement Character { get; set; }
        public Relationship Relationship { get; set; }
        public string PrimarytTrait { get; set; }
        public string OpposingTrait { get; set; }
        public string Remarks { get; set; }
        #endregion

        #region Constructor

        //TODO: Fix this
        public CharacterRelationship()
        {
            
            //Character = string.Empty;
            //PrimaryTrait = string.Empty;
            ////SecondCharacter = string.Empty;
            //OpposingTrait = string.Empty;
            //Relationship = null;
            //Remarks = string.Empty;
        }

        #endregion
    }
}
