using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace StoryCADLib.Collaborator.Views;

public sealed partial class CollaboratorHostRoot : Page
{
    public CollaboratorHostRoot()
    {
        InitializeComponent();
    }

    public Frame RootFrameControl => RootFrame;
}
