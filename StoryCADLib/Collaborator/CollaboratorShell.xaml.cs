using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Page = Microsoft.UI.Xaml.Controls.Page;

namespace StoryCAD.Collaborator;

public sealed partial class CollaboratorShell : Page
{
    public IWizardViewModel CollabVM = null;

    //public string UsageText
    //{
    //    get
    //    {
    //        if (CollabVM.CurrentStep != null)
    //        {
    //            return CollabVM.CurrentStep.UsageText;
    //        }

    //        return "";
    //    }
    //}

    public CollaboratorShell(IWizardViewModel wizard )
    {
        this.InitializeComponent();

        CollabVM = wizard;
        (this.Content as FrameworkElement).DataContext = CollabVM;

        CollabVM.ContentFrame = StepFrame;
        CollabVM.NavView = NavView;
    }

    private void StepFrame_OnNavigated(object sender, NavigationEventArgs e)
    {
        ///TODO: Figure out what this is supposed to do
        switch (e.SourcePageType.Name)
        {
            case "WelcomePage":
                //CollabVM.CurrentStep =
                //break;
            case "ComboPicker":
                //CollabVM.NavView.DataContext = CollabVM.CurrentStep;
                //CollabVM.CurrentStep.Model = CollabVM.Model;
                //break;
            case "TextAppender":
                //CollabVM.NavView.DataContext = CollabVM.CurrentStep;
                //CollabVM.CurrentStep.Model = CollabVM.Model;
                break;
            default:
                throw new Exception("Invalid page type");
                break;
        }



        /*  TODO: What does this actually do?
        What is step page? Why is current step set to 1 when its a IStepWizard?
        if ( == typeof(WelcomePage))
        { 
            //CollabVM.CurrentStep = 0;
        }
        else //if (/*e.SourcePageType == typeof(StepPage) false)
        {
            //CollabVM.CurrentStep = 1;
        }*/
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        CollabVM.NavView_SelectionChanged(sender, args);
    }
}