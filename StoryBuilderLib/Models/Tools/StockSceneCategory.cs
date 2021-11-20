using System.Collections.Generic;

namespace StoryBuilder.Models.Tools
{
    public class StockSceneCategory
    {
        public string Category;
        public List<string> Scenes;

        public StockSceneCategory(string category)
        {
            Category = category;
            Scenes = new List<string>();
        }
    }
}
