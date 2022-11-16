using System;
using Windows.Data.Xml.Dom;

namespace StoryBuilder.Models;

public class WebModel : StoryElement
{
    public Uri URL;
    public DateTime Timestamp;

    public WebModel(StoryModel model) : base("New Webpage", StoryItemType.Web, model)
    {
        URL = new Uri("https://google.com/");
        Timestamp = DateTime.Now;
    }

    public WebModel(IXmlNode xn, StoryModel model) : base(xn, model)
    {
        URL = new Uri("https://google.com/");
        Timestamp = DateTime.MaxValue;
    }
}