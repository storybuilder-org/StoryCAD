using Windows.Data.Xml.Dom;
using StoryCAD.Services;
using System.Text.Json.Serialization;

namespace StoryCAD.Models;

public class WebModel : StoryElement
{
	[JsonInclude]
	[JsonPropertyName("URI")]
	public Uri URL;

	[JsonInclude]
	[JsonPropertyName("Timestamp")]
	public DateTime Timestamp;

    public WebModel(StoryModel model, StoryNodeItem node) : base("New Webpage", StoryItemType.Web, model, node)
    {
        Timestamp = DateTime.Now;

        switch (Ioc.Default.GetRequiredService<PreferenceService>().Model.PreferredSearchEngine)
        {
            case BrowserType.DuckDuckGo:
                 URL = new Uri("https://duckduckgo.com/");
                break;
            case BrowserType.Google:
                URL = new Uri("https://google.com/");
                break;
            case BrowserType.Bing:
                URL = new Uri("https://bing.com/");
                break;
            case BrowserType.Yahoo:
               URL = new Uri("https://yahoo.com/");
                break;
            default: //Just default to DDG.
               URL = new Uri("https://duckduckgo.com/");
                break;
        }
    }

	public WebModel(){}
	
    //TODO: REMOVE WITH STORYREADER.
    public WebModel(IXmlNode xn, StoryModel model) : base(xn, model)
    {
        URL = new Uri("https://google.com/");
        Timestamp = DateTime.MaxValue;
    }
}