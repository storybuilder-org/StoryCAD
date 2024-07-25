using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace StoryCAD.Collaborator.ViewModels
{ 
    public class WorkflowViewModel: ObservableRecipient 
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public ObservableCollection<WorkflowStep> WorkflowSteps { get; set; }
        public ObservableCollection<string> ConversationList { get; set; }
        public string InputText { get; set; }  //  Chat input

        
        public WorkflowViewModel()
        {
            Title = string.Empty;
            Description = string.Empty;
            WorkflowSteps = new ObservableCollection<WorkflowStep>();
            InputText = string.Empty;
        }
    }
}
