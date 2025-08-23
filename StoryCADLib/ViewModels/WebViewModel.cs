using CommunityToolkit.Mvvm.ComponentModel;
using StoryCAD.Services.Navigation;
using CommunityToolkit.Mvvm.Messaging;
using StoryCAD.Services.Messages;
using System.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Web.WebView2.Core;
using System.Diagnostics;
using StoryCAD.Services;

namespace StoryCAD.ViewModels;

public class WebViewModel : ObservableRecipient, INavigable, ISaveable
{
    private readonly Windowing _window;
    private readonly AppState _appState;
    private readonly ILogService _logger;
    private readonly PreferenceService _preferenceService;

    public WebViewModel(Windowing window, AppState appState, ILogService logger, PreferenceService preferenceService)
    {
        _window = window;
        _appState = appState;
        _logger = logger;
        _preferenceService = preferenceService;
        
        PropertyChanged += OnPropertyChanged;
        RefreshCommand = new RelayCommand(ExecuteRefresh, () => true);
        GoBackCommand = new RelayCommand(ExecuteGoBack, () => true);
        GoForwardCommand = new RelayCommand(ExecuteGoForward, () => true);
    }

    ///TODO: Make sure queries are async
    #region Fields

    private bool _changed; // this story element has changed
    private bool _changeable;

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
                NameChangeMessage _msg = new(_name, value);
                Messenger.Send(new NameChangedMessage(_msg));
            }

            SetProperty(ref _name, value);
        }
    }

    private bool _isTextBoxFocused;
    public bool IsTextBoxFocused
    {
        get => _isTextBoxFocused;
        set => SetProperty(ref _isTextBoxFocused, value);
    }

    private Guid _uuid;
    public Guid Guid
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
    private Uri _url = new("https://google.com/");
    public Uri Url
    {
        get => _url;
        set => SetProperty(ref _url, value);
    }

    /// <summary>
    /// Last accessed URL/
    /// </summary>

    private DateTime _timestamp;
    public DateTime Timestamp
    {
        get => _timestamp;
        set => SetProperty(ref _timestamp, value);
    }

    private WebModel _model;
    public WebModel Model
    {
        get => _model;
        set => _model = value;
    }

    #endregion

    #region Methods

    /// <summary>
    /// This checks if web view is installed
    /// </summary>
    /// <returns>True if web view is installed</returns>
    public Task<bool> CheckWebViewState()
    {
        try
        {
            if (CoreWebView2Environment.GetAvailableBrowserVersionString() != null)
            {
                return Task.FromResult(true);
            }
        }
        catch { }
        return Task.FromResult(false);
    }


    /// <summary>
    /// This method installs the Evergreen Web View runtime.
    /// </summary>
    public async Task ShowWebViewDialog()
    {
	    _logger.Log(LogLevel.Error, "Showing WebView install dialog.");

        ContentDialog _dialog = new()
        {
            Title = "Web View is missing.",
            Content = "This computer is missing the WebView2 Runtime, without it some features may not work.\nWould you like to install this now?",
            PrimaryButtonText = "Yes",
            SecondaryButtonText = "No"
        };

        ContentDialogResult _result = await _window.ShowContentDialog(_dialog);
        _logger.Log(LogLevel.Error, $"User clicked {_result}");

        //Ok clicked
        if (_result == ContentDialogResult.Primary) { await InstallWebView(); }
    }

    /// <summary>
    /// This installs the evergreen WebView runtime
    /// </summary>
    public async Task InstallWebView()
    {
        try
        {
	        _logger.Log(LogLevel.Error, "Installing WebView runtime");

            //Download file
            HttpResponseMessage _httpResult =
                await new HttpClient().GetAsync(
                    "https://go.microsoft.com/fwlink/p/?LinkId=2124703"); //Get HTTP response
            await using Stream _resultStream = await _httpResult.Content.ReadAsStreamAsync(); //Read stream
            await using FileStream _fileStream =
                File.Create(Path.Combine(_appState.RootDirectory, "evergreenbootstrapper.exe")); //Create File.
            await _resultStream.CopyToAsync(_fileStream); //Write file
            await _fileStream.FlushAsync(); //Flushes steam.
            await _fileStream.DisposeAsync(); //Cleans up resources

            //Run installer and wait for it to finish
            await Process.Start(Path.Combine(_appState.RootDirectory, "evergreenbootstrapper.exe"))!
                .WaitForExitAsync();

            //Show success/fail dialog
            ContentDialog _dialog = new() { PrimaryButtonText = "Ok" };
            try
            {
                _dialog.Title = "WebView installed!";
                _dialog.Content = "You are good to go, everything should work now.";
                _logger.Log(LogLevel.Info, "WebView installed");
            }
            catch (Exception _ex)
            {
                _dialog.Title = "Something went wrong.";
                _dialog.Content = "Looks like something went wrong, you can still use StoryCAD " +
                                  "however some features may not work.";
                _logger.Log(LogLevel.Warn, $"An error occurred installing the Evergreen" +
                                           $" WebView Runtime ({_ex.Message})");
            }

            await _window.ShowContentDialog(_dialog);
            _logger.Log(LogLevel.Warn, "Finished installing WebView runtime.");
        }
        catch (Exception _ex)
        {
            _logger.LogException(LogLevel.Warn, _ex,  $"Error installing WebView runtime. " +
                                                      $"({_ex.Message})");
        }
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

    public void Deactivate(object parameter) { SaveModel(); }

    private void OnPropertyChanged(object sender, PropertyChangedEventArgs args)
    { 
        if (_changeable)
        {
            if (!_changed)
                _logger.Log(LogLevel.Info, $"WebViewModel.OnPropertyChanged: {args.PropertyName} changed");
            _changed = true;
            ShellViewModel.ShowChange();
        }
    }

    /// <summary>
    /// Ran when this model is loaded.
    /// </summary>
    private void LoadModel()
    {
        _changeable = false;
        _changed = false;
        
        Guid = Model.Uuid;
        Name = Model.Name;
        if (Name.Equals("New Webpage"))
            IsTextBoxFocused = true;
        Url = Model.URL;
        Timestamp = Model.Timestamp;

        _changeable = true;
    }

    public void SaveModel()
    {
        try
        {
            Model.Name = Name;
            IsTextBoxFocused = false;
            Model.URL = Url;
            Model.Timestamp = Timestamp;
        }
        catch (Exception ex)
        {
	        _logger.LogException(LogLevel.Error, ex, $"Failed to save WebVM {ex.Message}");
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
                Url = new Uri(Query);
                _logger.Log(LogLevel.Info, $"{Query} is a valid URI, navigating to it.");
            }

        }
        catch (UriFormatException)
        {
            _logger.Log(LogLevel.Info, $"Checking if {Query} is not URI, searching it.");
            if (Query == null) { Query = string.Empty; }
            string _dataString = Uri.EscapeDataString(Query);
            switch (_preferenceService.Model.PreferredSearchEngine)
            {
                case BrowserType.DuckDuckGo:
                    Url = new Uri("https://duckduckgo.com/?va=j&q=" + _dataString);
                    break;
                case BrowserType.Google:
                    Url = new Uri("https://www.google.com/search?q=" + _dataString);
                    break;
                case BrowserType.Bing:
                    Url = new Uri("https://www.bing.com/search?q=" + _dataString);
                    break;
                case BrowserType.Yahoo:
                    Url = new Uri("https://search.yahoo.com/search?p=" + _dataString);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            _logger.Log(LogLevel.Info, $"URL is: {Url}");
        }
        catch (Exception _ex) { _logger.LogException(LogLevel.Error, _ex, "Error in WebVM.SubmitQuery()"); }
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

    // Constructor for XAML compatibility - will be removed later
    public WebViewModel() : this(
        Ioc.Default.GetRequiredService<Windowing>(),
        Ioc.Default.GetRequiredService<AppState>(),
        Ioc.Default.GetRequiredService<ILogService>(),
        Ioc.Default.GetRequiredService<PreferenceService>())
    {
    }
}