using System.Collections.Generic;
using StoryCAD.Controls;
using StoryCAD.Models;

namespace StoryCAD.Collaborator.Views
{
    public interface IWizardStepViewModel
    {
        // Properties
        string Title { get; set; }
        string Description { get; set; }
        string InputText { get; set; }
        string PromptText { get; set; }
        string ChatModel { get; set; }
        float Temperature { get; set; }
        string OutputText { get; set; }
        string UsageText { get; set; }
        BindablePage PageInstance { get; set; }
        bool IsCompleted { get; set; }
        string PageType { get; set; }
        Dictionary<string, string> Inputs { get; set; }
        string Prompt { get; set; }
        string Output { get; set; }
        StoryElement Model { get; set; }

        // Methods
        void Activate(object parameter);
        void Deactivate(object parameter);
    }
}
