using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Navigation;
using StoryCAD.Collaborator.ViewModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace StoryCAD.Collaborator
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class WorkflowShell : Page
    {
        public WorkflowViewModel WorkflowVm => Ioc.Default.GetService<WorkflowViewModel>();

        public WorkflowShell()
        {
            InitializeComponent();
            (Content as FrameworkElement).DataContext = WorkflowVm;
            WorkflowVm.ContentFrame = StepFrame;
            WorkflowVm.NavView = NavView;
            WorkflowVm.WorkflowShellRoot = XamlRoot;
        }

        private void StepFrame_OnNavigated(object sender, NavigationEventArgs e)
        {
            //TODO: this
            /*
             * Navigation to a step in our Semantic Kernel application
             * will be quite different. A semantic function (plugin) is
             * a 'step', but there are no events associated with these
             * functions being launched.
             *
             * One possible approach would be to fire a native function
             * which runs an Invoke() on the corresponding step.
             *
             * I'm not even sure if these are needed at this point.
             */
        }


        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            WorkflowVm.NavView_SelectionChanged(sender, args);
        }
    }




}
