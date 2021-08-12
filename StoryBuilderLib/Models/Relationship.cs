using Microsoft.Toolkit.Mvvm.ComponentModel;
using System;

namespace StoryBuilder.Models 
{
    public class Relationship : ObservableObject
    {
        #region Properties

        public StoryElement Member { get; set; }   // This character in the relatinship
        public StoryElement Partner { get; set; }  // The other person in the relationship
        public Relationship PartnerRelationship { get; set; } // The partner's side of things
        public RelationType RelationType { get; set; }
        public string Trait { get; set; }
        public string Dynamic { get; set; } 
        public string Remarks { get; set; }
        #endregion

        #region Constructor

        public Relationship() 
        {
            Member = null;
            Partner = null;
            RelationType = null;
            Trait = string.Empty;
            Dynamic = string.Empty;
            Remarks = string.Empty;
        }

        public Relationship(StoryElement member, StoryElement partner, RelationType type)
        {
            Member = member;
            Partner = partner;
            RelationType = type;
            PartnerRelationship = null;
            Trait = string.Empty;
            Dynamic = string.Empty;
            Remarks = string.Empty;
        }

        #endregion
    }
}
