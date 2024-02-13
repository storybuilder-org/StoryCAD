using Microsoft.UI.Xaml;
using StoryCAD.Controls;

namespace StoryCAD.Collaborator.Views;

public sealed partial class TextAppender : BindablePage
{
    public IWizardStepViewModel StepVM => DataContext as IWizardStepViewModel;
    public TextAppender()
    {
        this.InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is IWizardStepViewModel viewModel)
        {
            viewModel.PageInstance = this;
            Bindings.Update();
        }
    }
}