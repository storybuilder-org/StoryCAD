namespace StoryCAD.Collaborator.Models;

public class WizardStepArgs
{
    public string Title { get; set; }
    public string Description { get; set; }
    public string InputText { get; set; }
    public string PromptText { get; set; }
    public string ChatModel { get; set; }
    public float Temperature { get; set; }
    public string OutputText { get; set; }
    public string UsageText { get; set; }
    public string PageType { get; set; }
    public Dictionary<string, string> Inputs { get; set; }
    public WizardStepArgs(string title)
    {
        Title = title;
        Description = string.Empty;
        InputText = string.Empty;
        PromptText = string.Empty;
        ChatModel = string.Empty;
        Temperature = 0.2f;
        OutputText = string.Empty;
        UsageText = string.Empty;
        PageType = string.Empty;
        Inputs = new Dictionary<string, string>();
    }
}