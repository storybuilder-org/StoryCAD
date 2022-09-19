using CommunityToolkit.Mvvm.ComponentModel;
using StoryBuilder.Services.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ABI.Microsoft.Web.WebView2.Core;
using CommunityToolkit.Mvvm.DependencyInjection;
using NLog;
using Microsoft.UI.Xaml.Controls;
using StoryBuilder.Services.Logging;
using StoryBuilder.Models;
using System.Xml.Linq;
using Microsoft.UI.Xaml;
using Octokit;
using Org.BouncyCastle.Utilities;
using System.Drawing;
using System.Security.Policy;
using CommunityToolkit.Mvvm.Messaging;
using StoryBuilder.Services.Messages;
using LogLevel = StoryBuilder.Services.Logging.LogLevel;

namespace StoryBuilder.ViewModels;

public class WebViewModel : ObservableRecipient, INavigable
{
    private bool _changed;    // this story element has changed

    private LogService Logger = Ioc.Default.GetRequiredService<LogService>();
    private string _name;
    public string Name
    {
        get => _name;
        set
        {
            if (_name != value) // Name changed?
            {
                Logger.Log(LogLevel.Info, $"Requesting Name change from {_name} to {value}");
                NameChangeMessage msg = new(_name, value);
                Messenger.Send(new NameChangedMessage(msg));
            }
            SetProperty(ref _name, value);
        }
    }

    private Guid _uuid;
    public Guid guid
    {
        get => _uuid;
        set => SetProperty(ref _uuid, value);
    }

    private string _query = "https://google.com/";
    public string Query
    {
        get => _query;
        set => SetProperty(ref _query, value);
    }

    private Uri _url = new Uri("https://google.com/") ;
    public Uri URL
    {
        get => _url;
        set { SetProperty(ref _url, value); }
    }

    private DateTime _timestamp;
    public DateTime Timestamp
    {
        get => _timestamp;
        set { SetProperty(ref _timestamp, value); }
    }

    private WebModel _model;
    public WebModel Model
    {
        get => _model;
        set => SetProperty(ref _model, value);
    }

    /// <summary>
    /// This is ran when the query box search icon is clicked.
    /// If its a URL then we navigate to it directly, if not
    /// search it with google.
    /// </summary>
    public void QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {

    }

    public void Activate(object parameter)
    {
        Model = (WebModel)parameter;
        LoadModel();
    }

    private void LoadModel()
    {
        guid = Model.Uuid;
        Name = Model.Name;
        URL = Model.URL;
        Timestamp = Model.Timestamp;

    }
    
    public void Deactivate(object parameter)
    {
        SaveModel();
    }

    public void SaveModel()
    {
        if (_changed)
        {
            // Story.Uuid is read-only and cannot be assigned
            Model.Name = Name;
            Model.URL = URL;
            Model.Timestamp = Timestamp;

        }
    }
}