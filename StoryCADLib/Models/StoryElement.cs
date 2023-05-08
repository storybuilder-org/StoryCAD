using System;
using System.Diagnostics.CodeAnalysis;
using Windows.Data.Xml.Dom;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCAD.ViewModels;

namespace StoryCAD.Models;

public class StoryElement : ObservableObject
{
    #region  Properties

    private readonly Guid _uuid;
    public Guid Uuid => _uuid;

    private string _name;
    public string Name
    {
        get => _name;
        set => _name = value;
    }

    private StoryItemType _type;
    public StoryItemType Type
    {
        get => _type;
        set => _type = value;
    }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => _isSelected = value;
    }

    #endregion

    #region Public Methods
    public static StoryElement StringToStoryElement(string value)
    {
        if (value == null)
            return null;
        if (value.Equals(string.Empty))
            return null;
        // Get the current StoryModel's StoryElementsCollection
        ShellViewModel shell = Ioc.Default.GetService<ShellViewModel>();
        StoryElementCollection elements = shell.StoryModel.StoryElements;
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
        if (Guid.TryParse(value, out Guid guid))
            if (elements.StoryElementGuids.ContainsKey(guid))
                return elements.StoryElementGuids[guid];
        return null;  // Not found
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