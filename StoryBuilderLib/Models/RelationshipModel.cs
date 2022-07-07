using Windows.Data.Xml.Dom;
using CommunityToolkit.Mvvm.DependencyInjection;
using StoryBuilder.ViewModels;

namespace StoryBuilder.Models;

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

    public CharacterViewModel CharVM = Ioc.Default.GetService<CharacterViewModel>();

    #endregion

    #region Constructors

    public RelationshipModel() 
    {
        Partner = null;
        PartnerUuid = string.Empty;
        RelationType = string.Empty;
        Trait = string.Empty;
        Attitude = string.Empty;
        Notes = string.Empty;
    }

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

        foreach (IXmlNode attr in xn.Attributes)
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