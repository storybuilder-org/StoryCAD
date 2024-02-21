using StoryCAD.Controls;
using StoryCAD.Models;
using System.Collections.Generic;

namespace StoryCAD.Collaborator
{
    public interface IWizardStepModel
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
        bool IsCompleted { get; set; }
        string PageType { get; set; }
        Dictionary<string, string> Inputs { get; set; }
        string Prompt { get; set; }
        string Output { get; set; }
        //StoryElement StoryElement { get; set; }
    }
}
