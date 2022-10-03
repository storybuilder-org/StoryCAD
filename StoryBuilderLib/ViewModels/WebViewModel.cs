using CommunityToolkit.Mvvm.ComponentModel;
using StoryBuilder.Services.Navigation;
using System;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using StoryBuilder.Services.Logging;
using StoryBuilder.Models;
using CommunityToolkit.Mvvm.Messaging;
using StoryBuilder.Services.Messages;
using LogLevel = StoryBuilder.Services.Logging.LogLevel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace StoryBuilder.ViewModels;

public class WebViewModel : ObservableRecipient, INavigable
{
    ///TODO: Make sure queries are async
    ///TODO: Do we need WebView2Core, WebView2Environment?

    #region Fields

    private bool _changed; // this story element has changed
    private bool _changeable;
    private LogService _logger = Ioc.Default.GetRequiredService<LogService>();

    #endregion

    #region Properties
    
    private string _name;
    public string Name
    {
        get => _name;
        set
        {
            if (_name != value) // Name changed?
            {
                _logger.Log(LogLevel.Info, $"Requesting Name change from {_name} to {value}");
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

    /// <summary>
    /// This is the whats shown on the UI.
    /// </summary>
    private string _query = "https://google.com/";
    public string Query
    {
        get => _query;
        set => SetProperty(ref _query, value);
    }

    /// <summary>
    /// This is the real webview URL, it may differ query but usually should be the same.
    /// </summary>
    private Uri _url = new Uri("https://google.com/");
    public Uri URL
    {
        get => _url;
        set { SetProperty(ref _url, value); }
    }

    /// <summary>
    /// Last accessed URL/
    /// </summary>

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

    #endregion  

    #region Relay Commands

    public RelayCommand RefreshCommand { get; }
    public RelayCommand GoBackCommand { get; }
    public RelayCommand GoForwardCommand { get; }

    #endregion
    
    public void Activate(object parameter)
    {
        Model = (WebModel)parameter;
        LoadModel();
    }

    public void Deactivate(object parameter)
    {
        SaveModel();
    }

    private void OnPropertyChanged(object sender, PropertyChangedEventArgs args)
    { 
        if (_changeable)
        {
            _changed = true;
            ShellViewModel.ShowChange();
        }
    }
    private void LoadModel()
    {
        _changeable = false;
        _changed = false;
        
        guid = Model.Uuid;
        Name = Model.Name;
        URL = Model.URL;
        Timestamp = Model.Timestamp;

        _changeable = true;
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

    /// <summary>
    /// This is ran when the AutoSuggestBox search icon is clicked.
    /// If it's a URL, navigate to it directly, if not,
    /// search it with google.
    /// </summary>
    public void SubmitQuery()
    {
        //Prevent crash as URI cast cant be empty.
        try
        {
            if (!string.IsNullOrEmpty(Query))
            {
                _logger.Log(LogLevel.Info, $"Checking if {Query} is a URI.");
                URL = new Uri(Query);
                _logger.Log(LogLevel.Info, $"{Query} is a valid URI, navigating to it.");
            }

        }
        catch (UriFormatException ex)
        {
            _logger.Log(LogLevel.Info, $"Checking if {Query} is not URI, searching it.");
            URL = new Uri("https://www.google.com/search?q=" + Uri.EscapeDataString(Query));
            _logger.Log(LogLevel.Info, $"URL is: {URL}");

        }
    }

    /// Delegate types and instances for WebView2 navigation controls
    public delegate void RefreshDelegate();
    public delegate void GoForwardDelegate();
    public delegate void GoBackDelegate();

    public RefreshDelegate Refresh;
    public GoForwardDelegate GoForward;
    public GoBackDelegate GoBack;

    private void ExecuteRefresh() { Refresh(); }

    private void ExecuteGoBack() { GoBack(); }

    private void ExecuteGoForward() { GoForward(); }

    public WebViewModel()
    {
        _logger = Ioc.Default.GetService<LogService>();
        PropertyChanged += OnPropertyChanged;

        RefreshCommand = new RelayCommand(ExecuteRefresh, () => true);
        GoBackCommand = new RelayCommand(ExecuteGoBack, () => true);
        GoForwardCommand = new RelayCommand(ExecuteGoForward, () => true);
    }
}