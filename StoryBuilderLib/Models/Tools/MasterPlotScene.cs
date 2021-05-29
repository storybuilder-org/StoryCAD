using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StoryBuilder.Models.Tools
{
    public class MasterPlotScene
    {
        public string SceneTitle;
        public string Notes;

        public MasterPlotScene(string title)
        {
            SceneTitle = title;
            Notes = string.Empty;
        }
    }
}
