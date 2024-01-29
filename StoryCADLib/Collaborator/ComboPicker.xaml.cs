using Microsoft.UI.Xaml;
using StoryCAD.Controls;

namespace StoryCAD.Collaborator;

public sealed partial class ComboPicker : BindablePage
{
    public IWizardStepViewModel StepVM => (IWizardStepViewModel)DataContext;
    public ComboPicker()
    {
        this.InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        StepVM.PageInstance = this;
        Bindings.Update();
        StepVM.Activate(null);
    }
}