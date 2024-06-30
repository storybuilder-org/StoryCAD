using Microsoft.UI.Xaml;
using StoryCAD.Controls;
using StoryCAD.Collaborator.ViewModels;

namespace StoryCAD.Collaborator.Views;

public sealed partial class TextAppender : BindablePage
{
    public WizardStepViewModel StepVM => Ioc.Default.GetService<WizardStepViewModel>();
    public TextAppender()
    {
        this.InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is WizardStepViewModel viewModel)
        {
            Bindings.Update();
        }
    }
}