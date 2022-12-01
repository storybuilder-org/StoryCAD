using System.Diagnostics;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using StoryBuilder.Models;
using StoryBuilder.ViewModels;
using Syncfusion.UI.Xaml.Editors;

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
    public void AddScenePurposeMethod(string purpose)
    {
        ScenePurpose.SelectedItems.Add(purpose);
    }

    private void CastMember_Checked(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        CheckBox chk = sender as CheckBox;
        object item = chk.DataContext;
        if (item == null)
            return;
        StoryElement element = item as StoryElement;
        SceneVm.AddCastMember(element);
    }

    private void CastMember_Unchecked(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        CheckBox chk = sender as CheckBox;
        object item = chk.DataContext;
        if (item == null)
            return;
        StoryElement element = item as StoryElement;
        SceneVm.RemoveCastMember(element);
    }

    private void SelectionChanged(object sender, ComboBoxSelectionChangedEventArgs e)
    {
        SceneVm.UpdateScenePurpose(sender, e);
    }
}