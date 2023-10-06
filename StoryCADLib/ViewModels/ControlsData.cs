using System.Collections.Generic;
using StoryCAD.Models.Tools;

namespace StoryCAD.ViewModels
{
    /// <summary>
    /// This contains controls data
    /// previously stored in GlobalData.cs
    /// </summary>
    public class ControlData
    {
        //Character conflics
        public SortedDictionary<string, ConflictCategoryModel> ConflictTypes;
        
        /// <summary>
        /// Possible relations
        /// </summary>
        public List<string> RelationTypes;
    }
}
