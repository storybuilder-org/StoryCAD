using System.Text.Json.Serialization;
using Windows.Data.Xml.Dom;

namespace StoryCAD.Models;

public class RelationshipModel
{
	#region Properties
	// The other character in the relationship
	[JsonIgnore]
	public StoryElement Partner { get; set; }

	// The other character's UUID
	[JsonInclude]
	[JsonPropertyName("PartnerUuid")]
	public Guid PartnerUuid { get; set; }

	// Relationship type
	[JsonInclude]
	[JsonPropertyName("RelationType")]
	public string RelationType { get; set; }

	[JsonInclude] 
	[JsonPropertyName("Trait")]
	public string Trait { get; set; }

	[JsonInclude]
	[JsonPropertyName("Attitude")]
	public string Attitude { get; set; }

	[JsonInclude]
	[JsonPropertyName("Notes")]
	public string Notes { get; set; }

	[JsonIgnore]
	public readonly CharacterViewModel CharVM = Ioc.Default.GetService<CharacterViewModel>();

	#endregion

	#region Constructors

	public RelationshipModel(Guid partnerUuid, string type)
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
        PartnerUuid = Guid.Empty;
        RelationType = null;
        Trait = string.Empty;
        Attitude = string.Empty;
        Notes = string.Empty;

        foreach (IXmlNode _attr in xn.Attributes)
        {
            switch (_attr.NodeName)
            {
                case "Partner":
                    PartnerUuid =  Guid.Parse(_attr.InnerText);
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

    public RelationshipModel() { }

    #endregion
}