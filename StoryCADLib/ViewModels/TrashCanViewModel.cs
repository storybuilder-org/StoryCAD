using CommunityToolkit.Mvvm.ComponentModel;
using StoryCADLib.Services.Navigation;

namespace StoryCADLib.ViewModels;

[Microsoft.UI.Xaml.Data.Bindable]
public class TrashCanViewModel : ObservableRecipient, INavigable
{
    #region Fields

    private readonly ILogService _logger;

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
        set => SetProperty(ref _name, value);
    }

    // There is no data for this View   

    // The StoryModel is passed when TrashCanPage is navigated to
    private TrashCanModel _model;

    public TrashCanModel Model
    {
        get => _model;
        set => _model = value;
    }

    #endregion

    #region Constructor

    public TrashCanViewModel(ILogService logger)
    {
        _logger = logger;
    }

    #endregion

    #region Methods

    public void Activate(object parameter)
    {
        var param = parameter as TrashCanModel;
        _logger.Log(LogLevel.Info, $"TrashCanViewModel.Activate: parameter={param?.Name} (Uuid={param?.Uuid})");
        Model = (TrashCanModel)parameter;
        _logger.Log(LogLevel.Info, $"TrashCanViewModel.Activate: Model set to {Model?.Name} (Uuid={Model?.Uuid})");
        LoadModel();
    }

    public void Deactivate(object parameter)
    {
        _logger.Log(LogLevel.Info, "TrashCanViewModel.Deactivate: (no-op)");
    }

    private void LoadModel()
    {
        Uuid = Model.Uuid;
        Name = Model.Name;
    }

    #endregion
}
