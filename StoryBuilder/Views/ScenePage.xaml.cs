using CommunityToolkit.Mvvm.DependencyInjection;
using StoryBuilder.ViewModels;

namespace StoryBuilder.Views;

public sealed partial class ScenePage : BindablePage
{
    public SceneViewModel SceneVm => Ioc.Default.GetService<SceneViewModel>();

    public ScenePage()
    {
        InitializeComponent();
        DataContext = SceneVm;
        SceneVm.ClearScenePurpose = ClearScenePurposeMethod;
        SceneVm.AddScenePurpose = AddScenePurposeMethod;
    }

    /// <summary>
    /// Clear the ScenePurpose SfComboBox SelectedItems property. 
    /// 
    /// This method is called via proxy from SceneViewModel,
    /// because the binding to SelectedItems is read-only.
    /// We therefore update the ComboBox here via callback.
    /// </summary>
    /// <param name="purpose"></param>
    public void ClearScenePurposeMethod()
    {
        ScenePurpose.SelectedItems.Clear();
    }

    /// <summary>
    /// Add a 'purpose of scene' value to the ScenePurpose
    /// SfComboBox SelectedItems property. 
    /// 
    /// This method is called via proxy from SceneViewModel,
    /// because the binding to SelectedItems is read-only.
    /// We therefore update the ComboBox here via callback.
    /// </summary>
    /// <param name="purpose"></param>
    public void AddScenePurposeMethod (string purpose)
    {     
        ScenePurpose.SelectedItems.Add(purpose);
    }
}