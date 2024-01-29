using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StoryCAD.Models;
using WinUIEx;

namespace StoryCAD.Services.Collaborator
{
    public class CollaboratorArgs
    {
        public StoryElement SelectedElement;

        public StoryModel StoryModel;

        public WindowEx window;

    }
}
