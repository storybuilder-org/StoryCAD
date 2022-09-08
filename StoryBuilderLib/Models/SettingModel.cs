using System.Collections.ObjectModel;
using Windows.Data.Xml.Dom;

namespace StoryBuilder.Models;

public class SettingModel : StoryElement
{
    #region Static Properties

    public static ObservableCollection<string> SettingNames = new();

    #endregion
    #region Properties

    // Besides its GUID, each Setting has a unique (to this story)
    // integer id number (useful in lists of settings.)
    private static int _nextSettingId;
    private int _id;
    public int Id
    {
        get => _id;
        set => _id = value;
    }

    // Setting (General) data

    private string _locale;
    public string Locale
    {
        get => _locale;
        set => _locale = value;
    }

    private string _season;
    public string Season
    {
        get => _season;
        set => _season = value;
    }

    private string _period;
    public string Period
    {
        get => _period;
        set => _period = value;
    }

    private string _lighting;
    public string Lighting
    {
        get => _lighting;
        set => _lighting = value;
    }

    private string _weather;
    public string Weather
    {
        get => _weather;
        set => _weather = value;
    }

    private string _temperature;
    public string Temperature
    {
        get => _temperature;
        set => _temperature = value;
    }

    private string _props;
    public string Props
    {
        get => _props;
        set => _props = value;
    }

    private string _summary;
    public string Summary
    {
        get => _summary;
        set => _summary = value;
    }

    // Setting Sense data

    private string _sights;
    public string Sights
    {
        get => _sights;
        set => _sights = value;
    }

    private string _sounds;
    public string Sounds
    {
        get => _sounds;
        set => _sounds = value;
    }

    private string _touch;
    public string Touch
    {
        get => _touch;
        set => _touch = value;
    }

    private string _smellTaste;
    public string SmellTaste
    {
        get => _smellTaste;
        set => _smellTaste = value;
    }

    // Setting Note data

    private string _notes;
    public string Notes
    {
        get => _notes;
        set => _notes = value;
    }

    #endregion

    #region Constructors
    public SettingModel(StoryModel model) : base("New Setting", StoryItemType.Setting, model)
    {
        Id = ++_nextSettingId;
        Locale = string.Empty;
        Season = string.Empty;
        Period = string.Empty;
        Lighting = string.Empty;
        Weather = string.Empty;
        Temperature = string.Empty;
        Props = string.Empty;
        Summary = string.Empty;
        Sights = string.Empty;
        Sounds = string.Empty;
        Touch = string.Empty;
        SmellTaste = string.Empty;
        Notes = string.Empty;
        SettingNames.Add(Name);
    }

    public SettingModel(IXmlNode xn, StoryModel model) : base(xn, model)
    {
        Id = ++_nextSettingId;
        Locale = string.Empty;
        Season = string.Empty;
        Period = string.Empty;
        Lighting = string.Empty;
        Weather = string.Empty;
        Temperature = string.Empty;
        Props = string.Empty;
        Summary = string.Empty;
        Sights = string.Empty;
        Sounds = string.Empty;
        Touch = string.Empty;
        SmellTaste = string.Empty;
        Notes = string.Empty;
        SettingNames.Add(Name);
    }

    #endregion
}