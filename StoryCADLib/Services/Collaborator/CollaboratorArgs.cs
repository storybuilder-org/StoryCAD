using Microsoft.UI.Xaml;
using StoryCAD.Collaborator.ViewModels;
using WinUIEx;

﻿namespace StoryCAD.Services.Collaborator;

public class CollaboratorArgs
{
    public StoryElement SelectedElement;

    public StoryModel StoryModel;
    
    public WorkflowViewModel WorkflowVm;

	/// <summary>
	/// Reference to the Collaborator window
	/// </summary>
    public WindowEx CollaboratorWindow;

    //public WorkflowStepViewModel WorkflowStepVM;
    
    public delegate void OnDone();

    public OnDone onDoneCallback;
}