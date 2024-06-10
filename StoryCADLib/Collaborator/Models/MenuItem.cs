using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StoryCAD.Collaborator.Models
{
    /// <summary>
    /// The WizardShell displays a NavigationView for a StoryElement
    /// which contains menu items for each of the story properties  
    /// Collaborator will provide user assistance for. Each menu
    /// item is a NavigationViewItem which displays a Title (the
    /// story property name (ex., Genre, ProtGoal) and a Description.
    /// 
    public class MenuItem
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string PageType {  get; set; }
    }
}
