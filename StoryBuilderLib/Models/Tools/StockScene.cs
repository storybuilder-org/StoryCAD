using System.Collections.Generic;

namespace StoryBuilder.Models.Tools
{
    public class StockScene
    {
        public string Category;
        public List<string> Scenes;

        public StockScene(string category)
        {
            Category = category;
            Scenes = new List<string>();
        }
    }
}
