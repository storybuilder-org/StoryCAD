using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCAD.ViewModels;
using System;
using Windows.Data.Xml.Dom;

namespace StoryCAD.Models;

public class WebModel : StoryElement
{
    public Uri URL;
    public DateTime Timestamp;

    public WebModel(StoryModel model) : base("New Webpage", StoryItemType.Web, model)
    {
        Timestamp = DateTime.Now;

        switch (Ioc.Default.GetRequiredService<AppState>().Preferences.PreferredSearchEngine)
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

    public WebModel(IXmlNode xn, StoryModel model) : base(xn, model)
    {
        URL = new Uri("https://google.com/");
        Timestamp = DateTime.MaxValue;
    }
}