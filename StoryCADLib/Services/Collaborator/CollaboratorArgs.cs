using StoryCAD.Collaborator.ViewModels;

﻿namespace StoryCAD.Services.Collaborator;

public class CollaboratorArgs
{
    public StoryElement SelectedElement;

    public StoryModel StoryModel;
    
    public WizardViewModel WizardVm;

    public WizardStepViewModel WizardStepVM;
    
    public delegate void OnDone();

    public OnDone onDoneCallback;
}