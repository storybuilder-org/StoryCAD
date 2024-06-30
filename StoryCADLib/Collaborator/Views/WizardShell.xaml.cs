using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Navigation;
using StoryCAD.Collaborator.ViewModels;

namespace StoryCAD.Collaborator;

public sealed partial class WizardShell : Page
{
    public WizardViewModel WizardVM => Ioc.Default.GetService<WizardViewModel>();

    //public string UsageText
    //{
    //    get
    //    {
    //        if (WizardVM.CurrentStep != null)
    //        {
    //            return WizardVM.CurrentStep.UsageText;
    //        }

    //        return "";
    //    }
    //}

    public WizardShell()
    {
        this.InitializeComponent();

        (this.Content as FrameworkElement).DataContext = WizardVM;
        WizardVM.ContentFrame = StepFrame;
        WizardVM.NavView = NavView;
    }

    private void StepFrame_OnNavigated(object sender, NavigationEventArgs e)
    {
        ///TODO: Figure out what this is supposed to do
        switch (e.SourcePageType.Name)
        {
            case "WelcomePage":
                //WizardVM.CurrentStep =
                //break;
            case "ComboPicker":
                //WizardVM.NavView.DataContext = WizardVM.CurrentStep;
                //WizardVM.CurrentStep.Model = WizardVM.Model;
                //break;
            case "TextAppender":
                //WizardVM.NavView.DataContext = WizardVM.CurrentStep;
                //WizardVM.CurrentStep.Model = WizardVM.Model;
                break;
            default:
                throw new Exception("Invalid page type");
        }



        /*  TODO: What does this actually do?
        What is step page? Why is current step set to 1 when its a IStepWizard?
        if ( == typeof(WelcomePage))
        { 
            //WizardVM.CurrentStep = 0;
        }
        else //if (/*e.SourcePageType == typeof(StepPage) false)
        {
            //WizardVM.CurrentStep = 1;
        }*/
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        WizardVM.NavView_SelectionChanged(sender, args);
    }
}