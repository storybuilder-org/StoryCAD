using ABI.Microsoft.UI.Xaml;
using StoryCAD.Collaborator.ViewModels;

﻿namespace StoryCAD.Services.Collaborator;

public class CollaboratorArgs
{
    public StoryElement SelectedElement;

    public StoryModel StoryModel;
    
    public WorkflowViewModel WorkflowVm;

    public XamlRoot XamlRoot;

    //public WorkflowStepViewModel WorkflowStepVM;
    
    public delegate void OnDone();

    public OnDone onDoneCallback;
}