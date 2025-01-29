using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Windows.Data.Xml.Dom;
using CommunityToolkit.Mvvm.ComponentModel;

namespace StoryCAD.Models;

public class StoryElement : ObservableObject
{
	#region  Properties
	[JsonIgnore]
	private Guid _uuid;

	[JsonInclude]
	[JsonPropertyName("GUID")]
	public Guid Uuid
	{
		get => _uuid;
		private set => _uuid = value;
	}

	[JsonIgnore]
    private string _name;

    [JsonInclude]
    [JsonPropertyName("Name")]
	public string Name
    {
        get => _name;
        set => _name = value;
    }

	[JsonIgnore]
    private StoryItemType _type;
	[JsonInclude]
	[JsonPropertyName("Type")]
    public StoryItemType Type
    {
        get => _type;
        set => _type = value;
    }

	[JsonIgnore]
    private bool _isSelected;
	[JsonIgnore]
    public bool IsSelected
    {
        get => _isSelected;
        set => _isSelected = value;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Retrieve a StoryElement from its Guid.
    ///
    /// Guids are used as keys to StoryElements, stored in
    /// the StoryModel's StoryElementCollection. They also
    /// identify links from one StoryElement to another,
    /// such as the Setting or a cast member Character in
    /// a Scene. We use Guid.Empty as the value for such a
    /// link  until it's assigned.
    ///
    /// These placeholder links are often expected to be a
    /// StoryElement key, such as to display the name of
    /// the setting on the Scene Content pane. Treating
    /// Guid.Empty as an 'Undefined' StoryElement, with
    /// a blank name, simplifies that code. 
    /// </summary>
    /// <param name="guid">The Guid of the</param>
    /// <returns></returns>
    public static StoryElement GetByGuid(Guid guid)
    {
        if (guid.Equals(Guid.Empty))
             return new StoryElement();
        // Get the current StoryModel's StoryElementsCollection
        ShellViewModel shell = Ioc.Default.GetService<ShellViewModel>();
        StoryElementCollection elements = shell!.StoryModel.StoryElements;
        // Look for the StoryElement corresponding to the passed guid
            if (elements.StoryElementGuids.ContainsKey(guid))
                return elements.StoryElementGuids[guid];
        //TODO: Log the error.
        return new StoryElement();  // Not found
    }

    public override string ToString()
    {
        return _uuid.ToString();
    }

	#endregion

	#region Constructor 
    public StoryElement(string name, StoryItemType type, StoryModel model)
    {
        _uuid = Guid.NewGuid();
        _name = name;
        _type = type;
        model.StoryElements.Add(this);
    }

	/// <summary>
	/// Parameterless constructor for JSON Deserialization.
	/// Don't remove.
	/// </summary>
    public StoryElement()
    {
        _uuid = Guid.Empty;
        _name = string.Empty;
        _type = StoryItemType.Unknown;
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")] //Some names conflict with class members, and I can't really think of any suitable alternatives.
    public StoryElement(IXmlNode xn, StoryModel model)
    {
        Guid uuid = default;
        StoryItemType type = StoryItemType.Unknown;
        string name = string.Empty;
        bool _uuidFound = false;
        bool _nameFound = false;
        Type = StoryItemType.Unknown;
        switch (xn.NodeName)
        {
            case "Overview":
                type = StoryItemType.StoryOverview;
                break;
            case "Problem":
                type = StoryItemType.Problem;
                break;
            case "Character":
                type = StoryItemType.Character;
                break;
            case "Setting":
                type = StoryItemType.Setting;
                break;
            case "PlotPoint":       // Legacy: PlotPoint was renamed to Scene   
                type = StoryItemType.Scene;
                break;
            case "Scene":
                type = StoryItemType.Scene;
                break;
            case "Separator":       // Legacy: Separator was renamed to Folder
                type = StoryItemType.Folder;
                break;
            case "Folder":
                type = StoryItemType.Folder;
                break;
            case "Section":
                type = StoryItemType.Section;
                break;
            case "Notes":
                type = StoryItemType.Notes;
                break;
            case "Web":
                type= StoryItemType.Web;
                break;
            case "TrashCan":
                type = StoryItemType.TrashCan;
                break;
        }
        foreach (IXmlNode _attr in xn.Attributes)
        {
            switch (_attr.NodeName)
            {
                case "UUID":
                    uuid = new Guid(_attr.InnerText);
                    _uuidFound = true;
                    break;
                case "Name":
                    name = _attr.InnerText;
                    _nameFound = true;
                    break;
            }
            if (_uuidFound && _nameFound)
                break;
        }
        _uuid = uuid;
        _name = name;
        _type = type;
        model.StoryElements.Add(this);
    }

    #endregion
}