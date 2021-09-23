using CommunityToolkit.Mvvm.ComponentModel;
using System;
using Windows.Data.Xml.Dom;

namespace StoryBuilder.Models
{
    public class Relationship : ObservableObject
    {
        #region fields

        StoryModel _model;
        #endregion
        #region Properties

        public StoryElement Member { get; set; }   // This character in the relatinship
        public StoryElement Partner { get; set; }  // The other person in the relationship

        private Relationship _partnerRelationship;
        public Relationship PartnerRelationship  // The partner's side of things
        {   get 
            {
                if (_partnerRelationship != null)
                    return _partnerRelationship;
                else
                    return FindPartnerRelationship(Member, Partner);
            }
            set => _partnerRelationship = value;
        }

        #endregion

        #region Private methods 
        /// <summary>
        /// Find the partner relationship to the current relationship.
        /// This is done by flipping the member and partner sides of
        /// the relationship.
        /// </summary>
        /// <param name="member">Caller's StoryElement</param>
        /// <param name="partner">Partner's StoryEelement</param>
        /// <returns>Relationship</returns>
        private Relationship FindPartnerRelationship(StoryElement member, StoryElement partner)
            {
            foreach (Relationship relationship in _model.Relationships)
            {
                if (relationship.Member.Equals(partner)
                &&  relationship.Partner.Equals(member))
                   return relationship;
            }
            return null;
        }

        public string RelationType { get; set; }
        public string Trait { get; set; }
        public string Attitude { get; set; } 
        public string Notes { get; set; }
        #endregion

        #region Constructor

        public Relationship(StoryModel model) 
        {
            Member = null;
            Partner = null;
            PartnerRelationship = null;
            RelationType = null;
            Trait = string.Empty;
            Attitude = string.Empty;
            Notes = string.Empty;

            model.Relationships.Add(this);
            _model = model;
        }

        public Relationship(StoryElement member, StoryElement partner, RelationType type, StoryModel model)
        {
            Member = member;
            Partner = partner;
            RelationType = type.ToString();
            PartnerRelationship = null;
            Trait = string.Empty;
            Attitude = string.Empty;
            Notes = string.Empty;

            model.Relationships.Add(this);
            _model = model;
        }

        public Relationship(IXmlNode xn, StoryModel model)
        {
            Member = null;
            Partner = null;
            PartnerRelationship = null;
            RelationType = null;
            Trait = string.Empty;
            Attitude = string.Empty;
            Notes = string.Empty;
            _model = model;

            foreach (var attr in xn.Attributes)
            {
                switch (attr.NodeName)
                {
                    case "Member":
                        Member = StringToStoryElement(attr.InnerText);
                        break;
                   case "Partner":
                        Partner = StringToStoryElement(attr.InnerText);
                        break;
                    case "RelationType":
                       RelationType = attr.InnerText;
                        break;
                    case "Trait":
                        Trait = attr.InnerText;
                        break;
                    case "Attitude":
                        Attitude = attr.InnerText;
                        break;
                    case "Notes":
                        Notes = attr.InnerText;
                        break;
                }
            }

            model.Relationships.Add(this);
        }

        private StoryElement StringToStoryElement(string value)
        {
            if (value == null)
                return null;
            if (value.Equals(string.Empty))
                return null;
            // Get the current StoryModel's StoryElementsCollection
            StoryElementCollection elements = _model.StoryElements;
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
    }
}
