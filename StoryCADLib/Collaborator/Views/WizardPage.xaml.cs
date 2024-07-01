using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCAD.Collaborator.ViewModels;
using StoryCAD.Controls;

namespace StoryCAD.Collaborator.Views;
public sealed partial class WizardPage : BindablePage
{
    public WizardStepViewModel StepVm = Ioc.Default.GetService<WizardStepViewModel>();
    public WizardPage()
    {
        this.InitializeComponent();
        this.DataContext = StepVm;
    }
}