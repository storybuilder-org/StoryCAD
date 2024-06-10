using StoryCAD.Models;
using System.Collections.Generic;
using StoryCAD.Collaborator.Models;

namespace StoryCAD.Services.Collaborator;

public class CollaboratorArgs
{
    public StoryElement SelectedElement;

    public StoryModel StoryModel;

    public delegate void OnDone();

    public OnDone onDoneCallback;
}