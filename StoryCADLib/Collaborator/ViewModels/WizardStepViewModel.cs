using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.ComponentModel;
//using StoryCollaborator.Services;
using StoryCAD.Models;
using StoryCAD.Services.Navigation;
using OpenAI.ObjectModels.RequestModels;
//using StoryCAD.Collaborator;
using StoryCAD.Controls;
using Microsoft.Extensions.DependencyInjection;
using StoryCAD.Collaborator.Views;
using StoryCAD.Collaborator.ViewModels;

namespace StoryCAD.Collaborator.ViewModels;

public class WizardStepViewModel: ObservableRecipient, INavigable
{
    private string _title;
    public string Title 
    { 
        get => _title; 
        set => SetProperty(ref _title, value); }

    private string _description;
    public string Description
    {
        get => _description; 
        set=> SetProperty(ref _description, value);
    }

    private string _inputText;
    public  string InputText
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
    public StoryElement Model
    {
        get; 
        set;
    }

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
        Debugger.Break();
        //var chat = Ioc.GetService<ChatService>();

        //Model = (CharacterModel)parameter;
        //LoadModel(); // Load the ViewModel from the Story
    }
    public void LoadModel()
    {
        Debugger.Break();
        GetInputValues();
    }

    private void GetInputValues()
    {
        Debugger.Break();
        //TODO: I need this, just not sure where. Move to Collaborator?
        // Maybe call from ProcessStep(), which would run Chat too?
        var vm = Ioc.Default.GetService<WizardViewModel>();
        foreach (string key in Inputs.Keys)
        {
            if (vm!.ModelProperties.ContainsKey(key))
            {
                string value = (string) vm.ModelProperties[key].GetValue(vm.Model);
                Inputs[key] = value;
            }
        }
    }

    public void Deactivate(object parameter)
    {
        //SaveModel(); // Save the ViewModel back to the Story
    }

    //public async Task<ChatResponse> RunStepCompletion(List<ChatMessage> prompt)
    //{
    //    //ConsoleExtensions.WriteLine("Chat Completion Testing is starting:", ConsoleColor.Cyan);

    //    try
    //    {
    //        var reply = await chat.RunConversation(prompt,ChatModel);

    //        if (reply.Status.Successful)
    //        {
    //            OutputText = reply.Choices.First().Message.Content;
    //            UsageText = reply.Usage.ToString();
    //            //Console.WriteLine(completionResult.Choices.First().Message.Content);
    //        }
    //        else
    //        {
    //            if (reply.ErrorMessage == null)
    //            {
    //                throw new Exception("Unknown Error");
    //            }
    //            Console.WriteLine((object)$"{reply.ErrorCode}: {reply.ErrorMessage}");
    //        }
    //    }
    //    catch (Exception e)
    //    {
    //        Console.WriteLine(e);
    //        throw;
    //    }
    //}
}