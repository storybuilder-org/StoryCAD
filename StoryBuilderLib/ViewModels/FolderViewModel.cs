using System;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using StoryBuilder.DAL;
using StoryBuilder.Models;
using StoryBuilder.Services.Logging;
using StoryBuilder.Services.Messages;
using StoryBuilder.Services.Navigation;

namespace StoryBuilder.ViewModels;

/// <summary>
/// A Folder StoryElement is a divider in the Story ExplorerView
/// view. A 'folder' in the NarratorView view, by contrast, 
/// is a Section StoryElement. A Folder can have anything as
/// a parent (including another Folder.) A Section can only have
/// another Section as its parent. Sections are Chapters, Acts,
/// etc.
/// </summary>
public class FolderViewModel : ObservableRecipient, INavigable
{
    #region Fields

    private readonly LogService _logger;
    private readonly StoryReader _rdr;
    private readonly StoryWriter _wtr;
    private bool _changeable; // process property changes for this story element
    private bool _changed;    // this story element has changed

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
                NameChangeMessage msg = new(_name, value);
                Messenger.Send(new NameChangedMessage(msg));
            }
            SetProperty(ref _name, value);
        }
    }

    // Folder data

    private string _notes;
    public string Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value);
    }

    // The StoryModel is passed when FolderPage is navigated to
    private FolderModel _model;
    public FolderModel Model
    {
        get => _model;
        set => SetProperty(ref _model, value);
    }

    #endregion

    #region Methods

    public void Activate(object parameter)
    {
        Model = (FolderModel)parameter;
        LoadModel();  // Load the ViewModel from the Story
    }

    public void Deactivate(object parameter)
    {
         SaveModel();    // Save the ViewModel back to the Story
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

        Uuid = Model.Uuid;
        Name = Model.Name;
        Notes = Model.Notes;

        _changeable = true;
    }

    internal void SaveModel()
    {
        if (_changed)
        {
            // Story.Uuid is read-only; no need to save
            Model.Name = Name;

            // Write RYG file
            Model.Notes = Notes;
        }
    }

    #endregion

    #region Constructor

    public FolderViewModel()
    {
        _logger = Ioc.Default.GetService<LogService>();
        _wtr = Ioc.Default.GetService<StoryWriter>();
        _rdr = Ioc.Default.GetService<StoryReader>();

        Notes = string.Empty;

        PropertyChanged += OnPropertyChanged;
    }

    #endregion
}