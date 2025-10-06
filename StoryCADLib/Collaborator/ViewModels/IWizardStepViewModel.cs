namespace StoryCAD.Collaborator.Views;

public interface IWizardStepViewModel
{
    // Properties
    public string Title { get; set; }
    public string Description { get; set; }
    public string InputText { get; set; }
    public string PromptText { get; set; }
    public string ChatModel { get; set; }
    public float Temperature { get; set; }
    public string OutputText { get; set; }
    public string UsageText { get; set; }
    public bool IsCompleted { get; set; }
    public string PageType { get; set; }
    public Dictionary<string, string> Inputs { get; set; }
    public string Prompt { get; set; }
    public string Output { get; set; }
}
