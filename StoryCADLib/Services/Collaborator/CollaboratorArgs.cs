using Microsoft.UI.Xaml;
using StoryCAD.Collaborator.ViewModels;

﻿namespace StoryCAD.Services.Collaborator;

public class CollaboratorArgs
{
    public StoryModel StoryModel;
    
    public WorkflowViewModel WorkflowVm;

	/// <summary>
	/// Reference to the Collaborator window
	/// </summary>
    public Window CollaboratorWindow;

    //public WorkflowStepViewModel WorkflowStepVM;
    
    public delegate void OnDone();

    public OnDone onDoneCallback;
}