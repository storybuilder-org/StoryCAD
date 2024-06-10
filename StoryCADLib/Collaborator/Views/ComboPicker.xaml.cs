using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCAD.Services.Collaborator;
using StoryCAD.Collaborator.ViewModels;
using StoryCAD.Controls;

namespace StoryCAD.Collaborator.Views;

public sealed partial class ComboPicker : BindablePage
{
    public WizardStepViewModel StepVM =
        //Ioc.Default.GetService<CollaboratorService>()!.GetWizardStepViewModel();
        Ioc.Default.GetService<WizardStepViewModel>();
    public ComboPicker()
    {
        this.InitializeComponent();
        this.DataContext = StepVM;
        //Bindings.Update();
    }
}