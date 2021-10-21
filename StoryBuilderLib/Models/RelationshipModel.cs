using CommunityToolkit.Mvvm.ComponentModel;
using System;
using Windows.Data.Xml.Dom;

namespace StoryBuilder.Models
{
    public class RelationshipModel
    {
        #region fields

        StoryModel _model;
        #endregion

        #region Properties

        public StoryElement Partner { get; set; } // The other character in the relationship
        public string PartnerUuid { get; set; }  // The other character's UUID
        public string RelationType { get; set; }
        public string Trait { get; set; }
        public string Attitude { get; set; }
        public string Notes { get; set; }

        #endregion

        #region Constructor

        public RelationshipModel() 
        {
            Partner = null;
            PartnerUuid = string.Empty;
            RelationType = string.Empty;
            Trait = string.Empty;
            Attitude = string.Empty;
            Notes = string.Empty;
        }

        public RelationshipModel(string partnerUuid, RelationType type)
        {

            PartnerUuid = partnerUuid;
            RelationType = type.ToString();
            Trait = string.Empty;
            Attitude = string.Empty;
            Notes = string.Empty;
        }

        public RelationshipModel(IXmlNode xn)
        {
            Partner = null;
            PartnerUuid = string.Empty;
            RelationType = null;
            Trait = string.Empty;
            Attitude = string.Empty;
            Notes = string.Empty;

            foreach (var attr in xn.Attributes)
            {
                switch (attr.NodeName)
                {
                   case "Partner":
                        PartnerUuid =  attr.InnerText;
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
        }

        #endregion
    }
}
