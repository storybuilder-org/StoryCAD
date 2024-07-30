using CommunityToolkit.Mvvm.ComponentModel;
//using StoryCollaborator.Services;
using StoryCAD.Services.Navigation;
//using StoryCAD.Collaborator;
using StoryCAD.Controls;
using StoryCAD.Services.Collaborator;

namespace StoryCAD.Collaborator.ViewModels;

public class WizardStepViewModel : ObservableRecipient, INavigable
{
    private string _title;

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    private string _description;

    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    private string _inputText;

    public string InputText
    {
        get => _inputText;
        set => SetProperty(ref _inputText, value);
    }

    private string _promptText;

    public string PromptText
    {
        get => _promptText;
        set => SetProperty(ref _promptText, value);
    }

    private string _chatModel;

    public string ChatModel
    {
        get => _chatModel;
        set => SetProperty(ref _chatModel, value);
    }

    private float _temperature;

    public float Temperature
    {
        get => _temperature;
        set => SetProperty(ref _temperature, value);
    }

    private string _outputText;

    public string OutputText
    {
        get => _outputText;
        set => SetProperty(ref _outputText, value);
    }

    private string _usageText;

    public string UsageText
    {
        get => _usageText;
        set => SetProperty(ref _usageText, value);
    }


    private BindablePage _pageInstance;

    public BindablePage PageInstance
    {
        get => _pageInstance;
        set => SetProperty(ref _pageInstance, value);
    }

    public bool IsCompleted { get; set; }

    public string PageType { get; set; }

    public Dictionary<string, string> Inputs { get; set; }

    public string Prompt { get; set; }
    public string Output { get; set; }
    public StoryElement Model { get; set; }

    public WizardStepViewModel()
    {
        Title = string.Empty;
        Description = string.Empty;
        PageType = string.Empty;
        Prompt = string.Empty;
        Inputs = new Dictionary<string, string>();
        Output = string.Empty;
        Temperature = 0.2f;
    }

    public async void Activate(object parameter)
    {
        //var chat = Ioc.GetService<ChatService>();

        //Model = (CharacterModel)parameter;
        LoadModel(); // Load the ViewModel from the Story
    }

    public void LoadModel()
    {
        //Ioc.Default.GetService<CollaboratorService>()!.LoadWizardStepViewModel();
    }

    public void Deactivate(object parameter)
    {
        //SaveModel(); // Save the ViewModel back to the Story
    }
}