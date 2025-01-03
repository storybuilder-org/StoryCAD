using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using Windows.Data.Xml.Dom;

namespace StoryCAD.Models;

public class SettingModel : StoryElement
{
	#region Static Properties
	[JsonIgnore]
    public static ObservableCollection<string> SettingNames = new();

	#endregion

	#region Properties

	// Setting (General) data

	[JsonIgnore]
	private string _locale;

	[JsonInclude]
	[JsonPropertyName("Locale")]
	public string Locale
	{
		get => _locale;
		set => _locale = value;
	}

	[JsonIgnore]
	private string _season;

	[JsonInclude]
	[JsonPropertyName("Season")]
	public string Season
	{
		get => _season;
		set => _season = value;
	}

	[JsonIgnore]
	private string _period;

	[JsonInclude]
	[JsonPropertyName("Period")]
	public string Period
	{
		get => _period;
		set => _period = value;
	}

	[JsonIgnore]
	private string _lighting;

	[JsonInclude]
	[JsonPropertyName("Lighting")]
	public string Lighting
	{
		get => _lighting;
		set => _lighting = value;
	}

	[JsonIgnore]
	private string _weather;

	[JsonInclude]
	[JsonPropertyName("Weather")]
	public string Weather
	{
		get => _weather;
		set => _weather = value;
	}

	[JsonIgnore]
	private string _temperature;

	[JsonInclude]
	[JsonPropertyName("Temperature")]
	public string Temperature
	{
		get => _temperature;
		set => _temperature = value;
	}

	[JsonIgnore]
	private string _props;

	[JsonInclude]
	[JsonPropertyName("Props")]
	public string Props
	{
		get => _props;
		set => _props = value;
	}

	[JsonIgnore]
	private string _summary;

	[JsonInclude]
	[JsonPropertyName("Summary")]
	public string Summary
	{
		get => _summary;
		set => _summary = value;
	}

	[JsonIgnore]
	private string _sights;

	[JsonInclude]
	[JsonPropertyName("Sights")]
	public string Sights
	{
		get => _sights;
		set => _sights = value;
	}

	[JsonIgnore]
	private string _sounds;

	[JsonInclude]
	[JsonPropertyName("Sounds")]
	public string Sounds
	{
		get => _sounds;
		set => _sounds = value;
	}

	[JsonIgnore]
	private string _touch;

	[JsonInclude]
	[JsonPropertyName("Touch")]
	public string Touch
	{
		get => _touch;
		set => _touch = value;
	}

	[JsonIgnore]
	private string _smellTaste;

	[JsonInclude]
	[JsonPropertyName("SmellTaste")]
	public string SmellTaste
	{
		get => _smellTaste;
		set => _smellTaste = value;
	}

	[JsonIgnore]
	private string _notes;

	[JsonInclude]
	[JsonPropertyName("Notes")]
	public string Notes
	{
		get => _notes;
		set => _notes = value;
	}

	#endregion

	#region Constructors
	public SettingModel(StoryModel model) : base("New Setting", StoryItemType.Setting, model)
    {
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
    public SettingModel(string name, StoryModel model) : base(name, StoryItemType.Setting, model)
    {
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

	//TODO: REMOVE WITH STORYREADER
	public SettingModel(IXmlNode xn, StoryModel model) : base(xn, model)
    {
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

	public SettingModel(){}
    #endregion
}