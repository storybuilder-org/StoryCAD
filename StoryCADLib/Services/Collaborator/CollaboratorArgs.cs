﻿using StoryCAD.Models;
using System.Collections.Generic;
using StoryCAD.Collaborator.Models;
using StoryCAD.Collaborator.ViewModels;

namespace StoryCAD.Services.Collaborator;

public class CollaboratorArgs
{
    public StoryElement SelectedElement;

    public StoryModel StoryModel;
    
    public WizardViewModel WizardVM;

    public WizardStepViewModel WizardStepVM;
    
    public delegate void OnDone();

    public OnDone onDoneCallback;
}