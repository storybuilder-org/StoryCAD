using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using StoryCAD.Collaborator.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace StoryCAD.Collaborator.Views
{
    /// <summary>
	/// Updated version of WizardPage
    /// </summary>
    public sealed partial class WorkflowPage : Page
    {
        public WorkflowViewModel WorkflowVm = Ioc.Default.GetService<WorkflowViewModel>();
        
        public WorkflowPage()
        {
	        WorkflowVm.Title = "test";
	        WorkflowVm.Description = "test description";
	        WorkflowVm.ConversationList = new()
	        {
		        "Assistant says hello",
		        "User says howdy"
	        };

            this.InitializeComponent();
            this.DataContext = WorkflowVm;
        }

        private void SendChat(AutoSuggestBox autoSuggestBox, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
	        throw new NotImplementedException();
        }
    }
}
