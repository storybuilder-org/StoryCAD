using CommunityToolkit.Mvvm.ComponentModel;
using StoryCAD.Services.Navigation;

namespace StoryCAD.ViewModels;

public class TrashCanViewModel : ObservableRecipient, INavigable
{
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

    #region Methods

    public void Activate(object parameter)
    {
        Model = (TrashCanModel)parameter;
        LoadModel();
    }

    public void Deactivate(object parameter)
    {
    }

    private void LoadModel()
    {
        Uuid = Model.Uuid;
        Name = Model.Name;
    }

    #endregion
}
