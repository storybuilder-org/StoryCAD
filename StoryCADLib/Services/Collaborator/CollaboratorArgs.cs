using StoryCAD.Models;
using WinUIEx;

namespace StoryCAD.Services.Collaborator;

public class CollaboratorArgs
{
    public StoryElement SelectedElement;

    public StoryModel StoryModel;

    public WindowEx window;

    public delegate void OnDone();

    public OnDone onDoneCallback;
}