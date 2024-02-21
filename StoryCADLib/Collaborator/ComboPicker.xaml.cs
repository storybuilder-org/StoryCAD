using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCAD.Services.Collaborator;
using StoryCAD.Controls;

namespace StoryCAD.Collaborator.Views;

public sealed partial class ComboPicker : BindablePage
{
    public IWizardStepViewModel StepVM =
        Ioc.Default.GetService<CollaboratorService>()!.GetWizardStepViewModel();
    public ComboPicker()
    {
        this.InitializeComponent();
        this.DataContext = StepVM;
        Bindings.Update();
    }
}