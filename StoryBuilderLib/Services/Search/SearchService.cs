using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Contacts.DataProvider;
using StoryBuilder.Models;
using StoryBuilder.ViewModels;

namespace StoryBuilder.Services.Search
{
    public class SearchService
    {
        private string arg;
        /// <summary>
        /// Search a StoryElement for a given string search argument
        /// </summary>
        /// <param name="node">StoryNodeItem whose StoryElement to search</param>
        /// <param name="searchArg">string to search for</param>
        /// <returns>true if StoryyElement contains search argument</returns>
        public bool SearchStoryElement(StoryNodeItem node, string searchArg)
        {
            bool result = false;
            arg = searchArg.ToLower();
            StoryElement element = null;

            if (StoryElement.StoryElements.ContainsKey(node.Uuid))
                element = StoryElement.StoryElements[node.Uuid];
            if (element == null)
                return false;
            switch (element.Type)
            {
                case StoryItemType.StoryOverview:
                    result = SearchStoryOverview(node, element);
                    break;
                case StoryItemType.Problem:
                    result = SearchProblem(node, element);
                    break;
                case StoryItemType.Character:
                    result = SearchCharacter(node, element);
                    break;
                case StoryItemType.Setting:
                    result = SearchSetting(node, element);
                    break;
                case StoryItemType.PlotPoint:
                    result = SearchPlotPoint(node, element);
                    break;
                case StoryItemType.Folder:
                    result = SearchFolder(node, element);
                    break;
                case StoryItemType.Section:
                    result = SearchSection(node, element);
                    break;
            }
            return result;
        }
        private bool Comparator(string text)
        {
            if (text.ToLower().Contains(arg))
                return true;
            return false;
        }

        private bool SearchSection(StoryNodeItem node, StoryElement element)
        {
            return Comparator(element.Name);
        }

        private bool SearchFolder(StoryNodeItem node, StoryElement element)
        {
            return Comparator(element.Name);
        }

        private bool SearchPlotPoint(StoryNodeItem node, StoryElement element)
        {
            return Comparator(element.Name);;
        }

        private bool SearchSetting(StoryNodeItem node, StoryElement element)
        {
            return Comparator(element.Name);
        }

        private bool SearchCharacter(StoryNodeItem node, StoryElement element)
        {
            return Comparator(element.Name);
        }

        private bool SearchProblem(StoryNodeItem node, StoryElement element)
        {
            return Comparator(element.Name);
        }

        private bool SearchStoryOverview(StoryNodeItem node, StoryElement element)
        {
            return Comparator(element.Name);
        }
    }
}
