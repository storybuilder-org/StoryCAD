using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using StoryCADLib.Services;
using StoryCADLib.Services.Messages;
using StoryCADLib.Services.Navigation;

namespace StoryCADLib.ViewModels;

public class SettingViewModel : ObservableRecipient, INavigable, ISaveable, IReloadable
{
    #region Fields

    private readonly ILogService _logger;
    private readonly ListData _listData;
    private readonly Windowing _windowing;
    private bool _changeable; // process property changes for this story element
    private bool _changed; // this story element has changed

    #endregion

    #region Properties

    // StoryElement data

    private Guid _uuid;

    public Guid Uuid
    {
        get => _uuid;
        set => SetProperty(ref _uuid, value);
    }

    private string _name;

    public string Name
    {
        get => _name;
        set
        {
            if (_changeable && _name != value) // Name changed?
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

    // Setting (General) data

    private string _locale;

    public string Locale
    {
        get => _locale;
        set => SetProperty(ref _locale, value ?? "");
    }

    private string _season;

    public string Season
    {
        get => _season;
        set => SetProperty(ref _season, value);
    }

    private string _period;

    public string Period
    {
        get => _period;
        set => SetProperty(ref _period, value);
    }

    private string _lighting;

    public string Lighting
    {
        get => _lighting;
        set => SetProperty(ref _lighting, value);
    }

    private string _weather;

    public string Weather
    {
        get => _weather;
        set => SetProperty(ref _weather, value);
    }

    private string _temperature;

    public string Temperature
    {
        get => _temperature;
        set => SetProperty(ref _temperature, value);
    }

    private string _props;

    public string Props
    {
        get => _props;
        set => SetProperty(ref _props, value);
    }

    // Description property (migrated from Summary)
    private string _description;

    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    // Setting Sense data

    private string _sights;

    public string Sights
    {
        get => _sights;
        set => SetProperty(ref _sights, value);
    }

    private string _sounds;

    public string Sounds
    {
        get => _sounds;
        set => SetProperty(ref _sounds, value);
    }

    private string _touch;

    public string Touch
    {
        get => _touch;
        set => SetProperty(ref _touch, value);
    }

    private string _smellTaste;

    public string SmellTaste
    {
        get => _smellTaste;
        set => SetProperty(ref _smellTaste, value);
    }

    // Setting notes data

    private string _notes;

    public string Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value);
    }

    // The StoryModel is passed when CharacterPage is navigated to
    private SettingModel _model;

    public SettingModel Model
    {
        get => _model;
        set => _model = value;
    }

    #endregion

    #region Methods

    public void Activate(object parameter)
    {
        _changeable = false;  // Disable change tracking before setting Model
        _changed = false;
        Model = (SettingModel)parameter;
        LoadModel();
    }

    public void Deactivate(object parameter)
    {
        SaveModel(); // Save the ViewModel back to the Story
    }

    private void OnPropertyChanged(object sender, PropertyChangedEventArgs args)
    {
        // Ignore Model property changes - these happen during navigation, not user edits
        if (args.PropertyName == nameof(Model))
            return;

        if (_changeable)
        {
            if (!_changed)
            {
                _logger.Log(LogLevel.Info, $"SettingViewModel.OnPropertyChanged: {args.PropertyName} changed");
            }

            _changed = true;
            ShellViewModel.ShowChange();
        }
    }

    private void LoadModel()
    {
        _changeable = false;
        _changed = false;

        Uuid = Model.Uuid;
        Name = Model.Name;
        if (Name.Equals("New Setting"))
        {
            IsTextBoxFocused = true;
        }

        Locale = Model.Locale;
        Season = Model.Season;
        Period = Model.Period;
        Lighting = Model.Lighting;
        Weather = Model.Weather;
        Temperature = Model.Temperature;
        Props = Model.Props;
        Description = Model.Description;
        Sights = Model.Sights;
        Sounds = Model.Sounds;
        Touch = Model.Touch;
        SmellTaste = Model.SmellTaste;
        Notes = Model.Notes;

        _changeable = true;
    }

    public void SaveModel()
    {
        try
        {
            // Story.Uuid is read-only and cannot be assigned
            Model.Name = Name;
            IsTextBoxFocused = false;
            Model.Locale = Locale;
            Model.Season = Season;
            Model.Period = Period;
            Model.Lighting = Lighting;
            Model.Weather = Weather;
            Model.Temperature = Temperature;
            Model.Props = Props;

            //Write RTF files
            Model.Description = Description;
            Model.Sights = Sights;
            Model.Sounds = Sounds;
            Model.Touch = Touch;
            Model.SmellTaste = SmellTaste;
            Model.Notes = Notes;
        }
        catch (Exception ex)
        {
            _logger.LogException(LogLevel.Error,
                ex, $"Failed to save setting model - {ex.Message}");
        }
    }

    public void ReloadFromModel()
    {
        if (Model != null)
        {
            LoadModel();
        }
    }

    #endregion

    #region ComboBox ItemsSource collections

    public ObservableCollection<string> LocaleList;
    public ObservableCollection<string> SeasonList;

    #endregion

    #region Constructor

    public SettingViewModel(ILogService logger, ListData listData, Windowing windowing)
    {
        _logger = logger;
        _listData = listData;
        _windowing = windowing;

        Locale = string.Empty;
        Season = string.Empty;
        Period = string.Empty;
        Lighting = string.Empty;
        Weather = string.Empty;
        Temperature = string.Empty;
        Props = string.Empty;
        Description = string.Empty;
        Sights = string.Empty;
        Sounds = string.Empty;
        Touch = string.Empty;
        SmellTaste = string.Empty;
        Notes = string.Empty;

        try
        {
            var _lists = _listData.ListControlSource;
            LocaleList = _lists["Locale"];
            SeasonList = _lists["Season"];
        }
        catch (Exception e)
        {
            _logger.LogException(LogLevel.Fatal, e, "Error loading lists in Problem view model");
            _windowing.ShowResourceErrorMessage();
        }


        PropertyChanged += OnPropertyChanged;
    }

    #endregion
}
