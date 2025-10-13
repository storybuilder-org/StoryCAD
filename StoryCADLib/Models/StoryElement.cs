using System.Text.Json;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using StoryCADLib.DAL;

namespace StoryCADLib.Models;

public class StoryElement : ObservableObject
{
    #region Properties

    [JsonIgnore] private Guid _uuid;

    [JsonInclude]
    [JsonPropertyName("GUID")]
    public Guid Uuid
    {
        get => _uuid;
        set => _uuid = value;
    }

    [JsonIgnore] private string _description;

    /// <summary>
    ///     Common description field that is mapped to the main textbox on an element.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("ElementDescription")]
    public string Description
    {
        get => _description;
        set => _description = value;
    }

    [JsonIgnore] private string _name;

    [JsonInclude]
    [JsonPropertyName("Name")]
    public string Name
    {
        get => _name;
        set
        {
            _name = value;
            // Keep the node synchronized when name changes from API
            if (_node != null)
            {
                _node.Name = value;
            }
        }
    }

    [JsonIgnore] private StoryItemType _type;

    [JsonInclude]
    [JsonPropertyName("Type")]
    public StoryItemType ElementType
    {
        get => _type;
        set => _type = value;
    }

    [JsonIgnore] private StoryNodeItem _node;

    [JsonIgnore]
    public StoryNodeItem Node
    {
        get => _node;
        set => _node = value;
    }

    [JsonIgnore] private bool _isSelected;

    [JsonIgnore]
    public bool IsSelected
    {
        get => _isSelected;
        set => _isSelected = value;
    }

    #endregion

    #region Public Methods

    /// <summary>
    ///     Updates this elements GUID field.
    ///     (Call this immediately after creating an element)
    /// </summary>
    internal void UpdateGuid(StoryModel model, Guid newGuid)
    {
        model.StoryElements.StoryElementGuids.Remove(Uuid);
        Uuid = newGuid;
        model.StoryElements.StoryElementGuids.Add(newGuid, this);
    }

    /// <summary>
    ///     Retrieve a StoryElement from its Guid.
    ///     Guids are used as keys to StoryElements, stored in
    ///     the StoryModel's StoryElementCollection. They also
    ///     identify links from one StoryElement to another,
    ///     such as the Setting or a cast member Character in
    ///     a Scene. We use Guid.Empty as the value for such a
    ///     link  until it's assigned.
    ///     These placeholder links are often expected to be a
    ///     StoryElement key, such as to display the name of
    ///     the setting on the Scene Content pane. Treating
    ///     Guid.Empty as an 'Undefined' StoryElement, with
    ///     a blank name, simplifies that code.
    /// </summary>
    /// <param name="guid">The Guid of the StoryElement to retrieve</param>
    /// <param name="storyModel">optional story model override, defaults to current app state model.</param>
    /// <returns></returns>
    public static StoryElement GetByGuid(Guid guid, StoryModel storyModel = null)
    {
        if (guid.Equals(Guid.Empty))
        {
            return new StoryElement();
        }

        // Get the StoryElementsCollection from the provided storyModel or from AppState
        StoryElementCollection elements;
        if (storyModel != null)
        {
            elements = storyModel.StoryElements;
        }
        else
        {
            // Fallback to AppState when storyModel is not provided
            // This global state dependency is acceptable for UI scenarios
            // API consumers should always provide storyModel explicitly
            var appState = Ioc.Default.GetRequiredService<AppState>();
            elements = appState.CurrentDocument?.Model.StoryElements ?? new();
        }

        // Look for the StoryElement corresponding to the passed guid
        if (elements.StoryElementGuids.ContainsKey(guid))
        {
            return elements.StoryElementGuids[guid];
        }

        Ioc.Default.GetRequiredService<ILogService>()
            .Log(LogLevel.Error, $"Cannot find GUID {guid} in outline");
        return new StoryElement(); // Not found
    }

    /// <summary>
    ///     Deserializes a JSON string into a StoryElement.
    /// </summary>
    /// <param name="json">JSON to deserialize.</param>
    /// <returns>StoryElement Object.</returns>
    public static StoryElement Deserialize(string json)
    {
        return JsonSerializer.Deserialize<StoryElement>(json, new JsonSerializerOptions
        {
            Converters =
            {
                new EmptyGuidConverter(),
                new StoryElementConverter(),
                new JsonStringEnumConverter()
            }
        });
    }

    /// <summary>
    ///     Serialises this StoryElement into JSON.
    /// </summary>
    /// <returns>JSON Representation of this object.</returns>
    public string Serialize()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            Converters =
            {
                new EmptyGuidConverter(),
                new StoryElementConverter(),
                new JsonStringEnumConverter()
            }
        });
    }

    public override string ToString() => _uuid.ToString();

    #endregion

    #region Constructor

    /// <summary>
    ///     Creates a new story element
    /// </summary>
    /// <param name="name">Name of element</param>
    /// <param name="type">Type of element</param>
    /// <param name="model">Story Model this element belongs to</param>
    /// <param name="parentNode">Parent of this node</param>
    public StoryElement(string name, StoryItemType type, StoryModel model, StoryNodeItem parentNode)
    {
        _uuid = Guid.NewGuid();
        _name = name;
        _type = type;
        _description = string.Empty;
        _node = new StoryNodeItem(this, parentNode, type);

        model.StoryElements.Add(this);
    }

    /// <summary>
    ///     Parameterless constructor for JSON Deserialization.
    ///     Don't remove.
    /// </summary>
    public StoryElement()
    {
        _uuid = Guid.Empty;
        _name = string.Empty;
        _type = StoryItemType.Unknown;
        _description = string.Empty;
        _node = null;
    }

    #endregion
}
