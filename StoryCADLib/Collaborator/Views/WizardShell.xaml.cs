using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Navigation;
using StoryCAD.Collaborator.ViewModels;

namespace StoryCAD.Collaborator;

public sealed partial class WizardShell : Page
{
    public WizardViewModel WizardVm => Ioc.Default.GetService<WizardViewModel>();

    //public string UsageText
    //{
    //    get
    //    {
    //        if (WizardVm.CurrentStep != null)
    //        {
    //            return WizardVm.CurrentStep.UsageText;
    //        }

    //        return "";
    //    }
    //}

    public WizardShell()
    {
        this.InitializeComponent();

        (this.Content as FrameworkElement).DataContext = WizardVm;
        WizardVm.ContentFrame = StepFrame;
        WizardVm.NavView = NavView;
    }

    private void StepFrame_OnNavigated(object sender, NavigationEventArgs e)
    {
        ///TODO: Figure out what this is supposed to do
        //switch (e.SourcePageType.Name)
        //{
            //case "WelcomePage":
            //    //WizardVm.CurrentStep =
            //    //break;
            //case "ComboPicker":
            //    //WizardVm.NavView.DataContext = WizardVm.CurrentStep;
            //    //WizardVm.CurrentStep.Model = WizardVm.Model;
            //    //break;
            //case "TextAppender":
            //    //WizardVm.NavView.DataContext = WizardVm.CurrentStep;
            //    //WizardVm.CurrentStep.Model = WizardVm.Model;
            //    //break;
            //case "WizardPage":
            //WizardVm.NavView.DataContext = WizardVm.CurrentStep;
            //WizardVm.CurrentStep.Model = WizardVm.Model;
            //break;
            //default:
            //    throw new Exception("Invalid page type");
        //}



        /*  TODO: What does this actually do?
        What is step page? Why is current step set to 1 when its a IStepWizard?
        if ( == typeof(WelcomePage))
        { 
            //WizardVm.CurrentStep = 0;
        }
        else //if (/*e.SourcePageType == typeof(StepPage) false)
        {
            //WizardVm.CurrentStep = 1;
        }*/
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        WizardVm.NavView_SelectionChanged(sender, args);
    }
}