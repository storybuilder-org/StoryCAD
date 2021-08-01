using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace StoryBuilder.Models
{
    public class Relationship : ObservableObject
    {
        public string FirstPersonRelationship;
        public string SecondPersonRelationship;
        public bool FamilyRelation;

        public Relationship(string first, string second, string family)
        {
            FirstPersonRelationship = first;
            SecondPersonRelationship = second;
            FamilyRelation = family.Equals("Y");
        }

        public override string ToString()
        {
            return FirstPersonRelationship + " => " + SecondPersonRelationship;
        }
    }
}
