using Windows.Data.Xml.Dom;
using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCAD.ViewModels;

namespace StoryCAD.Models;

public class RelationshipModel
{
    #region Properties

    public StoryElement Partner { get; set; } // The other character in the relationship
    public string PartnerUuid { get; set; }  // The other character's UUID
    public string RelationType { get; set; }
    public string Trait { get; set; }
    public string Attitude { get; set; }
    public string Notes { get; set; }
    public CharacterViewModel CharVM = Ioc.Default.GetService<CharacterViewModel>();
    #endregion

    #region Constructors

    public RelationshipModel(string partnerUuid, string type)
    {

        PartnerUuid = partnerUuid;
        RelationType = type;
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

        foreach (IXmlNode _attr in xn.Attributes)
        {
            switch (_attr.NodeName)
            {
                case "Partner":
                    PartnerUuid =  _attr.InnerText;
                    break;
                case "RelationType":
                    RelationType = _attr.InnerText;
                    break;
                case "Trait":
                    Trait = _attr.InnerText;
                    break;
                case "Attitude":
                    Attitude = _attr.InnerText;
                    break;
                case "Notes":
                    Notes = _attr.InnerText;
                    break;
            }
        }
    }

    #endregion
}