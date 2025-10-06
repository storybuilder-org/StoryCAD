using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using StoryCADLib.Services;
using StoryCADLib.Services.Messages;
using StoryCADLib.Services.Navigation;

namespace StoryCADLib.ViewModels;

/// <summary>
///     A Folder StoryElement is a divider in the Story ExplorerView
///     view. A 'folder' in the NarratorView view, by contrast,
///     is a Section StoryElement. A Folder can have anything as
///     a parent (including another Folder.) A Section can only have
///     another Section as its parent. Sections are Chapters, Acts,
///     etc.
/// </summary>
public class FolderViewModel : ObservableRecipient, INavigable, ISaveable
{
    #region Fields

    private readonly ILogService _logger;
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

    // Folder data

    // Description property (migrated from Notes)
    private string _description;

    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    // The StoryModel is passed when FolderPage is navigated to
    private FolderModel _model;

    public FolderModel Model
    {
        get => _model;
        set => _model = value;
    }

    #endregion

    #region Methods

    public void Activate(object parameter)
    {
        Model = (FolderModel)parameter;
        LoadModel(); // Load the ViewModel from the Story
    }

    public void Deactivate(object parameter)
    {
        SaveModel(); // Save the ViewModel back to the Story
    }

    private void OnPropertyChanged(object sender, PropertyChangedEventArgs args)
    {
        if (_changeable)
        {
            if (!_changed)
            {
                _logger.Log(LogLevel.Info, $"FolderViewModel.OnPropertyChanged: {args.PropertyName} changed");
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
        if (Name.Equals("New Folder") || Name.Equals("New Note"))
        {
            IsTextBoxFocused = true;
        }

        if (Name.Equals("New Section"))
        {
            IsTextBoxFocused = true;
        }

        Description = Model.Description;

        _changeable = true;
    }

    public void SaveModel()
    {
        // Story.Uuid is read-only; no need to save
        Model.Name = Name;
        IsTextBoxFocused = false;

        // Write RYG file
        Model.Description = Description;
    }

    #endregion

    #region Constructor

    // Constructor for XAML compatibility - will be removed later
    public FolderViewModel() : this(
        Ioc.Default.GetRequiredService<ILogService>())
    {
    }

    public FolderViewModel(ILogService logger)
    {
        _logger = logger;
        Description = string.Empty;
        PropertyChanged += OnPropertyChanged;
    }

    #endregion
}
